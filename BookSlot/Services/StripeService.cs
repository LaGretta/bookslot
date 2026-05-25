using Stripe;
using Stripe.Checkout;

namespace BookSlot.Services;

public class StripeService
{
    private readonly string _secretKey;
    private readonly string _webhookSecret;
    private readonly string _baseUrl;

    public StripeService(IConfiguration config)
    {
        _secretKey     = config["Stripe:SecretKey"]     ?? "";
        _webhookSecret = config["Stripe:WebhookSecret"] ?? "";
        _baseUrl       = config["Stripe:BaseUrl"]       ?? "http://localhost:5287";
        StripeConfiguration.ApiKey = _secretKey;
    }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(_secretKey) && _secretKey.StartsWith("sk_");

    public async Task<string> CreateCheckoutSessionAsync(
        int businessId, string plan, string businessName)
    {
        var amount = plan == "Pro" ? 59900L : 29900L; // в копійках (центах для UAH = найменша одиниця)

        var options = new SessionCreateOptions
        {
            Mode              = "payment",
            Currency          = "uah",
            LineItems         = [new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency    = "uah",
                    UnitAmount  = amount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name        = $"BookSlot {plan}",
                        Description = $"Підписка на 1 місяць — {businessName}"
                    }
                },
                Quantity = 1
            }],
            SuccessUrl        = $"{_baseUrl}/Dashboard/Subscription?success=1",
            CancelUrl         = $"{_baseUrl}/Dashboard/Subscription?cancelled=1",
            Metadata          = new Dictionary<string, string>
            {
                ["businessId"] = businessId.ToString(),
                ["plan"]       = plan
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public Event? ParseWebhookEvent(string payload, string signature)
    {
        try
        {
            return EventUtility.ConstructEvent(payload, signature, _webhookSecret);
        }
        catch
        {
            return null;
        }
    }
}
