namespace BookSlot.Services;

public interface IBookingNotificationQueue
{
    ValueTask QueueAsync(int bookingId);
}
