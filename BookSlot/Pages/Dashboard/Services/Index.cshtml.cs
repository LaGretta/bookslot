using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.Services;

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

    public List<Service> Services { get; set; } = [];

    private async Task<Business?> GetBusinessAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Businesses.FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        Services = await _db.Services
            .Where(s => s.BusinessId == business.Id && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string name, string? description, int duration, decimal price)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        _db.Services.Add(new Service
        {
            BusinessId = business.Id,
            Name = name,
            Description = description,
            DurationMinutes = duration,
            Price = price
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Послугу додано!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, string? description, int duration, decimal price)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == business.Id);
        if (service == null) return NotFound();

        service.Name = name;
        service.Description = description;
        service.DurationMinutes = duration;
        service.Price = price;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Послугу оновлено!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var business = await GetBusinessAsync();
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == business.Id);
        if (service != null)
        {
            service.IsActive = false;
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Послугу видалено!";
        return RedirectToPage();
    }
}
