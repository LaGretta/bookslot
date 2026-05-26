using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BookSlot.Data;

namespace BookSlot.Pages;

public class IndexModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnGet()
    {
        return Page();
    }
}
