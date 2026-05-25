using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.Schedule;

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

    public List<WorkSchedule> Schedules { get; set; } = [];

    private async Task<Business?> GetBusinessAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        Schedules = await _db.WorkSchedules
            .Where(w => w.BusinessId == business.Id)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm(Name = "isWorking")] Dictionary<int, bool> isWorking,
        [FromForm(Name = "start")] Dictionary<int, string> start,
        [FromForm(Name = "end")] Dictionary<int, string> end)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        var existing = await _db.WorkSchedules
            .Where(w => w.BusinessId == business.Id)
            .ToListAsync();

        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            var schedule = existing.FirstOrDefault(w => w.DayOfWeek == day);
            var working = isWorking.ContainsKey(i) && isWorking[i];
            var startTime = TimeSpan.TryParse(start.GetValueOrDefault(i), out var s) ? s : new TimeSpan(9, 0, 0);
            var endTime = TimeSpan.TryParse(end.GetValueOrDefault(i), out var e) ? e : new TimeSpan(18, 0, 0);

            if (schedule == null)
            {
                _db.WorkSchedules.Add(new WorkSchedule
                {
                    BusinessId = business.Id,
                    DayOfWeek = day,
                    IsWorking = working,
                    StartTime = startTime,
                    EndTime = endTime
                });
            }
            else
            {
                schedule.IsWorking = working;
                schedule.StartTime = startTime;
                schedule.EndTime = endTime;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Розклад збережено!";
        return RedirectToPage();
    }
}
