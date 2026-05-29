using System.Text.RegularExpressions;
using BookSlot.Data;
using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Features.AiAssistant.Services;

public partial class AiReceptionistService : IAiReceptionistService
{
    private enum CustomerLanguage
    {
        English,
        Ukrainian,
        Russian
    }

    private readonly ApplicationDbContext _db;
    private readonly IAiConversationInterpreter _interpreter;
    private readonly IAiAvailabilityService _availabilityService;

    public AiReceptionistService(
        ApplicationDbContext db,
        IAiConversationInterpreter interpreter,
        IAiAvailabilityService availabilityService)
    {
        _db = db;
        _interpreter = interpreter;
        _availabilityService = availabilityService;
    }

    public async Task<AiAssistantReply> HandleAsync(
        AiReceptionistRequest request,
        CancellationToken cancellationToken = default)
    {
        var language = DetectLanguage(request.CustomerMessage);
        var business = await _db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == request.BusinessId && b.IsActive,
                cancellationToken);

        if (business == null)
        {
            return new AiAssistantReply
            {
                Intent = AiIntentType.Unknown,
                MessageToCustomer = Localize(
                    language,
                    "I could not find an active business for this request.",
                    "Не знайшов активний бізнес для цього запиту.",
                    "Не нашел активный бизнес для этого запроса."),
                CanCreateBooking = false
            };
        }

        if (request.RequireEnabledSettings)
        {
            var settingsEnabled = await _db.AiAssistantSettings
                .AsNoTracking()
                .AnyAsync(
                    s => s.BusinessId == business.Id && s.IsEnabled,
                    cancellationToken);

            if (!settingsEnabled)
            {
                return new AiAssistantReply
                {
                    Intent = AiIntentType.Unknown,
                    MessageToCustomer = Localize(
                        language,
                        "AI Receptionist is disabled for this business.",
                        "AI Receptionist вимкнений для цього бізнесу.",
                        "AI Receptionist выключен для этого бизнеса."),
                    CanCreateBooking = false
                };
            }
        }

        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.BusinessId == business.Id && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var draft = EnrichDraft(request.Draft, business.Id, services, request.CustomerMessage);
        var reply = await _interpreter.InterpretAsync(
            request.CustomerMessage,
            draft,
            cancellationToken);

        reply.Draft = draft;

        if (reply.Intent == AiIntentType.AskPrice)
            return BuildPriceReply(reply, services, language);

        if (reply.Intent != AiIntentType.BookingRequest)
            return reply;

        if (!draft.ServiceId.HasValue)
        {
            reply.MessageToCustomer = services.Count == 0
                ? Localize(
                    language,
                    "This business has no active services yet.",
                    "У цього бізнесу ще немає активних послуг.",
                    "У этого бизнеса пока нет активных услуг.")
                : Localize(
                    language,
                    $"Which service would you like? Available services: {string.Join(", ", services.Select(s => s.Name))}.",
                    $"Яку послугу хочете? Доступні послуги: {string.Join(", ", services.Select(s => s.Name))}.",
                    $"Какую услугу хотите? Доступные услуги: {string.Join(", ", services.Select(s => s.Name))}.");
            return reply;
        }

        if (!draft.RequestedDate.HasValue)
        {
            reply.MessageToCustomer = Localize(
                language,
                "What date would you like to book?",
                "На яку дату вас записати?",
                "На какую дату вас записать?");
            return reply;
        }

        var availability = await _availabilityService.GetAvailabilityAsync(
            business.Id,
            draft.ServiceId.Value,
            draft.RequestedDate.Value,
            cancellationToken);

        reply.SuggestedSlots = availability.AvailableSlots
            .Take(3)
            .Select(slot => slot.ToString(@"hh\:mm"))
            .ToList();

        if (!availability.HasAvailableSlots)
        {
            reply.MessageToCustomer = Localize(
                language,
                "There are no available slots for that service on this date.",
                "На цю дату для цієї послуги немає вільних слотів.",
                "На эту дату для этой услуги нет свободных слотов.");
            return reply;
        }

        if (!draft.RequestedTime.HasValue)
        {
            reply.MessageToCustomer = Localize(
                language,
                $"Available times: {string.Join(", ", reply.SuggestedSlots)}. Which time works best?",
                $"Є вільні години: {string.Join(", ", reply.SuggestedSlots)}. Який час вам зручний?",
                $"Есть свободное время: {string.Join(", ", reply.SuggestedSlots)}. Какое время вам удобно?");
            return reply;
        }

        var requestedTimeIsAvailable = availability.AvailableSlots.Contains(draft.RequestedTime.Value);
        if (!requestedTimeIsAvailable)
        {
            reply.MessageToCustomer = Localize(
                language,
                $"That time is not available. Available alternatives: {string.Join(", ", reply.SuggestedSlots)}.",
                $"Цей час вже недоступний. Можу запропонувати: {string.Join(", ", reply.SuggestedSlots)}.",
                $"Это время уже недоступно. Могу предложить: {string.Join(", ", reply.SuggestedSlots)}.");
            return reply;
        }

        reply.CanCreateBooking = draft.HasMinimumBookingData;
        reply.MessageToCustomer = reply.CanCreateBooking
            ? Localize(
                language,
                "All booking details are collected and the slot is available. Booking creation is still disabled in the safe foundation.",
                "Усі дані зібрані, слот вільний. У безпечному MVP запис ще підтверджує власник у dashboard.",
                "Все данные собраны, слот свободен. В безопасном MVP запись еще подтверждает владелец в dashboard.")
            : Localize(
                language,
                "The slot is available. I still need the customer's name and contact before creating anything.",
                "Цей час вільний. Напишіть, будь ласка, ваше ім'я та контакт.",
                "Это время свободно. Напишите, пожалуйста, ваше имя и контакт.");

        return reply;
    }

    private static AiBookingDraft EnrichDraft(
        AiBookingDraft draft,
        int businessId,
        List<Service> services,
        string customerMessage)
    {
        draft.BusinessId = businessId;

        var matchedService = ResolveService(services, customerMessage, draft.ServiceId);
        if (matchedService != null)
        {
            draft.ServiceId = matchedService.Id;
            draft.ServiceName = matchedService.Name;
        }

        draft.RequestedDate ??= TryParseRequestedDate(customerMessage);
        draft.RequestedTime ??= TryParseRequestedTime(customerMessage);
        draft.CustomerContact ??= TryParseCustomerContact(customerMessage);
        draft.CustomerName ??= TryParseCustomerName(customerMessage, draft);

        return draft;
    }

    private static Service? ResolveService(
        List<Service> services,
        string customerMessage,
        int? currentServiceId)
    {
        if (currentServiceId.HasValue)
            return services.FirstOrDefault(s => s.Id == currentServiceId.Value);

        if (string.IsNullOrWhiteSpace(customerMessage))
            return null;

        var normalizedMessage = customerMessage.ToLowerInvariant();
        return services.FirstOrDefault(service =>
            normalizedMessage.Contains(service.Name.ToLowerInvariant()) ||
            ServiceAliases(service.Name).Any(normalizedMessage.Contains));
    }

    private static string[] ServiceAliases(string serviceName)
    {
        var normalized = serviceName.ToLowerInvariant();
        if (normalized.Contains("manicure") ||
            normalized.Contains("\u043c\u0430\u043d\u0456\u043a\u044e\u0440") ||
            normalized.Contains("\u043c\u0430\u043d\u0438\u043a\u044e\u0440"))
        {
            return ["\u043c\u0430\u043d\u0456\u043a\u044e\u0440", "\u043c\u0430\u043d\u0438\u043a\u044e\u0440", "manicure"];
        }

        if (normalized.Contains("hair") ||
            normalized.Contains("cut") ||
            normalized.Contains("\u0441\u0442\u0440\u0438\u0436"))
        {
            return ["\u0441\u0442\u0440\u0438\u0436\u043a\u0430", "\u0441\u0442\u0440\u0438\u0436\u043a\u0443", "haircut"];
        }

        return [];
    }

    private static DateTime? TryParseRequestedDate(string customerMessage)
    {
        if (string.IsNullOrWhiteSpace(customerMessage))
            return null;

        var normalized = customerMessage.ToLowerInvariant();
        var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

        if (ContainsAny(normalized, ["today", "dzisiaj", "\u0441\u044c\u043e\u0433\u043e\u0434\u043d\u0456", "\u0441\u0435\u0433\u043e\u0434\u043d\u044f"]))
            return today;

        if (ContainsAny(normalized, ["tomorrow", "jutro", "\u0437\u0430\u0432\u0442\u0440\u0430"]))
            return today.AddDays(1);

        var isoDateMatch = IsoDatePattern().Match(customerMessage);
        if (isoDateMatch.Success)
        {
            var year = int.Parse(isoDateMatch.Groups["year"].Value);
            var month = int.Parse(isoDateMatch.Groups["month"].Value);
            var day = int.Parse(isoDateMatch.Groups["day"].Value);
            return DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
        }

        var localDateMatch = LocalDatePattern().Match(customerMessage);
        if (localDateMatch.Success)
        {
            var day = int.Parse(localDateMatch.Groups["day"].Value);
            var month = int.Parse(localDateMatch.Groups["month"].Value);
            var year = int.Parse(localDateMatch.Groups["year"].Value);
            return DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
        }

        return DateTime.TryParse(customerMessage, out var parsed)
            ? DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc)
            : null;
    }

    private static TimeSpan? TryParseRequestedTime(string customerMessage)
    {
        if (string.IsNullOrWhiteSpace(customerMessage))
            return null;

        var match = TimePattern().Match(customerMessage);
        if (!match.Success)
        {
            var bareHourMatch = BareHourPattern().Match(customerMessage.Trim());
            if (!bareHourMatch.Success)
                return null;

            var bareHour = int.Parse(bareHourMatch.Groups["hours"].Value);
            return new TimeSpan(bareHour, 0, 0);
        }

        var hours = int.Parse(match.Groups["hours"].Value);
        var minutes = int.Parse(match.Groups["minutes"].Value);

        return new TimeSpan(hours, minutes, 0);
    }

    private static string? TryParseCustomerContact(string customerMessage)
    {
        if (string.IsNullOrWhiteSpace(customerMessage))
            return null;

        var emailMatch = EmailPattern().Match(customerMessage);
        if (emailMatch.Success)
            return emailMatch.Value;

        var phoneMatch = PhonePattern().Match(customerMessage);
        if (!phoneMatch.Success)
            return null;

        var candidate = phoneMatch.Value.Trim();
        var digitCount = candidate.Count(char.IsDigit);
        return digitCount >= 9
            ? candidate
            : null;
    }

    private static string? TryParseCustomerName(string customerMessage, AiBookingDraft draft)
    {
        if (string.IsNullOrWhiteSpace(customerMessage))
            return null;

        var match = ExplicitNamePattern().Match(customerMessage);
        if (match.Success)
            return CleanCustomerName(match.Groups["name"].Value);

        if (!draft.ServiceId.HasValue ||
            !draft.RequestedDate.HasValue ||
            !draft.RequestedTime.HasValue)
        {
            return null;
        }

        var bareNameMatch = BareNamePattern().Match(customerMessage.Trim());
        return bareNameMatch.Success
            ? CleanCustomerName(bareNameMatch.Value)
            : null;
    }

    private static AiAssistantReply BuildPriceReply(
        AiAssistantReply reply,
        List<Service> services,
        CustomerLanguage language)
    {
        if (reply.Draft.ServiceId.HasValue)
        {
            var service = services.FirstOrDefault(s => s.Id == reply.Draft.ServiceId.Value);
            if (service != null)
            {
                reply.MessageToCustomer = Localize(
                    language,
                    $"{service.Name} costs {service.Price:0.##}.",
                    $"{service.Name}: {service.Price:0.##}.",
                    $"{service.Name}: {service.Price:0.##}.");
                return reply;
            }
        }

        reply.MessageToCustomer = services.Count == 0
            ? Localize(
                language,
                "This business has no active services yet.",
                "У цього бізнесу ще немає активних послуг.",
                "У этого бизнеса пока нет активных услуг.")
            : Localize(
                language,
                $"Available services: {string.Join(", ", services.Select(s => $"{s.Name} ({s.Price:0.##})"))}.",
                $"Доступні послуги: {string.Join(", ", services.Select(s => $"{s.Name} ({s.Price:0.##})"))}.",
                $"Доступные услуги: {string.Join(", ", services.Select(s => $"{s.Name} ({s.Price:0.##})"))}.");

        return reply;
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords) =>
        keywords.Any(value.Contains);

    private static string CleanCustomerName(string value)
    {
        var name = value.Trim().Trim('.', ',', '!', '?');
        return name.Length > 80
            ? name[..80]
            : name;
    }

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

    private static string Localize(
        CustomerLanguage language,
        string english,
        string ukrainian,
        string russian) =>
        language switch
        {
            CustomerLanguage.Ukrainian => ukrainian,
            CustomerLanguage.Russian => russian,
            _ => english
        };

    [GeneratedRegex(@"\b(?<hours>[01]?\d|2[0-3])[:.](?<minutes>[0-5]\d)\b")]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"^(?<hours>[01]?\d|2[0-3])$")]
    private static partial Regex BareHourPattern();

    [GeneratedRegex(@"\b(?<year>20\d{2})-(?<month>0?[1-9]|1[0-2])-(?<day>0?[1-9]|[12]\d|3[01])\b")]
    private static partial Regex IsoDatePattern();

    [GeneratedRegex(@"\b(?<day>0?[1-9]|[12]\d|3[01])[.\/](?<month>0?[1-9]|1[0-2])[.\/](?<year>20\d{2})\b")]
    private static partial Regex LocalDatePattern();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(?<!\w)\+?\d[\d\s\-()]{6,}\d(?!\w)")]
    private static partial Regex PhonePattern();

    [GeneratedRegex("\\b(?:my name is|name is|name|i am|i'm|im|\\u0456\\u043c'\\u044f|\\u043c\\u0435\\u043d\\u0435 \\u0437\\u0432\\u0430\\u0442\\u0438|nazywam si\\u0119|nazywam sie)\\s+(?<name>[\\p{L}\\s'-]{2,80})", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitNamePattern();

    [GeneratedRegex("^[\\p{L}][\\p{L}\\s'-]{1,79}$", RegexOptions.IgnoreCase)]
    private static partial Regex BareNamePattern();
}
