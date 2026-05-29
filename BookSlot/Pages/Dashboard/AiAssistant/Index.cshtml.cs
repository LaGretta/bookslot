using BookSlot.Data;
using BookSlot.Features.AiAssistant.Configuration;
using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Features.AiAssistant.Services;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookSlot.Pages.Dashboard.AiAssistant;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAiReceptionistService _receptionistService;
    private readonly AiAssistantOptions _options;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAiReceptionistService receptionistService,
        IOptions<AiAssistantOptions> options)
    {
        _db = db;
        _userManager = userManager;
        _receptionistService = receptionistService;
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

    // Single platform bot — set once by the platform admin via config/env.
    public string? BotUsername => _options.TelegramBotUsername;
    public bool PlatformBotConfigured =>
        !string.IsNullOrWhiteSpace(_options.TelegramBotToken) &&
        !string.IsNullOrWhiteSpace(_options.TelegramBotUsername);

    /// <summary>Shareable link the owner gives to clients: t.me/&lt;bot&gt;?start=&lt;businessId&gt;</summary>
    public string? BotLink =>
        PlatformBotConfigured && Business != null
            ? $"https://t.me/{_options.TelegramBotUsername}?start={Business.Id}"
            : null;

    public bool IsLive => SettingsInput.IsEnabled && IsReadyForAiPilot && PlatformBotConfigured;

    public List<RecentAiConversationRow> RecentConversations { get; set; } = [];

    [BindProperty]
    public AiAssistantSettingsInput SettingsInput { get; set; } = new();

    [BindProperty]
    public string TestMessage { get; set; } = "";

    public AiAssistantReply? TestReply { get; set; }

    public class AiAssistantSettingsInput
    {
        public bool IsEnabled { get; set; }
        public string WelcomeMessage { get; set; } =
            "Привіт! Я допоможу обрати послугу та підготувати запис.";
        public string? BusinessDescription { get; set; }
        public string ToneOfVoice { get; set; } = "Дружній, чіткий і короткий";
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

    public async Task<IActionResult> OnPostTestMessageAsync()
    {
        if (!await LoadDashboardAsync())
            return RedirectToPage("/Dashboard/Settings/Index");

        await LoadSettingsInputAsync();

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
}
