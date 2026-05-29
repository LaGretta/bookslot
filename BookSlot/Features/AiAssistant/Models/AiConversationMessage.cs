namespace BookSlot.Features.AiAssistant.Models;

public enum AiMessageSenderType
{
    Customer = 1,
    Assistant = 2,
    System = 3
}

public class AiConversationMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public AiMessageSenderType SenderType { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiConversation Conversation { get; set; } = null!;
}
