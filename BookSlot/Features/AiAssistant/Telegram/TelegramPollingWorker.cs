using System.Text.Json;
using System.Text.Json.Serialization;
using BookSlot.Data;
using BookSlot.Features.AiAssistant.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookSlot.Features.AiAssistant.Telegram;

public class TelegramPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AiAssistantOptions> _options;
    private readonly ILogger<TelegramPollingWorker> _logger;
    private long? _nextOffset;
    private bool _pendingUpdatesChecked;

    public TelegramPollingWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AiAssistantOptions> options,
        ILogger<TelegramPollingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var delay = TimeSpan.FromSeconds(Math.Clamp(options.TelegramPollingIntervalSeconds, 1, 30));

            if (!IsPollingConfigured(options))
            {
                _pendingUpdatesChecked = false;
                _nextOffset = null;
                await Task.Delay(delay, stoppingToken);
                continue;
            }

            try
            {
                if (!_pendingUpdatesChecked)
                {
                    await DropPendingUpdatesIfConfiguredAsync(options, stoppingToken);
                    _pendingUpdatesChecked = true;
                }

                await PollOnceAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram polling failed.");
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private static bool IsPollingConfigured(AiAssistantOptions options) =>
        options.TelegramPollingEnabled &&
        options.TelegramPollingBusinessId.HasValue &&
        !string.IsNullOrWhiteSpace(options.TelegramBotToken);

    private async Task DropPendingUpdatesIfConfiguredAsync(
        AiAssistantOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.TelegramPollingDropPendingUpdatesOnStart)
            return;

        var response = await GetUpdatesAsync(
            options.TelegramBotToken!,
            offset: -1,
            limit: 1,
            timeoutSeconds: 0,
            cancellationToken);

        var lastUpdate = response.Result.LastOrDefault();
        if (lastUpdate != null)
            _nextOffset = lastUpdate.UpdateId + 1;
    }

    private async Task PollOnceAsync(
        AiAssistantOptions options,
        CancellationToken cancellationToken)
    {
        var response = await GetUpdatesAsync(
            options.TelegramBotToken!,
            _nextOffset,
            limit: 10,
            timeoutSeconds: 10,
            cancellationToken);

        foreach (var update in response.Result)
        {
            _nextOffset = update.UpdateId + 1;
            await HandleUpdateAsync(update, options.TelegramPollingBusinessId!.Value, cancellationToken);
        }
    }

    private async Task<TelegramGetUpdatesResponse> GetUpdatesAsync(
        string botToken,
        long? offset,
        int limit,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            $"limit={limit}",
            $"timeout={timeoutSeconds}",
            "allowed_updates=%5B%22message%22%5D"
        };

        if (offset.HasValue)
            query.Add($"offset={offset.Value}");

        var uri = $"https://api.telegram.org/bot{botToken}/getUpdates?{string.Join('&', query)}";
        var httpClient = _httpClientFactory.CreateClient(nameof(TelegramPollingWorker));
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<TelegramGetUpdatesResponse>(payload)
            ?? new TelegramGetUpdatesResponse();

        if (!result.Ok)
            _logger.LogWarning("Telegram getUpdates returned ok=false.");

        return result;
    }

    private async Task HandleUpdateAsync(
        TelegramUpdate update,
        int businessId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var connectionIsActive = await db.TelegramBotConnections
            .AsNoTracking()
            .AnyAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        if (!connectionIsActive)
        {
            _logger.LogInformation(
                "Telegram polling skipped update {UpdateId}: connection is not active for business {BusinessId}.",
                update.UpdateId,
                businessId);
            return;
        }

        var handler = scope.ServiceProvider.GetRequiredService<ITelegramAssistantHandler>();
        var sender = scope.ServiceProvider.GetRequiredService<ITelegramMessageSender>();

        var result = await handler.HandleUpdateAsync(
            update,
            businessId,
            requireEnabledSettings: true,
            cancellationToken);

        if (result.ShouldSendMessage && result.ExternalChatId.HasValue)
        {
            var sendResult = await sender.SendMessageAsync(
                result.ExternalChatId.Value,
                result.MessageToSend,
                cancellationToken);

            if (!sendResult.Success)
                _logger.LogWarning("Telegram polling send failed: {ErrorMessage}", sendResult.ErrorMessage);
        }
    }

    private sealed class TelegramGetUpdatesResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public List<TelegramUpdate> Result { get; set; } = [];
    }
}
