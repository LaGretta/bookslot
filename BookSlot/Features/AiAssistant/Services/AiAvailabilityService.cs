using BookSlot.Services;

namespace BookSlot.Features.AiAssistant.Services;

public class AiAvailabilityService : IAiAvailabilityService
{
    private readonly BookingService _bookingService;

    public AiAvailabilityService(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public async Task<AiAvailabilityResult> GetAvailabilityAsync(
        int businessId,
        int serviceId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var slots = await _bookingService.GetAvailableSlotsAsync(
            businessId,
            serviceId,
            date);

        return new AiAvailabilityResult
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            AvailableSlots = slots
        };
    }
}
