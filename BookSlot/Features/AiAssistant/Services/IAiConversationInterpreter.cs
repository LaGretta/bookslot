using BookSlot.Features.AiAssistant.Contracts;

namespace BookSlot.Features.AiAssistant.Services;

public interface IAiConversationInterpreter
{
    Task<AiAssistantReply> InterpretAsync(
        string customerMessage,
        AiBookingDraft currentDraft,
        CancellationToken cancellationToken = default);
}
