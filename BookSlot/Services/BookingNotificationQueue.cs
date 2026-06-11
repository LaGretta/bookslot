using System.Threading.Channels;
using BookSlot.Data;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Services;

public class BookingNotificationQueue : BackgroundService, IBookingNotificationQueue
{
    private readonly Channel<BookingNotificationWorkItem> _queue = Channel.CreateUnbounded<BookingNotificationWorkItem>(new UnboundedChannelOptions
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
        _queue.Writer.WriteAsync(BookingNotificationWorkItem.NewBooking(bookingId));

    public ValueTask QueueCancellationAsync(int bookingId) =>
        _queue.Writer.WriteAsync(BookingNotificationWorkItem.Cancellation(bookingId));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (item.Type == BookingNotificationWorkItemType.Cancellation)
                    await SendCancellationNotificationAsync(item.BookingId, stoppingToken);
                else
                    await SendNotificationsAsync(item.BookingId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queued {NotificationType} notification for booking {BookingId}", item.Type, item.BookingId);
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
                    booking.ClientPhone)
                    .WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            catch (EmailDeliveryException ex)
            {
                _logger.LogError(ex, "Failed to send owner notification for booking {BookingId}", booking.Id);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timed out sending owner notification for booking {BookingId}", booking.Id);
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
                booking.StartTime)
                .WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            _logger.LogError(ex, "Failed to send client confirmation for booking {BookingId}", booking.Id);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timed out sending client confirmation for booking {BookingId}", booking.Id);
        }
    }

    private async Task SendCancellationNotificationAsync(int bookingId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var booking = await db.Bookings
            .Include(b => b.Business)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null || string.IsNullOrWhiteSpace(booking.ClientEmail))
            return;

        try
        {
            await emailService.SendBookingCancelledAsync(
                booking.ClientEmail,
                booking.ClientName,
                booking.Business.Name,
                booking.BookingDate)
                .WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            _logger.LogError(ex, "Failed to send cancellation notification for booking {BookingId}", booking.Id);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timed out sending cancellation notification for booking {BookingId}", booking.Id);
        }
    }

    private static bool IsActiveSubscription(Subscription? subscription) =>
        subscription is { IsActive: true } && !subscription.IsExpired;

    private enum BookingNotificationWorkItemType
    {
        NewBooking,
        Cancellation
    }

    private sealed record BookingNotificationWorkItem(BookingNotificationWorkItemType Type, int BookingId)
    {
        public static BookingNotificationWorkItem NewBooking(int bookingId) =>
            new(BookingNotificationWorkItemType.NewBooking, bookingId);

        public static BookingNotificationWorkItem Cancellation(int bookingId) =>
            new(BookingNotificationWorkItemType.Cancellation, bookingId);
    }
}
