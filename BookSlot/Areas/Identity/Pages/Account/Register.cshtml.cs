using System.ComponentModel.DataAnnotations;
using BookSlot.Data;
using BookSlot.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Areas.Identity.Pages.Account;

[EnableRateLimiting("auth")]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailVerificationCodeService _emailVerification;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        EmailVerificationCodeService emailVerification,
        ILogger<RegisterModel> logger)
    {
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

        var validation = await _emailVerification.ValidateNewRegistrationAsync(email, Input.Password);
        if (!validation.Succeeded)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        try
        {
            var pending = await _emailVerification.CreatePendingRegistrationAsync(email, Input.Password);
            _logger.LogInformation("Pending registration created for {Email}", email);

            TempData["Success"] = "Ми надіслали 6-значний код на вашу пошту.";
            return RedirectToPage("./VerifyEmailCode", new { pendingId = pending.Id, returnUrl });
        }
        catch (EmailDeliveryException ex)
        {
            _logger.LogError(ex, "Failed to send registration code to {Email}", email);
            ModelState.AddModelError(string.Empty, "Не вдалося надіслати код на пошту. Перевірте email або спробуйте ще раз трохи пізніше.");
            return Page();
        }
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard/Index";
}
