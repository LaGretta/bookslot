using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Features.AiAssistant.Services;

namespace BookSlot.Features.AiAssistant.Telegram;

public class SafeTelegramAssistantHandler : ITelegramAssistantHandler
{
    private readonly IAiConversationInterpreter _interpreter;
    private readonly IAiReceptionistService _receptionistService;
    private readonly IAiConversationStore _conversationStore;

    public SafeTelegramAssistantHandler(
        IAiConversationInterpreter interpreter,
        IAiReceptionistService receptionistService,
        IAiConversationStore conversationStore)
    {
        _interpreter = interpreter;
        _receptionistService = receptionistService;
        _conversationStore = conversationStore;
    }

    public async Task<TelegramAssistantResult> HandleUpdateAsync(
        TelegramUpdate update,
        int? businessId = null,
        bool requireEnabledSettings = false,
        CancellationToken cancellationToken = default)
    {
        var chatId = update.Message?.Chat?.Id;
        var text = update.Message?.Text;

        if (chatId == null || string.IsNullOrWhiteSpace(text))
        {
            return new TelegramAssistantResult
            {
                ExternalChatId = chatId,
                ShouldSendMessage = false
            };
        }

        var conversation = businessId.HasValue
            ? await _conversationStore.GetOrCreateAsync(
                businessId.Value,
                AiConversationChannel.Telegram,
                chatId.Value.ToString(),
                cancellationToken)
            : null;

        if (conversation != null)
        {
            await _conversationStore.AddMessageAsync(
                conversation.Id,
                AiMessageSenderType.Customer,
                text,
                cancellationToken);
        }

        var currentDraft = conversation != null
            ? await _conversationStore.GetDraftAsync(conversation.Id, cancellationToken)
            : new AiBookingDraft();

        EnrichDraftFromTelegramUser(currentDraft, update.Message?.From);

        var reply = businessId.HasValue
            ? await _receptionistService.HandleAsync(
                new AiReceptionistRequest
                {
                    BusinessId = businessId.Value,
                    CustomerMessage = text,
                    Draft = currentDraft,
                    RequireEnabledSettings = requireEnabledSettings
                },
                cancellationToken)
            : await _interpreter.InterpretAsync(
                text,
                currentDraft,
                cancellationToken);

        if (conversation != null)
        {
            await _conversationStore.AddMessageAsync(
                conversation.Id,
                AiMessageSenderType.Assistant,
                reply.MessageToCustomer,
                cancellationToken);

            await _conversationStore.SaveDraftAsync(
                conversation.Id,
                reply.Draft,
                reply.CanCreateBooking,
                cancellationToken);
        }

        return new TelegramAssistantResult
        {
            ConversationId = conversation?.Id,
            ExternalChatId = chatId,
            MessageToSend = reply.MessageToCustomer,
            ShouldSendMessage = !string.IsNullOrWhiteSpace(reply.MessageToCustomer),
            CanCreateBooking = reply.CanCreateBooking
        };
    }

    private static void EnrichDraftFromTelegramUser(
        AiBookingDraft draft,
        TelegramUser? user)
    {
        if (user == null)
            return;

        if (string.IsNullOrWhiteSpace(draft.CustomerContact) &&
            !string.IsNullOrWhiteSpace(user.Username))
        {
            draft.CustomerContact = $"@{user.Username.TrimStart('@')}";
        }

        if (!string.IsNullOrWhiteSpace(draft.CustomerName))
            return;

        var displayName = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

        if (!string.IsNullOrWhiteSpace(displayName))
            draft.CustomerName = displayName;
    }
}
