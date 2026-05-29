namespace BookSlot.Features.AiAssistant.Models;

public enum AppointmentDraftStatus
{
    CollectingDetails = 1,
    ReadyToBook = 2,
    Booked = 3,
    Cancelled = 4
}

public class AppointmentDraft
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? ServiceId { get; set; }
    public DateTime? RequestedDate { get; set; }
    public TimeSpan? RequestedTime { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public AppointmentDraftStatus Status { get; set; } = AppointmentDraftStatus.CollectingDetails;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AiConversation Conversation { get; set; } = null!;
}
