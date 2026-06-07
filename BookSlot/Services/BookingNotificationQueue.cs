using System.Threading.Channels;
using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class BookingNotificationQueue : BackgroundService, IBookingNotificationQueue
{
    private readonly Channel<int> _queue = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingNotificationQueue> _logger;

    public BookingNotificationQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingNotificationQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ValueTask QueueAsync(int bookingId) =>
        _queue.Writer.WriteAsync(bookingId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var bookingId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendNotificationsAsync(bookingId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queued booking notification for booking {BookingId}", bookingId);
            }
        }
    }

    private async Task SendNotificationsAsync(int bookingId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var booking = await db.Bookings
            .Include(b => b.Business)
                .ThenInclude(b => b.Subscription)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
            return;

        var plan = IsActiveSubscription(booking.Business.Subscription)
            ? booking.Business.Subscription!.Plan
            : SubscriptionPlan.Free;
        if (plan == SubscriptionPlan.Free)
            return;

        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == booking.Business.UserId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(owner?.Email))
        {
            try
            {
                await emailService.SendNewBookingNotificationAsync(
                    owner.Email,
                    booking.ClientName,
                    booking.Service.Name,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.ClientPhone);
            }
            catch (EmailDeliveryException ex)
            {
                _logger.LogError(ex, "Failed to send owner notification for booking {BookingId}", booking.Id);
            }
        }

        if (plan != SubscriptionPlan.Pro || string.IsNullOrWhiteSpace(booking.ClientEmail))
            return;

        try
        {
            await emailService.SendBookingConfirmationAsync(
                booking.ClientEmail,
                booking.ClientName,
                booking.Business.Name,
                booking.Service.Name,
                booking.BookingDate,
                booking.StartTime);
        }
        catch (EmailDeliveryException ex)
        {
            _logger.LogError(ex, "Failed to send client confirmation for booking {BookingId}", booking.Id);
        }
    }

    private static bool IsActiveSubscription(Subscription? subscription) =>
        subscription is { IsActive: true } && !subscription.IsExpired;
}
