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

        TotalBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id);
        MonthBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id
            && b.BookingDate.Month == DateTime.Now.Month && b.BookingDate.Year == DateTime.Now.Year);
        TodayBookings = await _db.Bookings.CountAsync(b => b.BusinessId == Business.Id
            && b.BookingDate.Date == DateTime.Today);
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
