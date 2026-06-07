using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class BookingReminderWorker : BackgroundService
{
    private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingReminderWorker> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeZoneInfo _bookingTimeZone;

    public BookingReminderWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = TimeSpan.FromMinutes(Math.Max(
            1,
            configuration.GetValue("BookingReminders:CheckIntervalMinutes", 1)));
        _bookingTimeZone = ResolveTimeZone(
            configuration["BookingReminders:TimeZone"] ?? "Europe/Kyiv");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SendDueRemindersAsync(stoppingToken);

        using var timer = new PeriodicTimer(_checkInterval);
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

            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, _bookingTimeZone);
            var windowEndLocal = nowLocal.Add(ReminderLeadTime);
            var dateStart = DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Utc);
            var dateEnd = DateTime.SpecifyKind(windowEndLocal.Date.AddDays(1), DateTimeKind.Utc);

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
                    b.Business.Subscription.IsActive &&
                    b.Business.Subscription.Plan == SubscriptionPlan.Pro &&
                    (!b.Business.Subscription.EndDate.HasValue || b.Business.Subscription.EndDate > nowUtc.UtcDateTime))
                .ToListAsync(cancellationToken);

            var sentAny = false;
            var sentCount = 0;
            foreach (var booking in candidates)
            {
                if (string.IsNullOrWhiteSpace(booking.ClientEmail))
                    continue;

                var appointmentLocalDateTime = DateTime.SpecifyKind(
                    booking.BookingDate.Date + booking.StartTime,
                    DateTimeKind.Unspecified);
                var appointmentAtLocal = new DateTimeOffset(
                    appointmentLocalDateTime,
                    _bookingTimeZone.GetUtcOffset(appointmentLocalDateTime));

                if (appointmentAtLocal <= nowLocal || appointmentAtLocal > windowEndLocal)
                    continue;

                await email.SendBookingReminderAsync(
                    booking.ClientEmail,
                    booking.ClientName,
                    booking.Business.Name,
                    booking.Service.Name,
                    booking.BookingDate,
                    booking.StartTime);

                booking.ReminderSentAt = nowUtc.UtcDateTime;
                sentCount++;
                sentAny = true;

                _logger.LogInformation(
                    "Sent booking reminder for booking {BookingId} at {AppointmentAt} ({TimeZone})",
                    booking.Id,
                    appointmentAtLocal,
                    _bookingTimeZone.Id);
            }

            if (sentCount > 0)
                _logger.LogInformation("Sent {Count} booking reminder(s).", sentCount);

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

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        foreach (var candidate in TimeZoneCandidates(id))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static IEnumerable<string> TimeZoneCandidates(string id)
    {
        yield return id;

        if (id.Equals("Europe/Kyiv", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("Europe/Kiev", StringComparison.OrdinalIgnoreCase))
        {
            yield return "FLE Standard Time";
            yield return "Europe/Kiev";
        }
    }
}
