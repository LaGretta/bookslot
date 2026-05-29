using System.Text;
using System.Text.Json;

namespace BookSlot.Features.AiAssistant.Telegram;

public class TelegramMessageSender : ITelegramMessageSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramMessageSender> _logger;

    public TelegramMessageSender(
        HttpClient httpClient,
        ILogger<TelegramMessageSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TelegramSendResult> SendMessageAsync(
        string botToken,
        long chatId,
        string text,
        IReadOnlyCollection<string>? quickReplies = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return new TelegramSendResult
            {
                Success = false,
                ErrorMessage = "Telegram bot token is not configured."
            };
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramSendResult
            {
                Success = false,
                ErrorMessage = "Telegram message text is empty."
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["disable_web_page_preview"] = true
        };

        if (quickReplies is { Count: > 0 })
        {
            // One button per row → clean, full-width, easy to tap.
            payload["reply_markup"] = new
            {
                keyboard = quickReplies
                    .Select(label => new[] { new { text = label } })
                    .ToArray(),
                resize_keyboard = true,
                one_time_keyboard = true
            };
        }
        else
        {
            // Clear stale buttons when we now expect free-text (name, phone…).
            payload["reply_markup"] = new { remove_keyboard = true };
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.telegram.org/bot{botToken}/sendMessage");

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new TelegramSendResult { Success = true };

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Telegram sendMessage failed with {Status}: {Body}", response.StatusCode, body);

            return new TelegramSendResult
            {
                Success = false,
                ErrorMessage = $"Telegram API returned {response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram sendMessage failed.");
            return new TelegramSendResult
            {
                Success = false,
                ErrorMessage = "Telegram send failed."
            };
        }
    }
}
