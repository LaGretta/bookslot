using System.Text.Json;
using BookSlot.Data;
using BookSlot.Features.AiAssistant.Configuration;
using BookSlot.Features.AiAssistant.Telegram;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookSlot.Pages.Api;

[IgnoreAntiforgeryToken]
public class TelegramWebhookModel : PageModel
{
    private const string TelegramSecretHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    private readonly AiAssistantOptions _options;
    private readonly ITelegramAssistantHandler _handler;
    private readonly ITelegramMessageSender _sender;
    private readonly ITelegramTokenProtector _tokenProtector;
    private readonly ApplicationDbContext _db;

    public TelegramWebhookModel(
        IOptions<AiAssistantOptions> options,
        ITelegramAssistantHandler handler,
        ITelegramMessageSender sender,
        ITelegramTokenProtector tokenProtector,
        ApplicationDbContext db)
    {
        _options = options.Value;
        _handler = handler;
        _sender = sender;
        _tokenProtector = tokenProtector;
        _db = db;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_options.TelegramWebhookEnabled)
            return NotFound();

        if (!IsValidTelegramSecret())
            return new UnauthorizedObjectResult("Invalid Telegram webhook secret.");

        if (!_options.TelegramWebhookBusinessId.HasValue)
            return BadRequest("Telegram webhook business mapping is not configured.");

        var businessId = _options.TelegramWebhookBusinessId.Value;
        var connection = await _db.TelegramBotConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.BusinessId == businessId && c.IsActive,
                HttpContext.RequestAborted);

        if (connection == null)
        {
            return new JsonResult(new
            {
                skipped = true,
                reason = "Telegram connection is not active for this business."
            });
        }

        var botToken = _tokenProtector.TryUnprotect(connection.BotToken);
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return new JsonResult(new
            {
                skipped = true,
                reason = "Telegram bot token is not configured for this business."
            });
        }

        TelegramUpdate? update;
        try
        {
            update = await JsonSerializer.DeserializeAsync<TelegramUpdate>(
                Request.Body,
                cancellationToken: HttpContext.RequestAborted);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid Telegram update JSON.");
        }

        if (update == null)
            return BadRequest("Telegram update JSON is required.");

        var result = await _handler.HandleUpdateAsync(
            update,
            businessId,
            requireEnabledSettings: true,
            HttpContext.RequestAborted);

        TelegramSendResult? sendResult = null;
        if (result.ShouldSendMessage && result.ExternalChatId.HasValue)
        {
            sendResult = await _sender.SendMessageAsync(
                botToken,
                result.ExternalChatId.Value,
                result.MessageToSend,
                result.QuickReplies,
                HttpContext.RequestAborted);
        }

        return new JsonResult(new
        {
            accepted = true,
            result.ConversationId,
            result.ExternalChatId,
            result.ShouldSendMessage,
            result.MessageToSend,
            result.CanCreateBooking,
            sendResult = sendResult == null
                ? null
                : new { sendResult.Success, sendResult.ErrorMessage }
        });
    }

    private bool IsValidTelegramSecret()
    {
        if (string.IsNullOrWhiteSpace(_options.TelegramWebhookSecretToken))
            return true;

        if (!Request.Headers.TryGetValue(TelegramSecretHeaderName, out var receivedSecret))
            return false;

        return string.Equals(
            receivedSecret.ToString(),
            _options.TelegramWebhookSecretToken,
            StringComparison.Ordinal);
    }
}
