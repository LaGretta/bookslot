using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class BookingReminderWorker : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingReminderWorker> _logger;

    public BookingReminderWorker(IServiceScopeFactory scopeFactory, ILogger<BookingReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SendDueRemindersAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SendDueRemindersAsync(stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var windowEnd = now.AddHours(24);
            var dateStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
            var dateEnd = DateTime.SpecifyKind(windowEnd.Date.AddDays(1), DateTimeKind.Utc);

            var candidates = await db.Bookings
                .Include(b => b.Business)
                    .ThenInclude(b => b.Subscription)
                .Include(b => b.Service)
                .Where(b =>
                    b.Status == BookingStatus.Confirmed &&
                    b.ReminderSentAt == null &&
                    b.ClientEmail != null &&
                    b.ClientEmail != "" &&
                    b.BookingDate >= dateStart &&
                    b.BookingDate < dateEnd &&
                    b.Business.Subscription != null &&
                    b.Business.Subscription.Plan == SubscriptionPlan.Pro)
                .ToListAsync(cancellationToken);

            var sentAny = false;
            foreach (var booking in candidates)
            {
                if (string.IsNullOrWhiteSpace(booking.ClientEmail))
                    continue;

                var appointmentAt = DateTime.SpecifyKind(
                    booking.BookingDate.Date + booking.StartTime,
                    DateTimeKind.Utc);

                if (appointmentAt <= now || appointmentAt > windowEnd)
                    continue;

                await email.SendBookingReminderAsync(
                    booking.ClientEmail,
                    booking.ClientName,
                    booking.Business.Name,
                    booking.Service.Name,
                    booking.BookingDate,
                    booking.StartTime);

                booking.ReminderSentAt = now;
                sentAny = true;
            }

            if (sentAny)
                await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking reminders.");
        }
    }
}
