using BookSlot.Features.AiAssistant.Contracts;
using BookSlot.Features.AiAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookSlot.Pages.Api;

public class AiAssistantDebugModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IAiConversationInterpreter _interpreter;
    private readonly IAiReceptionistService _receptionistService;

    public AiAssistantDebugModel(
        IWebHostEnvironment environment,
        IAiConversationInterpreter interpreter,
        IAiReceptionistService receptionistService)
    {
        _environment = environment;
        _interpreter = interpreter;
        _receptionistService = receptionistService;
    }

    public async Task<IActionResult> OnGetAsync(
        string? message,
        int? businessId,
        int? serviceId,
        string? serviceName,
        string? date,
        string? time,
        string? customerName,
        string? customerContact,
        string? notes)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var draft = new AiBookingDraft
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            ServiceName = serviceName,
            RequestedDate = DateTime.TryParse(date, out var parsedDate) ? parsedDate : null,
            RequestedTime = TimeSpan.TryParse(time, out var parsedTime) ? parsedTime : null,
            CustomerName = customerName,
            CustomerContact = customerContact,
            Notes = notes
        };

        var reply = businessId.HasValue
            ? await _receptionistService.HandleAsync(
                new AiReceptionistRequest
                {
                    BusinessId = businessId.Value,
                    CustomerMessage = message ?? "",
                    Draft = draft
                },
                HttpContext.RequestAborted)
            : await _interpreter.InterpretAsync(
                message ?? "",
                draft,
                HttpContext.RequestAborted);

        return new JsonResult(new
        {
            environment = _environment.EnvironmentName,
            message = message ?? "",
            reply.Intent,
            reply.MessageToCustomer,
            reply.MissingFields,
            reply.SuggestedSlots,
            reply.CanCreateBooking,
            draft = reply.Draft
        });
    }
}
