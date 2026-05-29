using BookSlot.Data;
using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Features.AiAssistant.Services;
using BookSlot.Features.AiAssistant.Telegram;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BookSlot.Features.AiAssistant.Configuration;

namespace BookSlot.Pages.Dashboard.AiAssistant;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAiReceptionistService _receptionistService;
    private readonly ITelegramTokenProtector _tokenProtector;
    private readonly AiAssistantOptions _options;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAiReceptionistService receptionistService,
        ITelegramTokenProtector tokenProtector,
        IOptions<AiAssistantOptions> options)
    {
        _db = db;
        _userManager = userManager;
        _receptionistService = receptionistService;
        _tokenProtector = tokenProtector;
        _options = options.Value;
    }

    public Business? Business { get; set; }
    public SubscriptionPlan CurrentPlan { get; set; } = SubscriptionPlan.Free;
    public int ActiveServicesCount { get; set; }
    public int WorkingDaysCount { get; set; }
    public bool HasActiveServices => ActiveServicesCount > 0;
    public bool HasWorkingSchedule => WorkingDaysCount > 0;
    public bool IsProPlan => CurrentPlan == SubscriptionPlan.Pro;
    public bool IsReadyForAiPilot => HasActiveServices && HasWorkingSchedule;
    public bool TelegramWebhookEnabled => _options.TelegramWebhookEnabled;
    // Token is now stored per-business in the DB (encrypted), set during load.
    public bool TelegramBotTokenConfigured { get; set; }
    public bool TelegramWebhookSecretConfigured => !string.IsNullOrWhiteSpace(_options.TelegramWebhookSecretToken);
    public bool TelegramConnectionMetadataActive =>
        TelegramInput.IsActive && !string.IsNullOrWhiteSpace(TelegramInput.BotUsername) && TelegramBotTokenConfigured;
    public string TelegramWebhookBusinessStatus
    {
        get
        {
            if (!_options.TelegramWebhookBusinessId.HasValue)
                return "Not mapped";

            if (Business == null)
                return "Unknown";

            return _options.TelegramWebhookBusinessId.Value == Business.Id
                ? "Mapped to this business"
                : "Mapped to another business";
        }
    }

    public List<RecentAiConversationRow> RecentConversations { get; set; } = [];

    [BindProperty]
    public AiAssistantSettingsInput SettingsInput { get; set; } = new();

    [BindProperty]
    public string TestMessage { get; set; } = "";

    public AiAssistantReply? TestReply { get; set; }

    [BindProperty]
    public TelegramConnectionInput TelegramInput { get; set; } = new();

    public class AiAssistantSettingsInput
    {
        public bool IsEnabled { get; set; }
        public string WelcomeMessage { get; set; } =
            "Привіт! Я допоможу обрати послугу та підготувати запис.";
        public string? BusinessDescription { get; set; }
        public string ToneOfVoice { get; set; } = "Дружній, чіткий і короткий";
    }

    public class TelegramConnectionInput
    {
        public string? BotUsername { get; set; }

        /// <summary>New token entered by the owner. Leave blank to keep the existing one.</summary>
        public string? BotToken { get; set; }

        /// <summary>True if a token is already saved (for display only — never the actual value).</summary>
        public bool HasToken { get; set; }

        public bool IsActive { get; set; }
    }

    public class RecentAiConversationRow
    {
        public int Id { get; set; }
        public AiConversationChannel Channel { get; set; }
        public AiConversationStatus Status { get; set; }
        public string ExternalChatId { get; set; } = "";
        public string? LastMessage { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadDashboardAsync())
            return RedirectToPage("/Dashboard/Settings/Index");

        var settings = await _db.AiAssistantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == Business!.Id);

        if (settings != null)
        {
            SettingsInput = new AiAssistantSettingsInput
            {
                IsEnabled = settings.IsEnabled,
                WelcomeMessage = settings.WelcomeMessage,
                BusinessDescription = settings.BusinessDescription,
                ToneOfVoice = settings.ToneOfVoice
            };
        }

        await LoadTelegramInputAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveSettingsAsync()
    {
        if (!await LoadDashboardAsync())
            return RedirectToPage("/Dashboard/Settings/Index");

        NormalizeSettingsInput();

        var settings = await _db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.BusinessId == Business!.Id);

        if (settings == null)
        {
            settings = new AiAssistantSettings
            {
                BusinessId = Business!.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.AiAssistantSettings.Add(settings);
        }

        settings.IsEnabled = SettingsInput.IsEnabled;
        settings.WelcomeMessage = SettingsInput.WelcomeMessage;
        settings.BusinessDescription = SettingsInput.BusinessDescription;
        settings.ToneOfVoice = SettingsInput.ToneOfVoice;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Налаштування AI-помічника збережено.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveTelegramAsync()
    {
        if (!await LoadDashboardAsync())
            return RedirectToPage("/Dashboard/Settings/Index");

        await LoadSettingsInputAsync();
        NormalizeTelegramInput();

        if (string.IsNullOrWhiteSpace(TelegramInput.BotUsername))
        {
            ModelState.AddModelError("TelegramInput.BotUsername", "Вкажи username Telegram-бота.");
            await LoadTelegramTokenStatusAsync();
            return Page();
        }

        var connection = await _db.TelegramBotConnections
            .FirstOrDefaultAsync(t => t.BusinessId == Business!.Id);

        if (connection == null)
        {
            connection = new TelegramBotConnection
            {
                BusinessId = Business!.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.TelegramBotConnections.Add(connection);
        }

        // Save a freshly entered token (encrypted). Blank input keeps the existing token.
        var newToken = TelegramInput.BotToken?.Trim();
        if (!string.IsNullOrWhiteSpace(newToken))
            connection.BotToken = _tokenProtector.Protect(newToken);

        var hasToken = !string.IsNullOrWhiteSpace(connection.BotToken);

        // Can't activate without a token — the bot would have nothing to authenticate with.
        if (TelegramInput.IsActive && !hasToken)
        {
            ModelState.AddModelError("TelegramInput.BotToken",
                "Щоб увімкнути підключення, встав токен бота від @BotFather.");
            await LoadTelegramTokenStatusAsync();
            return Page();
        }

        connection.BotUsername = TelegramInput.BotUsername!;
        connection.IsActive = TelegramInput.IsActive;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = hasToken && connection.IsActive
            ? "Telegram-бота підключено! Він уже відповідає клієнтам."
            : "Telegram-підключення збережено.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestMessageAsync()
    {
        if (!await LoadDashboardAsync())
            return RedirectToPage("/Dashboard/Settings/Index");

        await LoadSettingsInputAsync();
        await LoadTelegramInputAsync();

        if (string.IsNullOrWhiteSpace(TestMessage))
        {
            ModelState.AddModelError(nameof(TestMessage), "Введи тестове повідомлення клієнта.");
            return Page();
        }

        TestMessage = TestMessage.Trim();
        TestReply = await _receptionistService.HandleAsync(
            new AiReceptionistRequest
            {
                BusinessId = Business!.Id,
                CustomerMessage = TestMessage,
                Draft = new AiBookingDraft()
            },
            HttpContext.RequestAborted);

        return Page();
    }

    private async Task<bool> LoadDashboardAsync()
    {
        var userId = _userManager.GetUserId(User)!;

        Business = await _db.Businesses
            .Include(b => b.Subscription)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId);

        if (Business == null)
            return false;

        CurrentPlan = Business.Subscription?.Plan ?? SubscriptionPlan.Free;

        ActiveServicesCount = await _db.Services
            .AsNoTracking()
            .CountAsync(s => s.BusinessId == Business.Id && s.IsActive);

        WorkingDaysCount = await _db.WorkSchedules
            .AsNoTracking()
            .CountAsync(w => w.BusinessId == Business.Id && w.IsWorking);

        await LoadRecentConversationsAsync();

        return true;
    }

    private async Task LoadRecentConversationsAsync()
    {
        if (Business == null)
            return;

        var conversations = await _db.AiConversations
            .AsNoTracking()
            .Where(c => c.BusinessId == Business.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(5)
            .ToListAsync();

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var latestMessages = await _db.AiConversationMessages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => new
            {
                ConversationId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.ConversationId, x => x.LastMessage);

        RecentConversations = conversations.Select(c => new RecentAiConversationRow
        {
            Id = c.Id,
            Channel = c.Channel,
            Status = c.Status,
            ExternalChatId = c.ExternalChatId,
            UpdatedAt = c.UpdatedAt,
            LastMessage = latestMessages.GetValueOrDefault(c.Id)
        }).ToList();
    }

    private async Task LoadSettingsInputAsync()
    {
        var settings = await _db.AiAssistantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == Business!.Id);

        if (settings == null)
            return;

        SettingsInput = new AiAssistantSettingsInput
        {
            IsEnabled = settings.IsEnabled,
            WelcomeMessage = settings.WelcomeMessage,
            BusinessDescription = settings.BusinessDescription,
            ToneOfVoice = settings.ToneOfVoice
        };
    }

    private async Task LoadTelegramInputAsync()
    {
        var connection = await _db.TelegramBotConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.BusinessId == Business!.Id);

        if (connection == null)
            return;

        var hasToken = !string.IsNullOrWhiteSpace(connection.BotToken);
        TelegramBotTokenConfigured = hasToken;

        TelegramInput = new TelegramConnectionInput
        {
            BotUsername = connection.BotUsername,
            IsActive = connection.IsActive,
            HasToken = hasToken
        };
    }

    private async Task LoadTelegramTokenStatusAsync()
    {
        var hasToken = await _db.TelegramBotConnections
            .AsNoTracking()
            .AnyAsync(t => t.BusinessId == Business!.Id && t.BotToken != null);

        TelegramBotTokenConfigured = hasToken;
        TelegramInput.HasToken = hasToken;
    }

    private void NormalizeSettingsInput()
    {
        SettingsInput.WelcomeMessage = string.IsNullOrWhiteSpace(SettingsInput.WelcomeMessage)
            ? "Привіт! Я допоможу обрати послугу та підготувати запис."
            : SettingsInput.WelcomeMessage.Trim();

        SettingsInput.BusinessDescription = string.IsNullOrWhiteSpace(SettingsInput.BusinessDescription)
            ? null
            : SettingsInput.BusinessDescription.Trim();

        SettingsInput.ToneOfVoice = string.IsNullOrWhiteSpace(SettingsInput.ToneOfVoice)
            ? "Дружній, чіткий і короткий"
            : SettingsInput.ToneOfVoice.Trim();
    }

    private void NormalizeTelegramInput()
    {
        var username = TelegramInput.BotUsername?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            TelegramInput.BotUsername = null;
            TelegramInput.IsActive = false;
            return;
        }

        TelegramInput.BotUsername = username.TrimStart('@');
    }
}
