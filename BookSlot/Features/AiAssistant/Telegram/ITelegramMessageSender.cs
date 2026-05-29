namespace BookSlot.Features.AiAssistant.Telegram;

public interface ITelegramMessageSender
{
    Task<TelegramSendResult> SendMessageAsync(
        string botToken,
        long chatId,
        string text,
        IReadOnlyCollection<string>? quickReplies = null,
        CancellationToken cancellationToken = default);
}
