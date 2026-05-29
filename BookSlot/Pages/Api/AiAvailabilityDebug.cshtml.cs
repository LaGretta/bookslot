using BookSlot.Features.AiAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookSlot.Pages.Api;

public class AiAvailabilityDebugModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IAiAvailabilityService _availabilityService;

    public AiAvailabilityDebugModel(
        IWebHostEnvironment environment,
        IAiAvailabilityService availabilityService)
    {
        _environment = environment;
        _availabilityService = availabilityService;
    }

    public async Task<IActionResult> OnGetAsync(int businessId, int serviceId, string date)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest("A valid date is required.");

        var result = await _availabilityService.GetAvailabilityAsync(
            businessId,
            serviceId,
            parsedDate,
            HttpContext.RequestAborted);

        return new JsonResult(new
        {
            environment = _environment.EnvironmentName,
            result.BusinessId,
            result.ServiceId,
            date = result.Date.ToString("yyyy-MM-dd"),
            hasAvailableSlots = result.HasAvailableSlots,
            availableSlots = result.AvailableSlots.Select(slot => slot.ToString(@"hh\:mm"))
        });
    }
}
