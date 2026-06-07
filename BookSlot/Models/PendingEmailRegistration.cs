namespace BookSlot.Models;

public class PendingEmailRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public string CodeSalt { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime LastSentAt { get; set; } = DateTime.UtcNow;
    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
}
