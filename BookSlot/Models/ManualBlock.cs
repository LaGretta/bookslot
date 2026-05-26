namespace BookSlot.Models;

/// <summary>
/// Manually blocked time slot — prevents clients from booking a specific hour on a specific date.
/// Use when the owner is busy elsewhere (personal appointment, walk-in client, etc.)
/// </summary>
public class ManualBlock
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    /// <summary>The date to block (UTC, time part is midnight)</summary>
    public DateTime Date { get; set; }

    /// <summary>The specific time slot to block within that day</summary>
    public TimeSpan BlockedTime { get; set; }

    /// <summary>Optional reason visible only to the owner</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
