using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Business? Business { get; set; }
    public int TotalBookings { get; set; }
    public int MonthBookings { get; set; }
    public int TodayBookings { get; set; }
    public int ServicesCount { get; set; }
    public List<Booking> RecentBookings { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        Business = await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);

        if (Business == null)
            return RedirectToPage("/Dashboard/Settings/Index");

        var now        = DateTime.UtcNow;
        var today      = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrow   = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        TotalBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id);
        MonthBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id
            && b.BookingDate >= monthStart && b.BookingDate < monthEnd);
        TodayBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id
            && b.BookingDate >= today && b.BookingDate < tomorrow);
        ServicesCount = await _db.Services.CountAsync(s => s.BusinessId == Business.Id && s.IsActive);

        RecentBookings = await _db.Bookings
            .Include(b => b.Service)
            .Where(b => b.BusinessId == Business.Id)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();

        return Page();
    }
}
