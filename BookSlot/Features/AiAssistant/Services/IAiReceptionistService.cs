using BookSlot.Features.AiAssistant.Contracts;

namespace BookSlot.Features.AiAssistant.Services;

public interface IAiReceptionistService
{
    Task<AiAssistantReply> HandleAsync(
        AiReceptionistRequest request,
        CancellationToken cancellationToken = default);
}
