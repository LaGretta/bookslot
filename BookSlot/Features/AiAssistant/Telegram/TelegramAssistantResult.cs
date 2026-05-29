namespace BookSlot.Features.AiAssistant.Telegram;

public class TelegramAssistantResult
{
    public int? ConversationId { get; set; }
    public long? ExternalChatId { get; set; }
    public string MessageToSend { get; set; } = "";
    public bool ShouldSendMessage { get; set; }
    public bool CanCreateBooking { get; set; }

    /// <summary>Tap-to-send button labels shown as a Telegram reply keyboard (services, time slots…).</summary>
    public List<string> QuickReplies { get; set; } = [];
}
