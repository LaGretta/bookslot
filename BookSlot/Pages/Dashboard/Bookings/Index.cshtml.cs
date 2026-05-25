using BookSlot.Data;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.Bookings;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BookingService _bookingService;
    private readonly IEmailService _emailService;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        BookingService bookingService, IEmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _bookingService = bookingService;
        _emailService = emailService;
    }

    public List<Booking> Bookings { get; set; } = [];
    public DateTime? FilterDate { get; set; }
    public string? FilterStatus { get; set; }

    private async Task<Business?> GetBusinessAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<IActionResult> OnGetAsync(DateTime? date, string? status)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        FilterDate = date;
        FilterStatus = status;

        var query = _db.Bookings
            .Include(b => b.Service)
            .Where(b => b.BusinessId == business.Id);

        if (date.HasValue)
        {
            var dayStart = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
            var dayEnd   = dayStart.AddDays(1);
            query = query.Where(b => b.BookingDate >= dayStart && b.BookingDate < dayEnd);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        var raw = await query.OrderByDescending(b => b.BookingDate).ToListAsync();
        Bookings = raw.OrderByDescending(b => b.BookingDate).ThenBy(b => b.StartTime).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        await _bookingService.CompleteBookingAsync(id, business.Id);
        TempData["Success"] = "Запис позначено як завершений.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        var booking = await _db.Bookings.Include(b => b.Business).FirstOrDefaultAsync(b => b.Id == id && b.BusinessId == business.Id);
        if (booking != null)
        {
            await _bookingService.CancelBookingAsync(id, business.Id);
            if (!string.IsNullOrEmpty(booking.ClientEmail))
                await _emailService.SendBookingCancelledAsync(
                    booking.ClientEmail, booking.ClientName, business.Name, booking.BookingDate);
        }

        TempData["Success"] = "Запис скасовано.";
        return RedirectToPage();
    }
}
