using BookSlot.Features.AiAssistant.Contracts;

namespace BookSlot.Features.AiAssistant.Services;

public class SafeAiConversationInterpreter : IAiConversationInterpreter
{
    private enum CustomerLanguage
    {
        English,
        Ukrainian,
        Russian
    }

    private static readonly string[] BookingKeywords =
    [
        "book",
        "booking",
        "appointment",
        "available",
        "availability",
        "free",
        "slot",
        "tomorrow",
        "today",
        "jutro",
        "dzisiaj",
        "\u0437\u0430\u0432\u0442\u0440\u0430",
        "\u0441\u044c\u043e\u0433\u043e\u0434\u043d\u0456",
        "\u0441\u0435\u0433\u043e\u0434\u043d\u044f",
        "\u0437\u0430\u043f\u0438\u0441",
        "\u0437\u0430\u043f\u0438\u0441\u0430\u0442\u0438",
        "\u043c\u043e\u0436\u043d\u0430",
        "\u0435\u0441\u0442\u044c"
    ];

    private static readonly string[] PriceKeywords =
    [
        "price",
        "cost",
        "how much",
        "cena",
        "ile kosztuje",
        "\u0446\u0456\u043d\u0430",
        "\u0441\u043a\u0456\u043b\u044c\u043a\u0438",
        "\u0446\u0435\u043d\u0430",
        "\u0441\u043a\u043e\u043b\u044c\u043a\u043e"
    ];

    private static readonly string[] CancelKeywords =
    [
        "cancel",
        "cancellation",
        "anuluj",
        "odwolaj",
        "\u0441\u043a\u0430\u0441\u0443\u0432\u0430\u0442\u0438",
        "\u0432\u0456\u0434\u043c\u0456\u043d\u0438\u0442\u0438",
        "\u043e\u0442\u043c\u0435\u043d\u0438\u0442\u044c"
    ];

    private static readonly string[] ChangeTimeKeywords =
    [
        "change",
        "reschedule",
        "another time",
        "different time",
        "przenies",
        "\u043f\u0435\u0440\u0435\u043d\u0435\u0441\u0442\u0438",
        "\u0437\u043c\u0456\u043d\u0438\u0442\u0438",
        "\u0438\u0437\u043c\u0435\u043d\u0438\u0442\u044c"
    ];

    public Task<AiAssistantReply> InterpretAsync(
        string customerMessage,
        AiBookingDraft currentDraft,
        CancellationToken cancellationToken = default)
    {
        var language = DetectLanguage(customerMessage);
        var intent = DetectIntent(customerMessage);
        if (intent == AiIntentType.Unknown && LooksLikeBookingContinuation(currentDraft))
            intent = AiIntentType.BookingRequest;

        var reply = new AiAssistantReply
        {
            Intent = intent,
            Draft = currentDraft,
            MessageToCustomer = BuildSafeMessage(intent, language),
            CanCreateBooking = false
        };

        if (!currentDraft.ServiceId.HasValue)
            reply.MissingFields.Add(nameof(AiBookingDraft.ServiceId));

        if (!currentDraft.RequestedDate.HasValue)
            reply.MissingFields.Add(nameof(AiBookingDraft.RequestedDate));

        if (!currentDraft.RequestedTime.HasValue)
            reply.MissingFields.Add(nameof(AiBookingDraft.RequestedTime));

        if (string.IsNullOrWhiteSpace(currentDraft.CustomerName))
            reply.MissingFields.Add(nameof(AiBookingDraft.CustomerName));

        if (string.IsNullOrWhiteSpace(currentDraft.CustomerContact))
            reply.MissingFields.Add(nameof(AiBookingDraft.CustomerContact));

        return Task.FromResult(reply);
    }

    private static AiIntentType DetectIntent(string customerMessage)
    {
        if (string.IsNullOrWhiteSpace(customerMessage))
            return AiIntentType.Unknown;

        var normalized = customerMessage.Trim().ToLowerInvariant();

        if (ContainsAny(normalized, CancelKeywords))
            return AiIntentType.CancelRequest;

        if (ContainsAny(normalized, ChangeTimeKeywords))
            return AiIntentType.ChangeTime;

        if (ContainsAny(normalized, PriceKeywords))
            return AiIntentType.AskPrice;

        if (ContainsAny(normalized, BookingKeywords))
            return AiIntentType.BookingRequest;

        return AiIntentType.Unknown;
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords) =>
        keywords.Any(value.Contains);

    private static bool LooksLikeBookingContinuation(AiBookingDraft draft) =>
        draft.ServiceId.HasValue ||
        draft.RequestedDate.HasValue ||
        draft.RequestedTime.HasValue ||
        !string.IsNullOrWhiteSpace(draft.CustomerName) ||
        !string.IsNullOrWhiteSpace(draft.CustomerContact);

    private static CustomerLanguage DetectLanguage(string customerMessage)
    {
        var normalized = customerMessage.ToLowerInvariant();
        if (normalized.Any(ch => ch is '\u0456' or '\u0457' or '\u0454' or '\u0491') ||
            ContainsAny(normalized, ["\u043f\u0440\u0438\u0432\u0456\u0442", "\u0434\u044f\u043a\u0443\u044e", "\u0441\u044c\u043e\u0433\u043e\u0434\u043d\u0456"]))
        {
            return CustomerLanguage.Ukrainian;
        }

        if (normalized.Any(ch => ch >= '\u0400' && ch <= '\u04ff'))
            return CustomerLanguage.Russian;

        return CustomerLanguage.English;
    }

    private static string BuildSafeMessage(AiIntentType intent, CustomerLanguage language) =>
        language switch
        {
            CustomerLanguage.Ukrainian => intent switch
            {
                AiIntentType.BookingRequest => "Так, я допоможу із записом. Мені ще потрібні послуга, дата, час, ім'я та контакт.",
                AiIntentType.AskPrice => "Можу підказати ціну, якщо зрозумію потрібну послугу.",
                AiIntentType.CancelRequest => "Розумію, схоже це скасування. Поки що скасування через AI ще не підключене.",
                AiIntentType.ChangeTime => "Розумію, схоже ви хочете змінити час. Перенесення запису через AI ще не підключене.",
                _ => "Я можу допомогти із записом. Напишіть, будь ласка, яку послугу, дату та час ви хочете."
            },
            CustomerLanguage.Russian => intent switch
            {
                AiIntentType.BookingRequest => "Да, я помогу с записью. Мне еще нужны услуга, дата, время, имя и контакт.",
                AiIntentType.AskPrice => "Могу подсказать цену, если пойму нужную услугу.",
                AiIntentType.CancelRequest => "Понимаю, похоже это отмена. Пока отмена через AI еще не подключена.",
                AiIntentType.ChangeTime => "Понимаю, похоже вы хотите изменить время. Перенос записи через AI еще не подключен.",
                _ => "Я могу помочь с записью. Напишите, пожалуйста, услугу, дату и время."
            },
            _ => intent switch
            {
                AiIntentType.BookingRequest => "I can help with booking. I still need the service, date, time, name, and contact before creating anything.",
                AiIntentType.AskPrice => "I can help with prices once I know the service.",
                AiIntentType.CancelRequest => "I understand this may be a cancellation request, but cancellation flow is not connected yet.",
                AiIntentType.ChangeTime => "I understand this may be a time change request, but rescheduling flow is not connected yet.",
                _ => "I can help with booking. Please send the service, date, and time."
            }
        };
}
