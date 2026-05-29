using Microsoft.AspNetCore.DataProtection;

namespace BookSlot.Features.AiAssistant.Telegram;

public class TelegramTokenProtector : ITelegramTokenProtector
{
    private readonly IDataProtector _protector;

    public TelegramTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("BookSlot.Telegram.BotToken.v1");
    }

    public string Protect(string token) => _protector.Protect(token);

    public string? TryUnprotect(string? protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
            return null;

        try
        {
            return _protector.Unprotect(protectedToken);
        }
        catch
        {
            // Token was stored with a different key or is corrupted — treat as missing.
            return null;
        }
    }
}
