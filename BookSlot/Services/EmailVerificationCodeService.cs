using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BookSlot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookSlot.Services;

public class EmailVerificationCodeService
{
    private const string LoginProvider = "BookSlot";
    private const string TokenName = "EmailConfirmationCode";
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _clock;

    public EmailVerificationCodeService(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        TimeProvider clock)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _clock = clock;
    }

    public async Task SendCodeAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiresAt = _clock.GetUtcNow().Add(CodeLifetime);
        var hash = HashCodeForUser(user, code);

        await _userManager.SetAuthenticationTokenAsync(
            user,
            LoginProvider,
            TokenName,
            $"{expiresAt:O}|{hash}");

        var safeEmail = HtmlEncoder.Default.Encode(user.Email);
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:28px;background:#0f1117;color:#f7f3ec;border-radius:24px">
              <div style="font-size:13px;letter-spacing:.18em;text-transform:uppercase;color:#d7c7a1;margin-bottom:18px">BookSlot verification</div>
              <h1 style="font-size:24px;margin:0 0 12px">Підтвердіть email</h1>
              <p style="color:#b8b3aa;line-height:1.6;margin:0 0 22px">Введіть цей код на сторінці реєстрації, щоб підтвердити адресу <strong style="color:#fff">{safeEmail}</strong>.</p>
              <div style="font-size:34px;letter-spacing:.35em;font-weight:800;background:#f7f3ec;color:#101014;border-radius:18px;padding:18px 22px;text-align:center">{code}</div>
              <p style="color:#8f897f;font-size:13px;line-height:1.5;margin:22px 0 0">Код діє 15 хвилин. Якщо ви не створювали акаунт BookSlot, просто проігноруйте цей лист.</p>
            </div>
            """;

        await _emailSender.SendEmailAsync(user.Email, "Код підтвердження BookSlot", html);
    }

    public async Task<bool> VerifyCodeAsync(ApplicationUser user, string code)
    {
        var payload = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, TokenName);
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        var parts = payload.Split('|', 2);
        if (parts.Length != 2 || !DateTimeOffset.TryParse(parts[0], out var expiresAt))
            return false;

        if (expiresAt < _clock.GetUtcNow())
            return false;

        var expectedHash = parts[1];
        var actualHash = HashCodeForUser(user, NormalizeCode(code));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHash),
            Encoding.UTF8.GetBytes(actualHash));
    }

    public Task ClearCodeAsync(ApplicationUser user) =>
        _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, TokenName);

    public static string NormalizeCode(string code) =>
        new(code.Where(char.IsDigit).Take(6).ToArray());

    private static string HashCodeForUser(ApplicationUser user, string code)
    {
        var stamp = user.SecurityStamp ?? "";
        var input = $"{user.Id}:{user.NormalizedEmail}:{stamp}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
