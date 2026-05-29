namespace BookSlot.Features.AiAssistant.Services;

public interface IAiAvailabilityService
{
    Task<AiAvailabilityResult> GetAvailabilityAsync(
        int businessId,
        int serviceId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
