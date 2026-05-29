namespace BookSlot.Features.AiAssistant.Telegram;

public class TelegramAssistantResult
{
    public int? ConversationId { get; set; }
    public long? ExternalChatId { get; set; }
    public string MessageToSend { get; set; } = "";
    public bool ShouldSendMessage { get; set; }
    public bool CanCreateBooking { get; set; }
}
