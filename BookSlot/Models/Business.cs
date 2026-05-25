namespace BookSlot.Models;

public class Business
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Service> Services { get; set; } = [];
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public Subscription? Subscription { get; set; }
}
