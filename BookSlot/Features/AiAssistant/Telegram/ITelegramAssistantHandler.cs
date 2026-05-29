namespace BookSlot.Features.AiAssistant.Telegram;

public interface ITelegramAssistantHandler
{
    Task<TelegramAssistantResult> HandleUpdateAsync(
        TelegramUpdate update,
        int? businessId = null,
        bool requireEnabledSettings = false,
        CancellationToken cancellationToken = default);
}
