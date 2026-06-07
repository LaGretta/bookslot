using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BookSlot.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody) =>
        await SendAsync(toEmail, toName, subject, htmlBody);

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var username = _config["Email:Username"] ?? "";
            var password = _config["Email:Password"] ?? "";
            var senderEmail = _config["Email:SenderEmail"] ?? "";

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                username == "your-email@gmail.com" ||
                password == "your-app-password")
            {
                throw new EmailDeliveryException("SMTP email credentials are not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "BookSlot",
                string.IsNullOrWhiteSpace(senderEmail) ? username : senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:Host"] ?? "smtp.gmail.com",
                int.Parse(_config["Email:Port"] ?? "465"),
                SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(
                username,
                password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (EmailDeliveryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw new EmailDeliveryException("Failed to send email via SMTP.", ex);
        }
    }

    public async Task SendBookingConfirmationAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#6C63FF;padding:30px;text-align:center'>
            <h1 style='color:white;margin:0'>BookSlot</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9'>
            <h2>Запис підтверджено!</h2>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <p>Ваш запис успішно підтверджено.</p>
            <div style='background:white;border-radius:8px;padding:20px;margin:20px 0;border-left:4px solid #6C63FF'>
              <p><strong>Заклад:</strong> {businessName}</p>
              <p><strong>Послуга:</strong> {serviceName}</p>
              <p><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
            <p style='color:#666'>Якщо ви хочете скасувати запис, зверніться до закладу.</p>
          </div>
          <div style='background:#eee;padding:15px;text-align:center;font-size:12px;color:#999'>
            BookSlot — система онлайн-запису
          </div>
        </div>";

        await SendAsync(toEmail, clientName, $"Запис підтверджено — {businessName}", html);
    }

    public async Task SendBookingCancelledAsync(string toEmail, string clientName, string businessName, DateTime date)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#6C63FF;padding:30px;text-align:center'>
            <h1 style='color:white;margin:0'>BookSlot</h1>
          </div>
          <div style='padding:30px'>
            <h2>Запис скасовано</h2>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <p>На жаль, ваш запис на <strong>{date:dd.MM.yyyy}</strong> у <strong>{businessName}</strong> було скасовано.</p>
            <p>Ви можете записатись на інший час.</p>
          </div>
        </div>";

        await SendAsync(toEmail, clientName, $"Запис скасовано — {businessName}", html);
    }

    public async Task SendBookingReminderAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#111827;padding:30px;text-align:center'>
            <h1 style='color:white;margin:0'>BookSlot</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9'>
            <h2>Нагадування про запис</h2>
            <p>Привіт, <strong>{clientName}</strong>!</p>
            <p>Нагадуємо, що завтра у вас запис у <strong>{businessName}</strong>.</p>
            <div style='background:white;border-radius:8px;padding:20px;margin:20px 0;border-left:4px solid #6C63FF'>
              <p><strong>Послуга:</strong> {serviceName}</p>
              <p><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
            <p style='color:#666'>Якщо плани змінилися, зв'яжіться із закладом напряму.</p>
          </div>
        </div>";

        await SendAsync(toEmail, clientName, $"Нагадування про запис — {businessName}", html);
    }

    public async Task SendNewBookingNotificationAsync(string toEmail, string clientName, string serviceName,
        DateTime date, TimeSpan time, string clientPhone)
    {
        var html = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
          <div style='background:#6C63FF;padding:30px;text-align:center'>
            <h1 style='color:white;margin:0'>BookSlot</h1>
          </div>
          <div style='padding:30px;background:#f9f9f9'>
            <h2>Новий запис!</h2>
            <p>У вас новий клієнт:</p>
            <div style='background:white;border-radius:8px;padding:20px;border-left:4px solid #6C63FF'>
              <p><strong>Клієнт:</strong> {clientName}</p>
              <p><strong>Телефон:</strong> {clientPhone}</p>
              <p><strong>Послуга:</strong> {serviceName}</p>
              <p><strong>Дата:</strong> {date:dd.MM.yyyy}</p>
              <p><strong>Час:</strong> {time:hh\:mm}</p>
            </div>
          </div>
        </div>";

        await SendAsync(toEmail, "Власник", "Новий запис — BookSlot", html);
    }
}
