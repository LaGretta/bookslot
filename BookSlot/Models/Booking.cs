namespace BookSlot.Models;

public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }

public class Booking
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int ServiceId { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string ClientEmail { get; set; } = "";
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReminderSentAt { get; set; }

    public Business Business { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
