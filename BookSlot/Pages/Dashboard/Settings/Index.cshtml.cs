using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.Settings;

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

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        Business = await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<IActionResult> OnPostAsync(
        string name, string slug, string? description, string? phone, string? address, string? category)
    {
        var userId = _userManager.GetUserId(User)!;
        slug = slug.ToLower().Trim().Replace(" ", "-");

        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);

        if (business == null)
        {
            var slugExists = await _db.Businesses.AnyAsync(b => b.Slug == slug);
            if (slugExists)
            {
                ModelState.AddModelError("", "Цей slug вже зайнятий. Оберіть інший.");
                Business = new Business { Name = name, Slug = slug };
                return Page();
            }

            business = new Business { UserId = userId };
            _db.Businesses.Add(business);

            _db.Subscriptions.Add(new Models.Subscription { Business = business });

            var defaultDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday };
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                _db.WorkSchedules.Add(new WorkSchedule
                {
                    Business = business,
                    DayOfWeek = day,
                    IsWorking = defaultDays.Contains(day),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0)
                });
            }
        }
        else if (business.Slug != slug)
        {
            var slugExists = await _db.Businesses.AnyAsync(b => b.Slug == slug && b.Id != business.Id);
            if (slugExists)
            {
                ModelState.AddModelError("", "Цей slug вже зайнятий. Оберіть інший.");
                Business = business;
                return Page();
            }
        }

        business.Name = name;
        business.Slug = slug;
        business.Description = description;
        business.Phone = phone;
        business.Address = address;
        business.Category = category;

        await _db.SaveChangesAsync();
        Business = business;

        TempData["Success"] = "Налаштування збережено!";
        return RedirectToPage();
    }
}
