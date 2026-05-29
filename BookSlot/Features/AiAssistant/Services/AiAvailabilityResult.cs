namespace BookSlot.Features.AiAssistant.Services;

public class AiAvailabilityResult
{
    public int BusinessId { get; set; }
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public List<TimeSpan> AvailableSlots { get; set; } = [];
    public bool HasAvailableSlots => AvailableSlots.Count > 0;
}
