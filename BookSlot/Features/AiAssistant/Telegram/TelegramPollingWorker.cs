using System.Text.Json;
using System.Text.Json.Serialization;
using BookSlot.Data;
using BookSlot.Features.AiAssistant.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookSlot.Features.AiAssistant.Telegram;

/// <summary>
/// Multi-tenant Telegram poller. Every interval it loads ALL active bot connections
/// from the database and polls each bot with its own per-business token.
/// No environment variables required — owners just paste their token in the dashboard.
/// </summary>
public class TelegramPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITelegramTokenProtector _tokenProtector;
    private readonly IOptionsMonitor<AiAssistantOptions> _options;
    private readonly ILogger<TelegramPollingWorker> _logger;

    // Per-business polling state (worker is a singleton, so this is safe in-memory state).
    private readonly Dictionary<int, long> _offsets = new();
    private readonly HashSet<int> _initialized = new();

    public TelegramPollingWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ITelegramTokenProtector tokenProtector,
        IOptionsMonitor<AiAssistantOptions> options,
        ILogger<TelegramPollingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _tokenProtector = tokenProtector;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromSeconds(
                Math.Clamp(_options.CurrentValue.TelegramPollingIntervalSeconds, 1, 30));

            try
            {
                var bots = await LoadActiveBotsAsync(stoppingToken);

                foreach (var bot in bots)
                {
                    try
                    {
                        await PollBotAsync(bot, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Telegram polling failed for business {BusinessId}.", bot.BusinessId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram polling loop failed.");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<List<ActiveBot>> LoadActiveBotsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var connections = await db.TelegramBotConnections
            .AsNoTracking()
            .Where(c => c.IsActive && c.BotToken != null)
            .Select(c => new { c.BusinessId, c.BotToken })
            .ToListAsync(cancellationToken);

        var bots = new List<ActiveBot>();
        foreach (var connection in connections)
        {
            var token = _tokenProtector.TryUnprotect(connection.BotToken);
            if (!string.IsNullOrWhiteSpace(token))
                bots.Add(new ActiveBot(connection.BusinessId, token));
        }

        // Forget state for bots that are no longer active.
        var activeIds = bots.Select(b => b.BusinessId).ToHashSet();
        foreach (var staleId in _offsets.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            _offsets.Remove(staleId);
            _initialized.Remove(staleId);
        }

        return bots;
    }

    private async Task PollBotAsync(ActiveBot bot, CancellationToken cancellationToken)
    {
        // First time we see this bot: drop any webhook + pending backlog so getUpdates works.
        if (!_initialized.Contains(bot.BusinessId))
        {
            await DeleteWebhookAndDropPendingAsync(bot.Token, cancellationToken);
            _initialized.Add(bot.BusinessId);
        }

        var offset = _offsets.TryGetValue(bot.BusinessId, out var stored) ? stored : (long?)null;
        var response = await GetUpdatesAsync(bot.Token, offset, cancellationToken);

        foreach (var update in response.Result)
        {
            _offsets[bot.BusinessId] = update.UpdateId + 1;
            await HandleUpdateAsync(bot, update, cancellationToken);
        }
    }

    private async Task DeleteWebhookAndDropPendingAsync(string botToken, CancellationToken cancellationToken)
    {
        try
        {
            var uri = $"https://api.telegram.org/bot{botToken}/deleteWebhook?drop_pending_updates=true";
            var httpClient = _httpClientFactory.CreateClient(nameof(TelegramPollingWorker));
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            // Best-effort; ignore failures (e.g. invalid token surfaces on getUpdates).
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "deleteWebhook failed (non-fatal).");
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
            "timeout=0",
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
        ActiveBot bot,
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ITelegramAssistantHandler>();
        var sender = scope.ServiceProvider.GetRequiredService<ITelegramMessageSender>();

        var result = await handler.HandleUpdateAsync(
            update,
            bot.BusinessId,
            requireEnabledSettings: true,
            cancellationToken);

        if (result.ShouldSendMessage && result.ExternalChatId.HasValue)
        {
            var sendResult = await sender.SendMessageAsync(
                bot.Token,
                result.ExternalChatId.Value,
                result.MessageToSend,
                cancellationToken);

            if (!sendResult.Success)
                _logger.LogWarning("Telegram polling send failed: {ErrorMessage}", sendResult.ErrorMessage);
        }
    }

    private readonly record struct ActiveBot(int BusinessId, string Token);

    private sealed class TelegramGetUpdatesResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public List<TelegramUpdate> Result { get; set; } = [];
    }
}
