using System.ComponentModel.DataAnnotations;
using BookSlot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Areas.Identity.Pages.Account;

[EnableRateLimiting("auth")]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
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
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Мінімум 6 символів")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = "";
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
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(string.Empty, "Акаунт з таким email вже існує. Увійдіть або використайте іншу пошту.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User registered: {Email}", email);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));

        return Page();
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard/Index";

    private static string TranslateIdentityError(IdentityError error) =>
        error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Акаунт з таким email вже існує. Увійдіть або використайте іншу пошту.",
            "PasswordTooShort" => "Пароль має містити мінімум 6 символів.",
            "PasswordRequiresDigit" => "Пароль має містити хоча б одну цифру.",
            "PasswordRequiresNonAlphanumeric" => "Пароль має містити хоча б один спеціальний символ.",
            "PasswordRequiresUpper" => "Пароль має містити хоча б одну велику літеру.",
            "PasswordRequiresLower" => "Пароль має містити хоча б одну малу літеру.",
            "InvalidEmail" => "Невірний формат email.",
            "InvalidUserName" => "Email містить недопустимі символи.",
            _ => error.Description
        };
}
