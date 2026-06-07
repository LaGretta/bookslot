using BookSlot.Data;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Book;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ApplicationDbContext db,
        BookingService bookingService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _bookingService = bookingService;
        _userManager = userManager;
    }

    public Business? Business { get; set; }
    public List<Service> Services { get; set; } = [];
    public bool IsOwner { get; set; }
    public bool IsClientSignedIn { get; set; }
    public string? CurrentUserEmail { get; set; }
    public string? CurrentUserName { get; set; }

    public async Task OnGetAsync(string slug)
    {
        await LoadPageAsync(slug);
    }

    [EnableRateLimiting("booking-write")]
    public async Task<IActionResult> OnPostAsync(
        string slug,
        int serviceId,
        string date,
        string time,
        string clientName,
        string clientPhone,
        string? notes)
    {
        await LoadPageAsync(slug);
        if (Business == null)
            return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToPage("/Account/Login", new
            {
                area = "Identity",
                returnUrl = Url.Page("/Book/Index", new { slug })
            });
        }

        if (!currentUser.EmailConfirmed || string.IsNullOrWhiteSpace(currentUser.Email))
        {
            return RedirectToPage("/Account/VerifyEmailCode", new
            {
                area = "Identity",
                userId = currentUser.Id,
                returnUrl = Url.Page("/Book/Index", new { slug })
            });
        }

        await LoadCurrentUserAsync(currentUser);

        if (!DateTime.TryParse(date, out var bookingDate) || !TimeSpan.TryParse(time, out var startTime))
        {
            ModelState.AddModelError(string.Empty, "Невірна дата або час");
            return Page();
        }

        var booking = await _bookingService.CreateBookingAsync(
            Business.Id,
            serviceId,
            clientName,
            clientPhone,
            currentUser.Email,
            bookingDate,
            startTime,
            notes);

        if (booking == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "На жаль, цей час недоступний або ліміт записів у тарифі вичерпано.");
            return Page();
        }

        TempData["BookingSuccess"] =
            $"Ви записані на {bookingDate:dd.MM.yyyy} о {startTime:hh\\:mm}. Чекаємо вас!";
        return RedirectToPage(new { slug });
    }

    private async Task LoadPageAsync(string slug)
    {
        Business = await _db.Businesses
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);

        if (Business == null)
            return;

        Services = await _db.Services
            .Where(s => s.BusinessId == Business.Id && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var userId = _userManager.GetUserId(User);
        IsOwner = userId != null && Business.UserId == userId;
        await LoadCurrentUserAsync();
    }

    private async Task LoadCurrentUserAsync(ApplicationUser? user = null)
    {
        user ??= await _userManager.GetUserAsync(User);
        IsClientSignedIn = user != null;
        CurrentUserEmail = user?.Email;
        CurrentUserName = string.IsNullOrWhiteSpace(user?.FullName)
            ? user?.Email
            : user.FullName;
    }
}
