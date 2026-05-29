using BookSlot.Data;
using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Features.AiAssistant.Services;

public class AiConversationStore : IAiConversationStore
{
    private readonly ApplicationDbContext _db;

    public AiConversationStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AiConversation?> GetOrCreateAsync(
        int businessId,
        AiConversationChannel channel,
        string externalChatId,
        CancellationToken cancellationToken = default)
    {
        var businessExists = await _db.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == businessId && b.IsActive, cancellationToken);

        if (!businessExists)
            return null;

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(
                c => c.BusinessId == businessId
                     && c.Channel == channel
                     && c.ExternalChatId == externalChatId,
                cancellationToken);

        if (conversation != null)
            return conversation;

        conversation = new AiConversation
        {
            BusinessId = businessId,
            Channel = channel,
            ExternalChatId = externalChatId,
            Status = AiConversationStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AiConversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    public async Task<int?> FindBusinessByChatAsync(
        AiConversationChannel channel,
        string externalChatId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AiConversations
            .AsNoTracking()
            .Where(c => c.Channel == channel && c.ExternalChatId == externalChatId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => (int?)c.BusinessId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetWelcomeMessageAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AiAssistantSettings
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .Select(s => s.WelcomeMessage)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddMessageAsync(
        int conversationId,
        AiMessageSenderType senderType,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return;

        _db.AiConversationMessages.Add(new AiConversationMessage
        {
            ConversationId = conversationId,
            SenderType = senderType,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiBookingDraft> GetDraftAsync(
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var appointmentDraft = await _db.AppointmentDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ConversationId == conversationId, cancellationToken);

        if (appointmentDraft == null)
            return new AiBookingDraft();

        return new AiBookingDraft
        {
            ServiceId = appointmentDraft.ServiceId,
            RequestedDate = appointmentDraft.RequestedDate,
            RequestedTime = appointmentDraft.RequestedTime,
            CustomerName = appointmentDraft.CustomerName,
            CustomerContact = appointmentDraft.CustomerContact
        };
    }

    public async Task SaveDraftAsync(
        int conversationId,
        AiBookingDraft draft,
        bool canCreateBooking,
        CancellationToken cancellationToken = default)
    {
        var appointmentDraft = await _db.AppointmentDrafts
            .FirstOrDefaultAsync(d => d.ConversationId == conversationId, cancellationToken);

        if (appointmentDraft == null)
        {
            appointmentDraft = new AppointmentDraft
            {
                ConversationId = conversationId,
                CreatedAt = DateTime.UtcNow
            };
            _db.AppointmentDrafts.Add(appointmentDraft);
        }

        appointmentDraft.ServiceId = draft.ServiceId;
        appointmentDraft.RequestedDate = draft.RequestedDate.HasValue
            ? DateTime.SpecifyKind(draft.RequestedDate.Value.Date, DateTimeKind.Utc)
            : null;
        appointmentDraft.RequestedTime = draft.RequestedTime;
        appointmentDraft.CustomerName = draft.CustomerName;
        appointmentDraft.CustomerContact = draft.CustomerContact;
        appointmentDraft.Status = canCreateBooking
            ? AppointmentDraftStatus.ReadyToBook
            : AppointmentDraftStatus.CollectingDetails;
        appointmentDraft.UpdatedAt = DateTime.UtcNow;

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.CustomerName = draft.CustomerName;
            conversation.CustomerContact = draft.CustomerContact;
            conversation.Status = canCreateBooking
                ? AiConversationStatus.BookingReady
                : AiConversationStatus.WaitingForCustomer;
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDraftBookedAsync(
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var appointmentDraft = await _db.AppointmentDrafts
            .FirstOrDefaultAsync(d => d.ConversationId == conversationId, cancellationToken);

        if (appointmentDraft != null)
        {
            appointmentDraft.Status = AppointmentDraftStatus.Booked;
            appointmentDraft.UpdatedAt = DateTime.UtcNow;
        }

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.Status = AiConversationStatus.Closed;
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
