using System.Text.Json;
using System.Text.Json.Serialization;
using BookSlot.Features.AiAssistant.Configuration;
using Microsoft.Extensions.Options;

namespace BookSlot.Features.AiAssistant.Telegram;

/// <summary>
/// Single platform-bot poller. There is ONE bot for the whole platform (token in config).
/// Each business is identified by the /start &lt;businessId&gt; deep-link; the handler routes
/// every message to the correct business. Owners never deal with tokens.
/// </summary>
public class TelegramPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AiAssistantOptions> _options;
    private readonly ILogger<TelegramPollingWorker> _logger;

    private long? _nextOffset;
    private bool _initialized;

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
            var token = options.TelegramBotToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                _initialized = false;
                _nextOffset = null;
                await Task.Delay(delay, stoppingToken);
                continue;
            }

            try
            {
                if (!_initialized)
                {
                    await DeleteWebhookAndDropPendingAsync(token, stoppingToken);
                    _initialized = true;
                }

                await PollOnceAsync(token, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram polling failed.");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task DeleteWebhookAndDropPendingAsync(string botToken, CancellationToken cancellationToken)
    {
        try
        {
            var uri = $"https://api.telegram.org/bot{botToken}/deleteWebhook?drop_pending_updates=true";
            var httpClient = _httpClientFactory.CreateClient(nameof(TelegramPollingWorker));
            using var _ = await httpClient.GetAsync(uri, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "deleteWebhook failed (non-fatal).");
        }
    }

    private async Task PollOnceAsync(string botToken, CancellationToken cancellationToken)
    {
        var response = await GetUpdatesAsync(botToken, _nextOffset, cancellationToken);

        foreach (var update in response.Result)
        {
            _nextOffset = update.UpdateId + 1;
            await HandleUpdateAsync(botToken, update, cancellationToken);
        }
    }

    private async Task<TelegramGetUpdatesResponse> GetUpdatesAsync(
        string botToken,
        long? offset,
        CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            "limit=10",
            "timeout=10",
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
        string botToken,
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ITelegramAssistantHandler>();
        var sender = scope.ServiceProvider.GetRequiredService<ITelegramMessageSender>();

        // businessId is resolved inside the handler from the /start payload or chat history.
        var result = await handler.HandleUpdateAsync(
            update,
            businessId: null,
            requireEnabledSettings: true,
            cancellationToken);

        if (result.ShouldSendMessage && result.ExternalChatId.HasValue)
        {
            var sendResult = await sender.SendMessageAsync(
                botToken,
                result.ExternalChatId.Value,
                result.MessageToSend,
                result.QuickReplies,
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
