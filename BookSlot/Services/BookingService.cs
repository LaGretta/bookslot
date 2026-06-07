using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BookSlot.Services;

public class BookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingNotificationQueue _notificationQueue;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ApplicationDbContext db,
        IBookingNotificationQueue notificationQueue,
        ILogger<BookingService> logger)
    {
        _db = db;
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    public async Task<List<TimeSpan>> GetAvailableSlotsAsync(int businessId, int serviceId, DateTime date)
    {
        if (date.Date < DateTime.UtcNow.Date || date.Date > DateTime.UtcNow.Date.AddMonths(6))
            return [];

        var service = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId && s.IsActive);
        if (service == null) return [];

        var dayOfWeek = date.DayOfWeek;
        var schedule = await _db.WorkSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.BusinessId == businessId && w.DayOfWeek == dayOfWeek && w.IsWorking);

        if (schedule == null) return [];

        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddDays(1);
        var existingBookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId
                     && b.BookingDate >= dayStart && b.BookingDate < dayEnd
                     && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        // Load manually blocked times for this day
        var manualBlocks = await _db.ManualBlocks
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.Date == dayStart)
            .Select(b => b.BlockedTime)
            .ToListAsync();

        var slots = new List<TimeSpan>();
        var current = schedule.StartTime;
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);

        while (current + duration <= schedule.EndTime)
        {
            var slotEnd = current + duration;
            var isBooked = existingBookings.Any(b =>
                b.StartTime < slotEnd && b.EndTime > current);

            // Also skip any slot that overlaps a manually blocked time
            var isManuallyBlocked = manualBlocks.Any(bt => bt >= current && bt < slotEnd);

            if (!isBooked && !isManuallyBlocked)
                slots.Add(current);

            current = current.Add(TimeSpan.FromMinutes(30));
        }

        return slots;
    }

    public async Task<Booking?> CreateBookingAsync(int businessId, int serviceId,
        string clientName, string clientPhone, string clientEmail,
        DateTime date, TimeSpan startTime, string? notes = null)
    {
        if (date.Date < DateTime.UtcNow.Date || date.Date > DateTime.UtcNow.Date.AddMonths(6))
            return null;

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId);
        if (service == null) return null;

        var business = await _db.Businesses
            .Include(b => b.Subscription)
            .FirstOrDefaultAsync(b => b.Id == businessId);
        if (business == null) return null;

        if (!await CanCreateBookingThisMonthAsync(business))
        {
            _logger.LogWarning(
                "Booking rejected by monthly limit for business {BusinessId}. Plan={Plan}, IsActive={IsActive}, EndDate={EndDate}",
                business.Id,
                business.Subscription?.Plan,
                business.Subscription?.IsActive,
                business.Subscription?.EndDate);
            return null;
        }

        var available = await GetAvailableSlotsAsync(businessId, serviceId, date);
        if (!available.Contains(startTime)) return null;

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var bookingDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var bookingEnd = startTime + TimeSpan.FromMinutes(service.DurationMinutes);
        var alreadyBooked = await _db.Bookings.AnyAsync(b =>
            b.BusinessId == businessId &&
            b.BookingDate == bookingDate &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < bookingEnd &&
            b.EndTime > startTime);

        if (alreadyBooked)
            return null;

        var booking = new Booking
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            ClientName = TrimTo(clientName, 120),
            ClientPhone = TrimTo(clientPhone, 60),
            ClientEmail = TrimTo(clientEmail, 160),
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = bookingEnd,
            Notes = TrimTo(notes, 1000),
            Status = BookingStatus.Confirmed
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await QueueNotificationAsync(booking.Id);

        return booking;
    }

    private async Task<bool> CanCreateBookingThisMonthAsync(Business business)
    {
        var max = IsActiveSubscription(business.Subscription)
            ? business.Subscription!.MaxBookingsPerMonth
            : 30;
        if (max == int.MaxValue) return true;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var count = await _db.Bookings.CountAsync(b =>
            b.BusinessId == business.Id &&
            b.CreatedAt >= monthStart &&
            b.CreatedAt < monthEnd &&
            b.Status != BookingStatus.Cancelled);

        return count < max;
    }

    private async Task QueueNotificationAsync(int bookingId)
    {
        try
        {
            await _notificationQueue.QueueAsync(bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Booking {BookingId} was created, but notification queueing failed.", bookingId);
        }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int businessId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.BusinessId == businessId);

        if (booking == null) return false;

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteBookingAsync(int bookingId, int businessId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.BusinessId == businessId);

        if (booking == null) return false;

        booking.Status = BookingStatus.Completed;
        await _db.SaveChangesAsync();
        return true;
    }

    private static string TrimTo(string? value, int maxLength)
    {
        value = value?.Trim() ?? "";
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static bool IsActiveSubscription(Subscription? subscription) =>
        subscription is { IsActive: true } && !subscription.IsExpired;
}
