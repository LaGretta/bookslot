using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;
using BookSlot.Features.AiAssistant.Services;

namespace BookSlot.Features.AiAssistant.Telegram;

public class SafeTelegramAssistantHandler : ITelegramAssistantHandler
{
    private const string DefaultWelcome =
        "Привіт! Я допоможу обрати послугу та підготувати запис. Напишіть, будь ласка, що вас цікавить.";

    private readonly IAiReceptionistService _receptionistService;
    private readonly IAiConversationStore _conversationStore;

    public SafeTelegramAssistantHandler(
        IAiReceptionistService receptionistService,
        IAiConversationStore conversationStore)
    {
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
        var text = update.Message?.Text?.Trim();

        if (chatId == null || string.IsNullOrWhiteSpace(text))
        {
            return new TelegramAssistantResult
            {
                ExternalChatId = chatId,
                ShouldSendMessage = false
            };
        }

        var isStart = text.StartsWith("/start", StringComparison.OrdinalIgnoreCase);

        // ── Route to a business ─────────────────────────────────────────────
        // Single-bot model: figure out which business this chat belongs to.
        // 1) explicit businessId (legacy webhook path)
        // 2) /start <businessId> deep-link payload
        // 3) the business this chat last talked to
        var targetBusinessId = businessId
            ?? (isStart ? ParseStartPayload(text) : null)
            ?? await _conversationStore.FindBusinessByChatAsync(
                AiConversationChannel.Telegram,
                chatId.Value.ToString(),
                cancellationToken);

        if (targetBusinessId == null)
        {
            // Customer messaged the bot directly without opening a business link.
            return new TelegramAssistantResult
            {
                ExternalChatId = chatId,
                MessageToSend = "Привіт! Щоб записатися, відкрийте, будь ласка, посилання потрібного закладу.",
                ShouldSendMessage = true
            };
        }

        var conversation = await _conversationStore.GetOrCreateAsync(
            targetBusinessId.Value,
            AiConversationChannel.Telegram,
            chatId.Value.ToString(),
            cancellationToken);

        if (conversation == null)
        {
            return new TelegramAssistantResult
            {
                ExternalChatId = chatId,
                ShouldSendMessage = false
            };
        }

        await _conversationStore.AddMessageAsync(
            conversation.Id,
            AiMessageSenderType.Customer,
            text,
            cancellationToken);

        // ── /start → greet with the business welcome message and reset state ─
        if (isStart)
        {
            var welcome = await _conversationStore.GetWelcomeMessageAsync(
                targetBusinessId.Value, cancellationToken);
            if (string.IsNullOrWhiteSpace(welcome))
                welcome = DefaultWelcome;

            await _conversationStore.SaveDraftAsync(
                conversation.Id, new AiBookingDraft(), canCreateBooking: false, cancellationToken);

            await _conversationStore.AddMessageAsync(
                conversation.Id, AiMessageSenderType.Assistant, welcome, cancellationToken);

            var serviceButtons = await _conversationStore.GetActiveServiceNamesAsync(
                targetBusinessId.Value, cancellationToken);

            return new TelegramAssistantResult
            {
                ConversationId = conversation.Id,
                ExternalChatId = chatId,
                MessageToSend = welcome,
                ShouldSendMessage = true,
                QuickReplies = serviceButtons
            };
        }

        // ── Normal message → run the receptionist ───────────────────────────
        var currentDraft = await _conversationStore.GetDraftAsync(conversation.Id, cancellationToken);
        EnrichDraftFromTelegramUser(currentDraft, update.Message?.From);

        var reply = await _receptionistService.HandleAsync(
            new AiReceptionistRequest
            {
                BusinessId = targetBusinessId.Value,
                CustomerMessage = text,
                Draft = currentDraft,
                RequireEnabledSettings = requireEnabledSettings
            },
            cancellationToken);

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

        // Helpful tap-to-send buttons: pick a service while none is chosen, or pick a free slot.
        var quickReplies = new List<string>();
        if (reply.SuggestedSlots.Count > 0)
            quickReplies = reply.SuggestedSlots;
        else if (reply.Draft.ServiceId == null)
            quickReplies = await _conversationStore.GetActiveServiceNamesAsync(
                targetBusinessId.Value, cancellationToken);

        return new TelegramAssistantResult
        {
            ConversationId = conversation.Id,
            ExternalChatId = chatId,
            MessageToSend = reply.MessageToCustomer,
            ShouldSendMessage = !string.IsNullOrWhiteSpace(reply.MessageToCustomer),
            CanCreateBooking = reply.CanCreateBooking,
            QuickReplies = quickReplies
        };
    }

    private static int? ParseStartPayload(string text)
    {
        // "/start 8" or "/start=8" or "/start" → 8 / 8 / null
        var parts = text.Split(new[] { ' ', '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        return int.TryParse(parts[1].Trim(), out var businessId) ? businessId : null;
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
