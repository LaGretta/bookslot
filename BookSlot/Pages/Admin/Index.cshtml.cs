using BookSlot.Data;
using BookSlot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Admin;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private const string OwnerEmail = "sashagutsul2014@gmail.com";

    public IndexModel(ApplicationDbContext db) => _db = db;

    public List<UserRow> Users { get; set; } = [];
    public string? SearchEmail { get; set; }
    public string? Message { get; set; }

    // Stats
    public int TotalUsers { get; set; }
    public int TotalBusinesses { get; set; }
    public int TotalBookings { get; set; }
    public int BookingsThisMonth { get; set; }
    public int PaidSubscriptions { get; set; }
    public int PromoSubscriptions { get; set; }
    public int NewUsersThisWeek { get; set; }

    public class UserRow
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? BusinessName { get; set; }
        public string? BusinessSlug { get; set; }
        public int? SubscriptionId { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public DateTime? EndDate { get; set; }
        public bool PromoUsed { get; set; }
        public bool IsExpired { get; set; }
    }

    private bool IsAdmin() =>
        User.Identity?.IsAuthenticated == true &&
        User.Identity.Name == OwnerEmail;

    public async Task<IActionResult> OnGetAsync(string? search)
    {
        if (!IsAdmin()) return Forbid();

        SearchEmail = search;
        await LoadStatsAsync();
        await LoadUsersAsync(search);
        return Page();
    }

    public async Task<IActionResult> OnPostGrantPromoAsync(int subscriptionId)
    {
        if (!IsAdmin()) return Forbid();

        var sub = await _db.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.PromoUsed)
        {
            TempData["Error"] = "Акція вже була використана цим акаунтом.";
            await LoadUsersAsync(null);
            return Page();
        }

        sub.Plan = SubscriptionPlan.Basic;
        sub.StartDate = DateTime.UtcNow;
        sub.EndDate = DateTime.UtcNow.AddDays(7);
        sub.PromoUsed = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "✅ 7 днів Basic активовано!";
        return RedirectToPage();
    }

    private async Task LoadStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfWeek  = now.AddDays(-7);

        TotalUsers        = await _db.Users.CountAsync();
        TotalBusinesses   = await _db.Businesses.CountAsync();
        TotalBookings     = await _db.Bookings.CountAsync();
        BookingsThisMonth = await _db.Bookings.CountAsync(b => b.CreatedAt >= startOfMonth);
        PaidSubscriptions = await _db.Subscriptions.CountAsync(s =>
            s.Plan != SubscriptionPlan.Free && !s.PromoUsed && (s.EndDate == null || s.EndDate > now));
        PromoSubscriptions = await _db.Subscriptions.CountAsync(s =>
            s.Plan != SubscriptionPlan.Free && s.PromoUsed && (s.EndDate == null || s.EndDate > now));
        NewUsersThisWeek  = await _db.Businesses.CountAsync(b => b.CreatedAt >= startOfWeek);
    }

    private async Task LoadUsersAsync(string? search)
    {
        var query = _db.Users
            .GroupJoin(
                _db.Businesses.Include(b => b.Subscription),
                u => u.Id,
                b => b.UserId,
                (u, buses) => new { User = u, Businesses = buses })
            .SelectMany(
                x => x.Businesses.DefaultIfEmpty(),
                (x, b) => new { x.User, Business = b });

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.User.Email!.Contains(search));

        var rows = await query
            .OrderByDescending(x => x.Business != null ? x.Business.CreatedAt : DateTime.MinValue)
            .Take(50)
            .ToListAsync();

        Users = rows.Select(r => new UserRow
        {
            UserId       = r.User.Id,
            Email        = r.User.Email ?? "",
            BusinessName = r.Business?.Name,
            BusinessSlug = r.Business?.Slug,
            SubscriptionId = r.Business?.Subscription?.Id,
            Plan         = r.Business?.Subscription?.Plan ?? SubscriptionPlan.Free,
            EndDate      = r.Business?.Subscription?.EndDate,
            PromoUsed    = r.Business?.Subscription?.PromoUsed ?? false,
            IsExpired    = r.Business?.Subscription?.IsExpired ?? false,
        }).ToList();
    }
}
