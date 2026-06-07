using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class EmailVerificationCodeService
{
    private const string LoginProvider = "BookSlot";
    private const string TokenName = "EmailConfirmationCode";
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(10);
    private const int MaxFailedAttempts = 5;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _clock;

    public EmailVerificationCodeService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        TimeProvider clock)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _clock = clock;
    }

    public async Task<IdentityResult> ValidateNewRegistrationAsync(string email, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email };

        foreach (var validator in _userManager.UserValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user);
            if (!result.Succeeded)
                return Translate(result);
        }

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
                return Translate(result);
        }

        return IdentityResult.Success;
    }

    public async Task<PendingEmailRegistration> CreatePendingRegistrationAsync(string email, string password)
    {
        email = email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var now = _clock.GetUtcNow().UtcDateTime;

        var oldRows = await _db.PendingEmailRegistrations
            .Where(p => p.NormalizedEmail == normalizedEmail || p.ExpiresAt < now)
            .ToListAsync();
        _db.PendingEmailRegistrations.RemoveRange(oldRows);

        var tempUser = new ApplicationUser { UserName = email, Email = email };
        var pending = new PendingEmailRegistration
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = _userManager.PasswordHasher.HashPassword(tempUser, password),
            CreatedAt = now,
            ExpiresAt = now.Add(CodeLifetime),
            LastSentAt = now
        };

        var code = GenerateCode();
        SetPendingCode(pending, code);
        _db.PendingEmailRegistrations.Add(pending);
        await _db.SaveChangesAsync();

        try
        {
            await SendCodeEmailAsync(pending.Email, code);
        }
        catch
        {
            _db.PendingEmailRegistrations.Remove(pending);
            await _db.SaveChangesAsync();
            throw;
        }

        return pending;
    }

    public async Task<PendingEmailRegistration?> FindPendingAsync(Guid pendingId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var pending = await _db.PendingEmailRegistrations.FirstOrDefaultAsync(p => p.Id == pendingId);
        if (pending == null)
            return null;

        if (pending.ExpiresAt >= now)
            return pending;

        _db.PendingEmailRegistrations.Remove(pending);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<(IdentityResult Result, ApplicationUser? CreatedUser)> ConfirmPendingRegistrationAsync(
        Guid pendingId,
        string code)
    {
        var pending = await FindPendingAsync(pendingId);
        if (pending == null)
            return (IdentityResult.Failed(new IdentityError { Description = "Код вже закінчився. Спробуйте зареєструватись ще раз." }), null);

        var now = _clock.GetUtcNow().UtcDateTime;
        if (pending.LockoutEnd.HasValue && pending.LockoutEnd > now)
            return (IdentityResult.Failed(new IdentityError { Description = "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин." }), null);

        if (!VerifyPendingCode(pending, code))
        {
            pending.AccessFailedCount++;
            if (pending.AccessFailedCount >= MaxFailedAttempts)
                pending.LockoutEnd = now.Add(LockoutTime);

            await _db.SaveChangesAsync();
            return (IdentityResult.Failed(new IdentityError { Description = "Код неправильний або вже закінчився. Перевірте пошту або надішліть новий код." }), null);
        }

        if (await _userManager.FindByEmailAsync(pending.Email) != null)
        {
            _db.PendingEmailRegistrations.Remove(pending);
            await _db.SaveChangesAsync();
            return (IdentityResult.Failed(new IdentityError { Description = "Акаунт з таким email вже існує. Увійдіть або використайте іншу пошту." }), null);
        }

        var user = new ApplicationUser
        {
            UserName = pending.Email,
            Email = pending.Email,
            EmailConfirmed = true,
            PasswordHash = pending.PasswordHash
        };

        var result = Translate(await _userManager.CreateAsync(user));
        if (!result.Succeeded)
            return (result, null);

        _db.PendingEmailRegistrations.Remove(pending);
        await _db.SaveChangesAsync();
        return (IdentityResult.Success, user);
    }

    public async Task ResendPendingCodeAsync(PendingEmailRegistration pending)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var code = GenerateCode();
        SetPendingCode(pending, code);
        pending.ExpiresAt = now.Add(CodeLifetime);
        pending.LastSentAt = now;
        pending.AccessFailedCount = 0;
        pending.LockoutEnd = null;
        await _db.SaveChangesAsync();
        await SendCodeEmailAsync(pending.Email, code);
    }

    public async Task DeletePendingAsync(Guid pendingId)
    {
        var pending = await _db.PendingEmailRegistrations.FirstOrDefaultAsync(p => p.Id == pendingId);
        if (pending == null)
            return;

        _db.PendingEmailRegistrations.Remove(pending);
        await _db.SaveChangesAsync();
    }

    public async Task SendCodeAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var code = GenerateCode();
        var expiresAt = _clock.GetUtcNow().Add(CodeLifetime);
        var hash = HashUserCode(user, code);

        await _userManager.SetAuthenticationTokenAsync(
            user,
            LoginProvider,
            TokenName,
            $"{expiresAt:O}|{hash}");

        await SendCodeEmailAsync(user.Email, code);
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
        var actualHash = HashUserCode(user, NormalizeCode(code));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHash),
            Encoding.UTF8.GetBytes(actualHash));
    }

    public Task ClearCodeAsync(ApplicationUser user) =>
        _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, TokenName);

    public static string NormalizeCode(string code) =>
        new(code.Where(char.IsDigit).Take(6).ToArray());

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static void SetPendingCode(PendingEmailRegistration pending, string code)
    {
        pending.CodeSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        pending.CodeHash = HashPendingCode(pending, code);
    }

    private static bool VerifyPendingCode(PendingEmailRegistration pending, string code)
    {
        var expectedHash = pending.CodeHash;
        var actualHash = HashPendingCode(pending, NormalizeCode(code));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHash),
            Encoding.UTF8.GetBytes(actualHash));
    }

    private static string HashPendingCode(PendingEmailRegistration pending, string code)
    {
        var input = $"{pending.Id}:{pending.NormalizedEmail}:{pending.CodeSalt}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static string HashUserCode(ApplicationUser user, string code)
    {
        var stamp = user.SecurityStamp ?? "";
        var input = $"{user.Id}:{user.NormalizedEmail}:{stamp}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private async Task SendCodeEmailAsync(string email, string code)
    {
        var safeEmail = HtmlEncoder.Default.Encode(email);
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:28px;background:#0f1117;color:#f7f3ec;border-radius:24px">
              <div style="font-size:13px;letter-spacing:.18em;text-transform:uppercase;color:#d7c7a1;margin-bottom:18px">BookSlot verification</div>
              <h1 style="font-size:24px;margin:0 0 12px">Підтвердіть email</h1>
              <p style="color:#b8b3aa;line-height:1.6;margin:0 0 22px">Введіть цей код на сторінці реєстрації, щоб підтвердити адресу <strong style="color:#fff">{safeEmail}</strong>.</p>
              <div style="font-size:34px;letter-spacing:.35em;font-weight:800;background:#f7f3ec;color:#101014;border-radius:18px;padding:18px 22px;text-align:center">{code}</div>
              <p style="color:#8f897f;font-size:13px;line-height:1.5;margin:22px 0 0">Код діє 15 хвилин. Якщо ви не створювали акаунт BookSlot, просто проігноруйте цей лист.</p>
            </div>
            """;

        await _emailSender.SendEmailAsync(email, "Код підтвердження BookSlot", html);
    }

    private static IdentityResult Translate(IdentityResult result)
    {
        if (result.Succeeded)
            return result;

        return IdentityResult.Failed(result.Errors.Select(error => new IdentityError
        {
            Code = error.Code,
            Description = TranslateIdentityError(error)
        }).ToArray());
    }

    private static string TranslateIdentityError(IdentityError error) =>
        error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Акаунт з таким email вже існує. Увійдіть або використайте іншу пошту.",
            "PasswordTooShort" => "Пароль має містити мінімум 6 символів.",
            "PasswordRequiresDigit" => "Пароль має містити хоча б одну цифру.",
            "PasswordRequiresNonAlphanumeric" => "Пароль має містити хоча б один спеціальний символ.",
            "PasswordRequiresUpper" => "Пароль має містити хоча б одну велику літеру.",
            "PasswordRequiresLower" => "Пароль має містити хоча б одну малу літеру.",
            "InvalidEmail" => "Невірний формат email.",
            "InvalidUserName" => "Email містить недопустимі символи.",
            _ => error.Description
        };
}
