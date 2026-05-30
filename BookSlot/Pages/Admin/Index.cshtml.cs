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
    public string? SelectedUserId { get; set; }
    public UserProfile? SelectedUserProfile { get; set; }
    public string? Message { get; set; }
    public string OwnerPlanLabel { get; set; } = "Free";
    public string? OwnerBusinessName { get; set; }
    public bool OwnerHasBusiness { get; set; }

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

    public class UserProfile
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessSlug { get; set; }
        public string? BusinessDescription { get; set; }
        public string? BusinessPhone { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BusinessCategory { get; set; }
        public bool BusinessIsActive { get; set; }
        public DateTime? BusinessCreatedAt { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool PromoUsed { get; set; }
        public int ServicesCount { get; set; }
        public int BookingsCount { get; set; }
        public DateTime? LatestBookingDate { get; set; }
    }

    private bool IsAdmin() =>
        User.Identity?.IsAuthenticated == true &&
        string.Equals(User.Identity.Name, OwnerEmail, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(string? search, string? userId)
    {
        if (!IsAdmin()) return Forbid();

        SearchEmail = search;
        SelectedUserId = userId;
        await LoadStatsAsync();
        await LoadOwnerSubscriptionAsync();
        await LoadUsersAsync(search);
        await LoadSelectedUserProfileAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostSetOwnerPlanAsync(string plan)
    {
        if (!IsAdmin()) return Forbid();

        if (!TryParsePlan(plan, out var selectedPlan))
        {
            TempData["Error"] = "Невідомий тариф. Обери Free, Basic або Ultra AI.";
            return RedirectToPage();
        }

        var business = await GetOwnerBusinessAsync(includeSubscription: true);
        if (business == null)
        {
            TempData["Error"] = "Спочатку створи бізнес у Dashboard → Налаштування, тоді можна видати підписку.";
            return RedirectToPage();
        }

        var subscription = business.Subscription;
        if (subscription == null)
        {
            subscription = new Models.Subscription
            {
                BusinessId = business.Id
            };
            _db.Subscriptions.Add(subscription);
        }

        subscription.Plan = selectedPlan;
        subscription.StartDate = DateTime.UtcNow;
        subscription.EndDate = null;
        subscription.IsActive = true;
        subscription.PromoUsed = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = selectedPlan == SubscriptionPlan.Free
            ? "Підписку знято: твій акаунт повернуто на Free."
            : $"Тобі видано тариф {PlanLabel(selectedPlan)} без кінцевої дати.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearOwnerPlanAsync()
    {
        if (!IsAdmin()) return Forbid();

        var business = await GetOwnerBusinessAsync(includeSubscription: true);
        if (business?.Subscription == null)
        {
            TempData["Success"] = "У твого акаунта ще немає платної підписки — змін не потрібно.";
            return RedirectToPage();
        }

        business.Subscription.Plan = SubscriptionPlan.Free;
        business.Subscription.StartDate = DateTime.UtcNow;
        business.Subscription.EndDate = null;
        business.Subscription.IsActive = true;
        business.Subscription.PromoUsed = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Підписку знято: твій акаунт повернуто на Free.";
        return RedirectToPage();
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

    private async Task LoadOwnerSubscriptionAsync()
    {
        var business = await GetOwnerBusinessAsync(includeSubscription: true);
        OwnerHasBusiness = business != null;
        OwnerBusinessName = business?.Name;
        OwnerPlanLabel = PlanLabel(business?.Subscription?.Plan ?? SubscriptionPlan.Free);
    }

    private async Task<Business?> GetOwnerBusinessAsync(bool includeSubscription)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == OwnerEmail);

        if (user == null)
            return null;

        var query = _db.Businesses.AsQueryable();
        if (includeSubscription)
            query = query.Include(b => b.Subscription);

        return await query.FirstOrDefaultAsync(b => b.UserId == user.Id);
    }

    private static bool TryParsePlan(string? plan, out SubscriptionPlan subscriptionPlan)
    {
        subscriptionPlan = SubscriptionPlan.Free;

        if (string.IsNullOrWhiteSpace(plan))
            return false;

        return plan.Trim().ToLowerInvariant() switch
        {
            "free" => SetPlan(SubscriptionPlan.Free, out subscriptionPlan),
            "basic" => SetPlan(SubscriptionPlan.Basic, out subscriptionPlan),
            "pro" => SetPlan(SubscriptionPlan.Pro, out subscriptionPlan),
            "ultra" => SetPlan(SubscriptionPlan.Pro, out subscriptionPlan),
            "ultraai" => SetPlan(SubscriptionPlan.Pro, out subscriptionPlan),
            "ultra ai" => SetPlan(SubscriptionPlan.Pro, out subscriptionPlan),
            _ => false
        };
    }

    private static bool SetPlan(SubscriptionPlan value, out SubscriptionPlan target)
    {
        target = value;
        return true;
    }

    private static string PlanLabel(SubscriptionPlan plan) =>
        plan switch
        {
            SubscriptionPlan.Basic => "Basic",
            SubscriptionPlan.Pro => "Ultra AI",
            _ => "Free"
        };

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

    private async Task LoadSelectedUserProfileAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return;

        var business = await _db.Businesses
            .AsNoTracking()
            .Include(b => b.Subscription)
            .FirstOrDefaultAsync(b => b.UserId == user.Id);

        var servicesCount = 0;
        var bookingsCount = 0;
        DateTime? latestBookingDate = null;

        if (business != null)
        {
            servicesCount = await _db.Services.CountAsync(s => s.BusinessId == business.Id);
            bookingsCount = await _db.Bookings.CountAsync(b => b.BusinessId == business.Id);
            latestBookingDate = await _db.Bookings
                .Where(b => b.BusinessId == business.Id)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => (DateTime?)b.BookingDate)
                .FirstOrDefaultAsync();
        }

        SelectedUserProfile = new UserProfile
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            BusinessName = business?.Name,
            BusinessSlug = business?.Slug,
            BusinessDescription = business?.Description,
            BusinessPhone = business?.Phone,
            BusinessAddress = business?.Address,
            BusinessCategory = business?.Category,
            BusinessIsActive = business?.IsActive ?? false,
            BusinessCreatedAt = business?.CreatedAt,
            Plan = business?.Subscription?.Plan ?? SubscriptionPlan.Free,
            SubscriptionEndDate = business?.Subscription?.EndDate,
            PromoUsed = business?.Subscription?.PromoUsed ?? false,
            ServicesCount = servicesCount,
            BookingsCount = bookingsCount,
            LatestBookingDate = latestBookingDate
        };
    }
}
