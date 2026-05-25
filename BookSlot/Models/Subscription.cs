namespace BookSlot.Models;

public enum SubscriptionPlan { Free, Basic, Pro }

public class Subscription
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LiqPayOrderId { get; set; }

    public Business Business { get; set; } = null!;

    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool CanAddServices => Plan != SubscriptionPlan.Free || true;
    public int MaxBookingsPerMonth => Plan switch
    {
        SubscriptionPlan.Free => 30,
        SubscriptionPlan.Basic => 200,
        SubscriptionPlan.Pro => int.MaxValue,
        _ => 30
    };
}
