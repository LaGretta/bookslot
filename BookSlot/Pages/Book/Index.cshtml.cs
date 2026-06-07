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

    public IndexModel(ApplicationDbContext db, BookingService bookingService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _bookingService = bookingService;
        _userManager = userManager;
    }

    public Business? Business { get; set; }
    public List<Service> Services { get; set; } = [];
    public bool IsOwner { get; set; }

    public async Task OnGetAsync(string slug)
    {
        Business = await _db.Businesses
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);

        if (Business != null)
        {
            Services = await _db.Services
                .Where(s => s.BusinessId == Business.Id && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);
            IsOwner = userId != null && Business.UserId == userId;
        }
    }

    [EnableRateLimiting("booking-write")]
    public async Task<IActionResult> OnPostAsync(
        string slug, int serviceId, string date, string time,
        string clientName, string clientPhone, string clientEmail, string? notes)
    {
        Business = await _db.Businesses.FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        if (Business == null) return NotFound();

        Services = await _db.Services
            .Where(s => s.BusinessId == Business.Id && s.IsActive)
            .ToListAsync();

        if (!DateTime.TryParse(date, out var bookingDate) || !TimeSpan.TryParse(time, out var startTime))
        {
            ModelState.AddModelError("", "Невірна дата або час");
            return Page();
        }

        var booking = await _bookingService.CreateBookingAsync(
            Business.Id, serviceId, clientName, clientPhone, clientEmail, bookingDate, startTime, notes);

        if (booking == null)
        {
            ModelState.AddModelError("", "На жаль, цей час недоступний або ліміт записів у тарифі вичерпано.");
            return Page();
        }

        TempData["BookingSuccess"] = $"Ви записані на {bookingDate:dd.MM.yyyy} о {startTime:hh\\:mm}. Чекаємо вас!";
        return RedirectToPage(new { slug });
    }
}
