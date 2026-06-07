using System.ComponentModel.DataAnnotations;
using BookSlot.Data;
using BookSlot.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Areas.Identity.Pages.Account;

[EnableRateLimiting("auth")]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailVerificationCodeService _emailVerification;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        EmailVerificationCodeService emailVerification,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailVerification = emailVerification;
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
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
                return Page();
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, Input.Password);
            if (!passwordIsValid)
            {
                await _userManager.AccessFailedAsync(user);
                ModelState.AddModelError(string.Empty, "Невірний email або пароль");
                return Page();
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _emailVerification.SendCodeAsync(user);
            TempData["Success"] = "Email ще не підтверджено. Ми надіслали новий код на вашу пошту.";
            return RedirectToPage("./VerifyEmailCode", new { userId = user.Id, returnUrl });
        }

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

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Підтвердіть email перед входом.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Невірний email або пароль");
        return Page();
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard/Index";
}
