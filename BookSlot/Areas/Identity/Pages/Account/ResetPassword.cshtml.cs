using BookSlot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookSlot.Areas.Identity.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Success { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Введіть новий пароль")]
        [MinLength(6, ErrorMessage = "Мінімум 6 символів")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = "";

        public string Code { get; set; } = "";
    }

    public IActionResult OnGet(string? code, string? email)
    {
        if (code == null) return RedirectToPage("./Login");

        Input = new InputModel
        {
            Code  = code,
            Email = email ?? ""
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            Success = true; // Don't reveal user doesn't exist
            return Page();
        }

        var token  = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        var result = await _userManager.ResetPasswordAsync(user, token, Input.Password);

        if (result.Succeeded)
        {
            Success = true;
            return Page();
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
