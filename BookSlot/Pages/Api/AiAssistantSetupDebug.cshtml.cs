using BookSlot.Data;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Api;

[IgnoreAntiforgeryToken]
public class AiAssistantSetupDebugModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AiAssistantSetupDebugModel(
        IWebHostEnvironment environment,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _environment = environment;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_environment.IsDevelopment())
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

        return new JsonResult(new
        {
            environment = _environment.EnvironmentName,
            businesses
        });
    }

    public async Task<IActionResult> OnPostAsync(
        int businessId,
        string botUsername,
        bool enableAi = true,
        bool activateTelegram = true)
    {
        if (!_environment.IsDevelopment())
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
            ? "Hi! I can help you choose a service and book an appointment."
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

    public async Task<IActionResult> OnPostSeedDemoAsync(
        string botUsername = "BookSlot_AI_Assistant_Bot")
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        const string demoEmail = "ai-demo-owner@bookslot.local";
        const string demoSlug = "ai-demo-studio";

        var user = await _userManager.FindByEmailAsync(demoEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true,
                FullName = "AI Demo Owner"
            };

            var result = await _userManager.CreateAsync(user, "Demo123!");
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    error = "Could not create demo user.",
                    details = result.Errors.Select(e => e.Description)
                });
            }
        }

        var business = await _db.Businesses
            .FirstOrDefaultAsync(b => b.Slug == demoSlug, HttpContext.RequestAborted);

        if (business == null)
        {
            business = new Business
            {
                UserId = user.Id,
                Name = "AI Demo Studio",
                Slug = demoSlug,
                Description = "Local demo business for BookSlot AI Receptionist.",
                Category = "Beauty",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Businesses.Add(business);
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
        }

        if (!await _db.Services.AnyAsync(s => s.BusinessId == business.Id && s.Name == "manicure", HttpContext.RequestAborted))
        {
            _db.Services.Add(new Service
            {
                BusinessId = business.Id,
                Name = "manicure",
                Description = "Demo manicure service.",
                DurationMinutes = 60,
                Price = 45,
                IsActive = true
            });
        }

        if (!await _db.WorkSchedules.AnyAsync(w => w.BusinessId == business.Id, HttpContext.RequestAborted))
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                _db.WorkSchedules.Add(new WorkSchedule
                {
                    BusinessId = business.Id,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    IsWorking = day is not DayOfWeek.Saturday and not DayOfWeek.Sunday
                });
            }
        }

        if (!await _db.Subscriptions.AnyAsync(s => s.BusinessId == business.Id, HttpContext.RequestAborted))
        {
            _db.Subscriptions.Add(new Subscription
            {
                BusinessId = business.Id,
                Plan = SubscriptionPlan.Pro,
                IsActive = true,
                StartDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        await ConfigureAiAsync(
            business.Id,
            botUsername,
            enableAi: true,
            activateTelegram: true);

        return new JsonResult(new
        {
            seeded = true,
            businessId = business.Id,
            business.Name,
            business.Slug,
            ownerEmail = demoEmail,
            ownerPassword = "Demo123!",
            botUsername = NormalizeBotUsername(botUsername)
        });
    }

    private static string NormalizeBotUsername(string botUsername) =>
        string.IsNullOrWhiteSpace(botUsername)
            ? ""
            : botUsername.Trim().TrimStart('@');

    private async Task ConfigureAiAsync(
        int businessId,
        string botUsername,
        bool enableAi,
        bool activateTelegram)
    {
        var normalizedBotUsername = NormalizeBotUsername(botUsername);

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
            ? "Hi! I can help you choose a service and book an appointment."
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
    }
}
