namespace BookSlot.Features.AiAssistant.Models;

public class AiAssistantSettings
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public bool IsEnabled { get; set; }
    public string WelcomeMessage { get; set; } =
        "Hi! I can help you choose a service and book an appointment.";
    public string? BusinessDescription { get; set; }
    public string ToneOfVoice { get; set; } = "Friendly, clear, and concise";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
