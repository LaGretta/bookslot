using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class BookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public BookingService(ApplicationDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<List<TimeSpan>> GetAvailableSlotsAsync(int businessId, int serviceId, DateTime date)
    {
        var service = await _db.Services.FindAsync(serviceId);
        if (service == null) return [];

        var dayOfWeek = date.DayOfWeek;
        var schedule = await _db.WorkSchedules
            .FirstOrDefaultAsync(w => w.BusinessId == businessId && w.DayOfWeek == dayOfWeek && w.IsWorking);

        if (schedule == null) return [];

        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddDays(1);
        var existingBookings = await _db.Bookings
            .Where(b => b.BusinessId == businessId
                     && b.BookingDate >= dayStart && b.BookingDate < dayEnd
                     && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        // Load manually blocked times for this day
        var manualBlocks = await _db.ManualBlocks
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
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId);
        if (service == null) return null;

        var business = await _db.Businesses
            .Include(b => b.Subscription)
            .FirstOrDefaultAsync(b => b.Id == businessId);
        if (business == null) return null;

        if (!await CanCreateBookingThisMonthAsync(business))
            return null;

        var available = await GetAvailableSlotsAsync(businessId, serviceId, date);
        if (!available.Contains(startTime)) return null;

        var booking = new Booking
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            ClientName = clientName,
            ClientPhone = clientPhone,
            ClientEmail = clientEmail,
            BookingDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            StartTime = startTime,
            EndTime = startTime + TimeSpan.FromMinutes(service.DurationMinutes),
            Notes = notes,
            Status = BookingStatus.Confirmed
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await SendPlanNotificationsAsync(booking, business, service);

        return booking;
    }

    private async Task<bool> CanCreateBookingThisMonthAsync(Business business)
    {
        var max = business.Subscription?.MaxBookingsPerMonth ?? 30;
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

    private async Task SendPlanNotificationsAsync(Booking booking, Business business, Service service)
    {
        var plan = business.Subscription?.Plan ?? SubscriptionPlan.Free;
        if (plan == SubscriptionPlan.Free) return;

        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == business.UserId);
        if (!string.IsNullOrWhiteSpace(owner?.Email))
        {
            await _emailService.SendNewBookingNotificationAsync(
                owner.Email,
                booking.ClientName,
                service.Name,
                booking.BookingDate,
                booking.StartTime,
                booking.ClientPhone);
        }

        if (plan != SubscriptionPlan.Pro || string.IsNullOrWhiteSpace(booking.ClientEmail))
            return;

        await _emailService.SendBookingConfirmationAsync(
            booking.ClientEmail,
            booking.ClientName,
            business.Name,
            service.Name,
            booking.BookingDate,
            booking.StartTime);
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
}
