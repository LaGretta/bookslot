using BookSlot.Data;
using BookSlot.Models;
using BookSlot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
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

    [EnableRateLimiting("webhook")]
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
            if (!meta.TryGetValue("businessId", out var businessIdValue) ||
                !int.TryParse(businessIdValue, out var businessId) ||
                !meta.TryGetValue("plan", out var planValue))
            {
                _logger.LogWarning("Stripe webhook missing expected metadata on session {SessionId}", session.Id);
                return new OkResult();
            }

            var plan = planValue == "Pro"
                ? SubscriptionPlan.Pro
                : planValue == "Basic"
                    ? SubscriptionPlan.Basic
                    : SubscriptionPlan.Free;

            if (plan == SubscriptionPlan.Free)
            {
                _logger.LogWarning("Stripe webhook rejected unknown plan {Plan} on session {SessionId}", planValue, session.Id);
                return new OkResult();
            }

            var businessExists = await _db.Businesses.AnyAsync(b => b.Id == businessId);
            if (!businessExists)
            {
                _logger.LogWarning("Stripe webhook rejected missing business {BusinessId} on session {SessionId}", businessId, session.Id);
                return new OkResult();
            }

            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.BusinessId == businessId);
            if (sub == null)
            {
                sub = new Models.Subscription { BusinessId = businessId };
                _db.Subscriptions.Add(sub);
            }

            sub.Plan      = plan;
            sub.StartDate = DateTime.UtcNow;
            sub.EndDate   = DateTime.UtcNow.AddMonths(1);
            sub.IsActive  = true;
            sub.PromoUsed = false;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Stripe: activated {Plan} for business {Id}", plan, businessId);
        }

        return new OkResult();
    }
}
