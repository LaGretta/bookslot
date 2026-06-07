using System.ComponentModel.DataAnnotations;
using BookSlot.Data;
using BookSlot.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BookSlot.Areas.Identity.Pages.Account;

[EnableRateLimiting("auth")]
public class VerifyEmailCodeModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly EmailVerificationCodeService _emailVerification;
    private readonly ILogger<VerifyEmailCodeModel> _logger;

    public VerifyEmailCodeModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        EmailVerificationCodeService emailVerification,
        ILogger<VerifyEmailCodeModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailVerification = emailVerification;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = "";

    public class InputModel
    {
        [Required]
        public string UserId { get; set; } = "";

        public string ReturnUrl { get; set; } = "/Dashboard/Index";

        [Required(ErrorMessage = "Введіть код з email")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Код має містити 6 цифр")]
        public string Code { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(string userId, string? returnUrl = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return RedirectToPage("./Register");

        if (await _userManager.IsEmailConfirmedAsync(user))
            return RedirectToPage("./Login", new { returnUrl });

        Input.UserId = user.Id;
        Input.ReturnUrl = NormalizeReturnUrl(returnUrl);
        Email = user.Email ?? "";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.ReturnUrl = NormalizeReturnUrl(Input.ReturnUrl);
        var user = await LoadUserAsync();
        if (user == null)
            return RedirectToPage("./Register");

        if (!ModelState.IsValid)
            return Page();

        if (await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
            return Page();
        }

        var isValid = await _emailVerification.VerifyCodeAsync(user, Input.Code);
        if (!isValid)
        {
            await _userManager.AccessFailedAsync(user);
            ModelState.AddModelError(string.Empty, "Код неправильний або вже закінчився. Перевірте пошту або надішліть новий код.");
            return Page();
        }

        user.EmailConfirmed = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _emailVerification.ClearCodeAsync(user);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation("User confirmed email: {Email}", user.Email);
        return LocalRedirect(Input.ReturnUrl);
    }

    public async Task<IActionResult> OnPostResendAsync(string userId, string? returnUrl = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return RedirectToPage("./Register");

        if (await _userManager.IsEmailConfirmedAsync(user))
            return RedirectToPage("./Login", new { returnUrl });

        if (await _userManager.IsLockedOutAsync(user))
        {
            Input.UserId = user.Id;
            Input.ReturnUrl = NormalizeReturnUrl(returnUrl);
            Email = user.Email ?? "";
            ModelState.AddModelError(string.Empty, "Забагато спроб. Спробуйте ще раз через 10 хвилин.");
            return Page();
        }

        await _emailVerification.SendCodeAsync(user);
        TempData["Success"] = "Новий код надіслано.";
        return RedirectToPage("./VerifyEmailCode", new { userId = user.Id, returnUrl });
    }

    private async Task<ApplicationUser?> LoadUserAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user != null)
            Email = user.Email ?? "";

        return user;
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard/Index";
}
