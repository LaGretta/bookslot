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

    /// <summary>
    /// Finds which business a chat is currently talking to (the most recently used one).
    /// Used in the single-bot model to route follow-up messages without a /start payload.
    /// </summary>
    Task<int?> FindBusinessByChatAsync(
        AiConversationChannel channel,
        string externalChatId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the configured welcome message for a business, or null if none/disabled.</summary>
    Task<string?> GetWelcomeMessageAsync(
        int businessId,
        CancellationToken cancellationToken = default);

    /// <summary>Active service names for a business — used to build tap-to-pick buttons.</summary>
    Task<List<string>> GetActiveServiceNamesAsync(
        int businessId,
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
