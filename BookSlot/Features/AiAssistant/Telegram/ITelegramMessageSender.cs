namespace BookSlot.Features.AiAssistant.Telegram;

public interface ITelegramMessageSender
{
    Task<TelegramSendResult> SendMessageAsync(
        string botToken,
        long chatId,
        string text,
        CancellationToken cancellationToken = default);
}
