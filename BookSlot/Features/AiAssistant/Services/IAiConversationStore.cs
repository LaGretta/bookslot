using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;

namespace BookSlot.Features.AiAssistant.Services;

public interface IAiConversationStore
{
    Task<AiConversation?> GetOrCreateAsync(
        int businessId,
        AiConversationChannel channel,
        string externalChatId,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        int conversationId,
        AiMessageSenderType senderType,
        string text,
        CancellationToken cancellationToken = default);

    Task<AiBookingDraft> GetDraftAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    Task SaveDraftAsync(
        int conversationId,
        AiBookingDraft draft,
        bool canCreateBooking,
        CancellationToken cancellationToken = default);

    Task MarkDraftBookedAsync(
        int conversationId,
        CancellationToken cancellationToken = default);
}
