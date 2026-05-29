namespace BookSlot.Features.AiAssistant.Models;

public enum AiConversationChannel
{
    Telegram = 1,
    DashboardTest = 2
}

public enum AiConversationStatus
{
    Open = 1,
    WaitingForCustomer = 2,
    BookingReady = 3,
    Closed = 4
}

public class AiConversation
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public AiConversationChannel Channel { get; set; } = AiConversationChannel.Telegram;
    public string ExternalChatId { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public AiConversationStatus Status { get; set; } = AiConversationStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AiConversationMessage> Messages { get; set; } = [];
}
