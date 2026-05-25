using BookSlot.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookSlot.Services;

// Підключає стандартний Identity "Forgot Password" до нашого Gmail SMTP
public class IdentityEmailSender : IEmailSender
{
    private readonly IEmailService _email;

    public IdentityEmailSender(IEmailService email) => _email = email;

    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        _email.SendRawAsync(email, email, subject, htmlMessage);
}
