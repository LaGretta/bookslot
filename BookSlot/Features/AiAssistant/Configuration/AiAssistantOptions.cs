namespace BookSlot.Features.AiAssistant.Configuration;

public class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    public bool IsEnabled { get; set; }
    public bool TelegramWebhookEnabled { get; set; }
    public int? TelegramWebhookBusinessId { get; set; }
    public bool TelegramPollingEnabled { get; set; }
    public int? TelegramPollingBusinessId { get; set; }
    public int TelegramPollingIntervalSeconds { get; set; } = 3;
    public bool TelegramPollingDropPendingUpdatesOnStart { get; set; } = true;
    public string? TelegramBotToken { get; set; }
    /// <summary>Public @username of the single platform bot (without @). Used to build per-business share links.</summary>
    public string? TelegramBotUsername { get; set; }
    public string? TelegramWebhookSecretToken { get; set; }
    public string? SetupSecretToken { get; set; }
    public string DefaultWelcomeMessage { get; set; } =
        "Hi! I can help you choose a service and book an appointment.";
    public string DefaultToneOfVoice { get; set; } = "Friendly, clear, and concise";
}
