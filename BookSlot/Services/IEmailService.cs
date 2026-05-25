namespace BookSlot.Services;

public interface IEmailService
{
    Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody);
    Task SendBookingConfirmationAsync(string toEmail, string clientName, string businessName,
        string serviceName, DateTime date, TimeSpan time);
    Task SendBookingCancelledAsync(string toEmail, string clientName, string businessName, DateTime date);
    Task SendNewBookingNotificationAsync(string toEmail, string clientName, string serviceName,
        DateTime date, TimeSpan time, string clientPhone);
}
