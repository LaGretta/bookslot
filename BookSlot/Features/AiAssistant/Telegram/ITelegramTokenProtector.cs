namespace BookSlot.Features.AiAssistant.Telegram;

/// <summary>
/// Encrypts/decrypts per-business Telegram bot tokens at rest using ASP.NET DataProtection.
/// Tokens are never stored in plaintext in the database.
/// </summary>
public interface ITelegramTokenProtector
{
    string Protect(string token);
    string? TryUnprotect(string? protectedToken);
}
