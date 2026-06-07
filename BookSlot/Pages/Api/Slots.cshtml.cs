using BookSlot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Pages.Api;

public class SlotsModel : PageModel
{
    private readonly BookingService _bookingService;

    public SlotsModel(BookingService bookingService) => _bookingService = bookingService;

    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> OnGetAsync(int businessId, int serviceId, string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return new JsonResult(Array.Empty<string>());

        var slots = await _bookingService.GetAvailableSlotsAsync(businessId, serviceId, parsedDate);
        var formatted = slots.Select(s => s.ToString(@"hh\:mm")).ToList();
        return new JsonResult(formatted);
    }
}
