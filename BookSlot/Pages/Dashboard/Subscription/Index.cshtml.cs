using BookSlot.Data;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Pages.Dashboard.Subscription;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StripeService _stripe;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, StripeService stripe)
    {
        _db          = db;
        _userManager = userManager;
        _stripe      = stripe;
    }

    public Models.Subscription? Subscription    { get; set; }
    public Business?             Business        { get; set; }
    public bool                 StripeConfigured => _stripe.IsConfigured;
    public string?              SuccessMessage  { get; set; }

    public async Task<IActionResult> OnGetAsync(string? success, string? cancelled)
    {
        if (success == "1") TempData["Success"] = "Оплата пройшла! Підписку активовано.";
        if (cancelled == "1") TempData["Info"]  = "Оплату скасовано.";

        var userId   = _userManager.GetUserId(User)!;
        var business = await _db.Businesses.Include(b => b.Subscription)
                                           .FirstOrDefaultAsync(b => b.UserId == userId);
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");
        Business = business;
        Subscription = business.Subscription;
        return Page();
    }

    public async Task<IActionResult> OnPostSubscribeAsync(string plan)
    {
        return await StartCheckoutAsync(plan);
    }

    public async Task<IActionResult> OnGetCheckoutAsync(string plan)
    {
        return await StartCheckoutAsync(plan);
    }

    private async Task<IActionResult> StartCheckoutAsync(string plan)
    {
        var userId   = _userManager.GetUserId(User)!;
        var business = await _db.Businesses.Include(b => b.Subscription)
                                           .FirstOrDefaultAsync(b => b.UserId == userId);
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        var checkoutPlan = NormalizeCheckoutPlan(plan);
        if (checkoutPlan == null)
        {
            TempData["Error"] = "Невідомий тариф. Оберіть Basic або Pro AI.";
            return RedirectToPage();
        }

        if (!_stripe.IsConfigured)
        {
            TempData["Error"] = "Stripe ще не налаштований";
            return RedirectToPage();
        }

        var url = await _stripe.CreateCheckoutSessionAsync(business.Id, checkoutPlan, business.Name);
        return Redirect(url);
    }

    private static string? NormalizeCheckoutPlan(string? plan)
    {
        return plan?.Trim().ToLowerInvariant() switch
        {
            "basic" => "Basic",
            "pro" or "pro ai" or "proai" => "Pro",
            _ => null
        };
    }
}
