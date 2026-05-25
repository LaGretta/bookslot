namespace BookSlot.Models;

public class Service
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public Business Business { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
}
