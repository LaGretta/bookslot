namespace BookSlot.Features.AiAssistant.Contracts;

public class AiAssistantReply
{
    public AiIntentType Intent { get; set; } = AiIntentType.Unknown;
    public AiBookingDraft Draft { get; set; } = new();
    public string MessageToCustomer { get; set; } = "";
    public List<string> MissingFields { get; set; } = [];
    public List<string> SuggestedSlots { get; set; } = [];
    public bool CanCreateBooking { get; set; }
}
