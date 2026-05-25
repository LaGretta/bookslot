using BookSlot.Data;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace BookSlot.Pages.Api;

[IgnoreAntiforgeryToken]
public class StripeWebhookModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StripeService        _stripe;
    private readonly ILogger<StripeWebhookModel> _logger;

    public StripeWebhookModel(ApplicationDbContext db, StripeService stripe,
        ILogger<StripeWebhookModel> logger)
    {
        _db     = db;
        _stripe = stripe;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var payload   = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var evt = _stripe.ParseWebhookEvent(payload, signature);
        if (evt == null) return BadRequest("Invalid signature");

        if (evt.Type == "checkout.session.completed")
        {
            var session = evt.Data.Object as Session;
            if (session == null) return new OkResult();

            var meta       = session.Metadata;
            var businessId = int.Parse(meta["businessId"]);
            var plan       = meta["plan"] == "Pro" ? SubscriptionPlan.Pro : SubscriptionPlan.Basic;

            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.BusinessId == businessId);
            if (sub != null)
            {
                sub.Plan      = plan;
                sub.StartDate = DateTime.UtcNow;
                sub.EndDate   = DateTime.UtcNow.AddMonths(1);
                sub.IsActive  = true;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Stripe: activated {Plan} for business {Id}", plan, businessId);
            }
        }

        return new OkResult();
    }
}
