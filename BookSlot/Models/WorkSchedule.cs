namespace BookSlot.Models;

public class WorkSchedule
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);
    public bool IsWorking { get; set; } = true;

    public Business Business { get; set; } = null!;
}
