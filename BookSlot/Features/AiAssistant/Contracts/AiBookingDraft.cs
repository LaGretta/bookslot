namespace BookSlot.Features.AiAssistant.Contracts;

public class AiBookingDraft
{
    public int? BusinessId { get; set; }
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public DateTime? RequestedDate { get; set; }
    public TimeSpan? RequestedTime { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public string? Notes { get; set; }

    public bool HasMinimumBookingData =>
        BusinessId.HasValue &&
        ServiceId.HasValue &&
        RequestedDate.HasValue &&
        RequestedTime.HasValue &&
        !string.IsNullOrWhiteSpace(CustomerName) &&
        !string.IsNullOrWhiteSpace(CustomerContact);
}
