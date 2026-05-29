using System.Text.Json;
using BookSlot.Features.AiAssistant.Telegram;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookSlot.Pages.Api;

[IgnoreAntiforgeryToken]
public class TelegramAiDebugModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly ITelegramAssistantHandler _handler;

    public TelegramAiDebugModel(
        IWebHostEnvironment environment,
        ITelegramAssistantHandler handler)
    {
        _environment = environment;
        _handler = handler;
    }

    public async Task<IActionResult> OnPostAsync(int? businessId)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

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
            requireEnabledSettings: false,
            HttpContext.RequestAborted);

        return new JsonResult(new
        {
            environment = _environment.EnvironmentName,
            businessId,
            result.ConversationId,
            update.UpdateId,
            chatId = result.ExternalChatId,
            result.ShouldSendMessage,
            result.MessageToSend,
            result.CanCreateBooking
        });
    }
}
