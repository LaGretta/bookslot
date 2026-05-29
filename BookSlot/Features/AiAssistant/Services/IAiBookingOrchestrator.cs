using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Models;

namespace BookSlot.Features.AiAssistant.Services;

public interface IAiBookingOrchestrator
{
    Task<Booking?> TryCreateBookingAsync(
        AiBookingDraft draft,
        CancellationToken cancellationToken = default);
}
