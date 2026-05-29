namespace BookSlot.Features.AiAssistant.Models;

public class TelegramBotConnection
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string BotUsername { get; set; } = "";

    /// <summary>Encrypted (DataProtection) Telegram bot token. Null = not configured.</summary>
    public string? BotToken { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
