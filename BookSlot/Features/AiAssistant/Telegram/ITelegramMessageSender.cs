namespace BookSlot.Features.AiAssistant.Telegram;

public interface ITelegramMessageSender
{
    Task<TelegramSendResult> SendMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken = default);
}
