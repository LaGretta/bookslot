using System.Text;
using System.Text.Json;

namespace BookSlot.Services;

/// <summary>
/// Email via Resend HTTP API — works on Railway (no SMTP port blocking).
/// Free plan: 3 000 emails/month. Sender: onboarding@resend.dev (until custom domain).
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient http, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var apiKey = GetConfigValue("Resend:ApiKey", "RESEND_API_KEY");
            var from   = GetConfigValue("Resend:From", "RESEND_FROM");
            if (string.IsNullOrWhiteSpace(from))
                from = "BookSlot <onboarding@resend.dev>";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new EmailDeliveryException("Resend API key is not configured. Set Resend:ApiKey or RESEND_API_KEY.");

            var payload = new
            {
                from,
                to   = new[] { toEmail },
                subject,
                html = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Resend API error {Status}: {Body}", response.StatusCode, body);
                throw new EmailDeliveryException($"Resend rejected email with status {(int)response.StatusCode}: {body}");
            }
        }
        catch (EmailDeliveryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Resend to {Email}", toEmail);
            throw new EmailDeliveryException("Failed to send email via Resend.", ex);
        }
    }

    public async Task SendBookingConfirmationAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#4F46E5;padding:30px;text-align:center;border-radius:12px 12px 0 0'>
            <h1 style='color:white;margin:0;font-size:1.5rem'>✅ Запис підтверджено!</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9;border-radius:0 0 12px 12px'>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <div style='background:white;border-radius:10px;padding:20px;margin:20px 0;border-left:4px solid #4F46E5'>
              <p style='margin:6px 0'><strong>Заклад:</strong> {businessName}</p>
              <p style='margin:6px 0'><strong>Послуга:</strong> {serviceName}</p>
              <p style='margin:6px 0'><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p style='margin:6px 0'><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
            <p style='color:#666;font-size:.9rem'>Щоб скасувати — зверніться до закладу.</p>
          </div>
        </div>";

        await SendRawAsync(toEmail, clientName, $"Запис підтверджено — {businessName}", html);
    }

    public async Task SendBookingCancelledAsync(string toEmail, string clientName, string businessName, DateTime date)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#EF4444;padding:30px;text-align:center;border-radius:12px 12px 0 0'>
            <h1 style='color:white;margin:0;font-size:1.5rem'>Запис скасовано</h1>
          </div>
          <div style='padding:30px'>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <p>Ваш запис на <strong>{date:dd.MM.yyyy}</strong> у <strong>{businessName}</strong> скасовано.</p>
            <p>Ви можете записатись на інший час.</p>
          </div>
        </div>";

        await SendRawAsync(toEmail, clientName, $"Запис скасовано — {businessName}", html);
    }

    public async Task SendBookingReminderAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#111827;padding:30px;text-align:center;border-radius:12px 12px 0 0'>
            <h1 style='color:white;margin:0;font-size:1.5rem'>Нагадування про запис</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9;border-radius:0 0 12px 12px'>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <p>Нагадуємо, що завтра у вас запис у <strong>{businessName}</strong>.</p>
            <div style='background:white;border-radius:10px;padding:20px;margin:20px 0;border-left:4px solid #4F46E5'>
              <p style='margin:6px 0'><strong>Послуга:</strong> {serviceName}</p>
              <p style='margin:6px 0'><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p style='margin:6px 0'><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
            <p style='color:#666;font-size:.9rem'>Якщо плани змінилися, зв'яжіться із закладом напряму.</p>
          </div>
        </div>";

        await SendRawAsync(toEmail, clientName, $"Нагадування про запис — {businessName}", html);
    }

    public async Task SendNewBookingNotificationAsync(string toEmail, string clientName, string serviceName,
        DateTime date, TimeSpan time, string clientPhone)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#4F46E5;padding:30px;text-align:center;border-radius:12px 12px 0 0'>
            <h1 style='color:white;margin:0;font-size:1.5rem'>🔔 Новий запис!</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9;border-radius:0 0 12px 12px'>
            <p>У вас новий клієнт:</p>
            <div style='background:white;border-radius:10px;padding:20px;border-left:4px solid #4F46E5'>
              <p style='margin:6px 0'><strong>Клієнт:</strong> {clientName}</p>
              <p style='margin:6px 0'><strong>Телефон:</strong> {clientPhone}</p>
              <p style='margin:6px 0'><strong>Послуга:</strong> {serviceName}</p>
              <p style='margin:6px 0'><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p style='margin:6px 0'><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
          </div>
        </div>";

        await SendRawAsync(toEmail, "Власник", "🔔 Новий запис — BookSlot", html);
    }

    private string GetConfigValue(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _config[key] ?? Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return "";
    }
}
