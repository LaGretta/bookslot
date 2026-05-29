using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Models;
using BookSlot.Services;

namespace BookSlot.Features.AiAssistant.Services;

public class AiBookingOrchestrator : IAiBookingOrchestrator
{
    private readonly BookingService _bookingService;

    public AiBookingOrchestrator(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public async Task<Booking?> TryCreateBookingAsync(
        AiBookingDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (!draft.HasMinimumBookingData)
            return null;

        var contact = draft.CustomerContact!.Trim();
        var clientEmail = LooksLikeEmail(contact) ? contact : "";
        var clientPhone = string.IsNullOrWhiteSpace(clientEmail) ? contact : "";

        return await _bookingService.CreateBookingAsync(
            draft.BusinessId!.Value,
            draft.ServiceId!.Value,
            draft.CustomerName!,
            clientPhone,
            clientEmail,
            draft.RequestedDate!.Value,
            draft.RequestedTime!.Value,
            draft.Notes);
    }

    private static bool LooksLikeEmail(string contact) =>
        contact.Contains('@') &&
        contact.Contains('.') &&
        !contact.StartsWith('@') &&
        contact.IndexOf('@') > 0;
}
