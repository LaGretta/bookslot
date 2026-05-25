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
        Subscription = business.Subscription;
        return Page();
    }

    public async Task<IActionResult> OnPostSubscribeAsync(string plan)
    {
        var userId   = _userManager.GetUserId(User)!;
        var business = await _db.Businesses.Include(b => b.Subscription)
                                           .FirstOrDefaultAsync(b => b.UserId == userId);
        if (business == null) return RedirectToPage("/Dashboard/Settings/Index");

        if (!_stripe.IsConfigured)
        {
            TempData["Error"] = "Stripe ще не налаштований";
            return RedirectToPage();
        }

        var url = await _stripe.CreateCheckoutSessionAsync(business.Id, plan, business.Name);
        return Redirect(url);
    }
}
