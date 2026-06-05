using BookSlot.Data;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Features.AiAssistant.Services;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.AiAssistant.Conversations;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BookingService _bookingService;
    private readonly IAiConversationStore _conversationStore;

    public DetailsModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        BookingService bookingService,
        IAiConversationStore conversationStore)
    {
        _db = db;
        _userManager = userManager;
        _bookingService = bookingService;
        _conversationStore = conversationStore;
    }

    public Business? Business { get; set; }
    public AiConversation? Conversation { get; set; }
    public AppointmentDraft? Draft { get; set; }
    public Service? DraftService { get; set; }
    public List<AiConversationMessage> Messages { get; set; } = [];
    public bool CanCreateBookingFromDraft =>
        Draft?.Status == AppointmentDraftStatus.ReadyToBook &&
        Draft.ServiceId.HasValue &&
        Draft.RequestedDate.HasValue &&
        Draft.RequestedTime.HasValue &&
        !string.IsNullOrWhiteSpace(Draft.CustomerName) &&
        !string.IsNullOrWhiteSpace(Draft.CustomerContact);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateBookingAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        if (!CanCreateBookingFromDraft)
        {
            TempData["Error"] = "Чернетка ще не готова для створення запису.";
            return RedirectToPage(new { id });
        }

        var contact = Draft!.CustomerContact!.Trim();
        var clientEmail = LooksLikeEmail(contact) ? contact : "";
        var clientPhone = string.IsNullOrWhiteSpace(clientEmail) ? contact : "";

        var booking = await _bookingService.CreateBookingAsync(
            Business!.Id,
            Draft.ServiceId!.Value,
            Draft.CustomerName!.Trim(),
            clientPhone,
            clientEmail,
            Draft.RequestedDate!.Value,
            Draft.RequestedTime!.Value,
            notes: $"Створено вручну з AI-розмови #{Conversation!.Id}.");

        if (booking == null)
        {
            TempData["Error"] = "Не вдалося створити запис. Слот може бути вже недоступний або ліміт тарифу вичерпано.";
            return RedirectToPage(new { id });
        }

        await _conversationStore.MarkDraftBookedAsync(Conversation!.Id);
        await _conversationStore.AddMessageAsync(
            Conversation.Id,
            AiMessageSenderType.System,
            $"Власник створив запис #{booking.Id} з цієї AI-чернетки.",
            HttpContext.RequestAborted);

        TempData["Success"] = $"Запис #{booking.Id} створено з AI-чернетки.";
        return RedirectToPage(new { id });
    }

    private static bool LooksLikeEmail(string contact) =>
        contact.Contains('@') &&
        contact.Contains('.') &&
        !contact.StartsWith('@') &&
        contact.IndexOf('@') > 0;

    private async Task<bool> LoadAsync(int conversationId)
    {
        var userId = _userManager.GetUserId(User)!;
        Business = await _db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId);

        if (Business == null)
            return false;

        Conversation = await _db.AiConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.BusinessId == Business.Id);

        if (Conversation == null)
            return false;

        Messages = await _db.AiConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == Conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        Draft = await _db.AppointmentDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ConversationId == Conversation.Id);

        if (Draft?.ServiceId != null)
            DraftService = await _db.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == Draft.ServiceId.Value && s.BusinessId == Business.Id);

        return true;
    }
}
