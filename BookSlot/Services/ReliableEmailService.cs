namespace BookSlot.Services;

public class ReliableEmailService : IEmailService
{
    private readonly ResendEmailService _resend;
    private readonly EmailService _smtp;
    private readonly IConfiguration _config;
    private readonly ILogger<ReliableEmailService> _logger;

    public ReliableEmailService(
        ResendEmailService resend,
        EmailService smtp,
        IConfiguration config,
        ILogger<ReliableEmailService> logger)
    {
        _resend = resend;
        _smtp = smtp;
        _config = config;
        _logger = logger;
    }

    public Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody) =>
        SendWithFallbackAsync(
            "raw email",
            toEmail,
            service => service.SendRawAsync(toEmail, toName, subject, htmlBody));

    public Task SendBookingConfirmationAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time) =>
        SendWithFallbackAsync(
            "booking confirmation",
            toEmail,
            service => service.SendBookingConfirmationAsync(toEmail, clientName, businessName, serviceName, date, time));

    public Task SendBookingReminderAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time) =>
        SendWithFallbackAsync(
            "booking reminder",
            toEmail,
            service => service.SendBookingReminderAsync(toEmail, clientName, businessName, serviceName, date, time));

    public Task SendBookingCancelledAsync(string toEmail, string clientName, string businessName, DateTime date) =>
        SendWithFallbackAsync(
            "booking cancellation",
            toEmail,
            service => service.SendBookingCancelledAsync(toEmail, clientName, businessName, date));

    public Task SendNewBookingNotificationAsync(string toEmail, string clientName, string serviceName,
        DateTime date, TimeSpan time, string clientPhone) =>
        SendWithFallbackAsync(
            "new booking notification",
            toEmail,
            service => service.SendNewBookingNotificationAsync(toEmail, clientName, serviceName, date, time, clientPhone));

    private async Task SendWithFallbackAsync(string purpose, string toEmail, Func<IEmailService, Task> send)
    {
        EmailDeliveryException? resendError = null;

        if (HasResendConfig())
        {
            try
            {
                await send(_resend);
                return;
            }
            catch (EmailDeliveryException ex)
            {
                resendError = ex;
                _logger.LogError(ex, "Resend failed to send {Purpose} to {Email}; trying SMTP fallback if configured.", purpose, toEmail);
            }
        }

        if (HasSmtpConfig())
        {
            try
            {
                await send(_smtp);
                return;
            }
            catch (EmailDeliveryException ex)
            {
                _logger.LogError(ex, "SMTP failed to send {Purpose} to {Email}.", purpose, toEmail);
                throw;
            }
        }

        if (resendError != null)
            throw resendError;

        throw new EmailDeliveryException("Email provider is not configured. Set RESEND_API_KEY or SMTP/EMAIL credentials.");
    }

    private bool HasResendConfig() =>
        HasConfigValue("Resend:ApiKey", "RESEND_API_KEY");

    private bool HasSmtpConfig() =>
        HasConfigValue("Email:Username", "EMAIL_USERNAME", "SMTP_USERNAME") &&
        HasConfigValue("Email:Password", "EMAIL_PASSWORD", "SMTP_PASSWORD");

    private bool HasConfigValue(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _config[key] ?? Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value) &&
                value != "your-email@gmail.com" &&
                value != "your-app-password")
            {
                return true;
            }
        }

        return false;
    }
}
