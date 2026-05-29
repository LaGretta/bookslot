namespace BookSlot.Features.AiAssistant.Contracts;

public class AiReceptionistRequest
{
    public int BusinessId { get; set; }
    public string CustomerMessage { get; set; } = "";
    public AiBookingDraft Draft { get; set; } = new();
    public bool RequireEnabledSettings { get; set; }
}
