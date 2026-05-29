using BookSlot.Data;
using BookSlot.Features.AiAssistant.Configuration;
using BookSlot.Features.AiAssistant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookSlot.Pages.Api;

[IgnoreAntiforgeryToken]
public class AiAssistantSetupModel : PageModel
{
    private const string SetupSecretHeaderName = "X-BookSlot-AI-Setup-Secret";

    private readonly ApplicationDbContext _db;
    private readonly AiAssistantOptions _options;

    public AiAssistantSetupModel(
        ApplicationDbContext db,
        IOptions<AiAssistantOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAuthorized())
            return NotFound();

        var businesses = await _db.Businesses
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Slug,
                b.IsActive,
                ActiveServices = _db.Services.Count(s => s.BusinessId == b.Id && s.IsActive),
                WorkingDays = _db.WorkSchedules.Count(w => w.BusinessId == b.Id && w.IsWorking),
                AiSettings = _db.AiAssistantSettings
                    .Where(s => s.BusinessId == b.Id)
                    .Select(s => new { s.IsEnabled })
                    .FirstOrDefault(),
                Telegram = _db.TelegramBotConnections
                    .Where(t => t.BusinessId == b.Id)
                    .Select(t => new { t.BotUsername, t.IsActive })
                    .FirstOrDefault()
            })
            .ToListAsync(HttpContext.RequestAborted);

        return new JsonResult(new { businesses });
    }

    public async Task<IActionResult> OnPostAsync(
        int businessId,
        string botUsername,
        bool enableAi = true,
        bool activateTelegram = true)
    {
        if (!IsAuthorized())
            return NotFound();

        var businessExists = await _db.Businesses
            .AnyAsync(b => b.Id == businessId && b.IsActive, HttpContext.RequestAborted);

        if (!businessExists)
            return BadRequest("Active business was not found.");

        var normalizedBotUsername = NormalizeBotUsername(botUsername);
        if (string.IsNullOrWhiteSpace(normalizedBotUsername))
            return BadRequest("Bot username is required.");

        var settings = await _db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.BusinessId == businessId, HttpContext.RequestAborted);

        if (settings == null)
        {
            settings = new AiAssistantSettings
            {
                BusinessId = businessId,
                CreatedAt = DateTime.UtcNow
            };
            _db.AiAssistantSettings.Add(settings);
        }

        settings.IsEnabled = enableAi;
        settings.WelcomeMessage = string.IsNullOrWhiteSpace(settings.WelcomeMessage)
            ? "Привіт! Я допоможу обрати послугу та підготувати запис."
            : settings.WelcomeMessage;
        settings.ToneOfVoice = string.IsNullOrWhiteSpace(settings.ToneOfVoice)
            ? "Friendly, clear, and concise"
            : settings.ToneOfVoice;
        settings.UpdatedAt = DateTime.UtcNow;

        var telegram = await _db.TelegramBotConnections
            .FirstOrDefaultAsync(t => t.BusinessId == businessId, HttpContext.RequestAborted);

        if (telegram == null)
        {
            telegram = new TelegramBotConnection
            {
                BusinessId = businessId,
                CreatedAt = DateTime.UtcNow
            };
            _db.TelegramBotConnections.Add(telegram);
        }

        telegram.BotUsername = normalizedBotUsername;
        telegram.IsActive = activateTelegram;
        telegram.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        return new JsonResult(new
        {
            configured = true,
            businessId,
            botUsername = normalizedBotUsername,
            aiEnabled = settings.IsEnabled,
            telegramActive = telegram.IsActive
        });
    }

    private bool IsAuthorized()
    {
        if (string.IsNullOrWhiteSpace(_options.SetupSecretToken))
            return false;

        if (!Request.Headers.TryGetValue(SetupSecretHeaderName, out var receivedSecret))
            return false;

        return string.Equals(
            receivedSecret.ToString(),
            _options.SetupSecretToken,
            StringComparison.Ordinal);
    }

    private static string NormalizeBotUsername(string botUsername) =>
        string.IsNullOrWhiteSpace(botUsername)
            ? ""
            : botUsername.Trim().TrimStart('@');
}
