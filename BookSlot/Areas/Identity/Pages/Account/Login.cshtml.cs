using System.ComponentModel.DataAnnotations;
using BookSlot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Areas.Identity.Pages.Account;

[EnableRateLimiting("auth")]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = NormalizeReturnUrl(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var email = Input.Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(
            email, Input.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in: {Email}", email);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Невірний email або пароль");
        return Page();
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard/Index";
}
