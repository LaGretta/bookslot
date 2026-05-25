using BookSlot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace BookSlot.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public bool EmailSent { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        public string Email { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);

        // Always show "sent" to not reveal which emails exist
        if (user == null)
        {
            EmailSent = true;
            return Page();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var code  = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", code, email = Input.Email },
            protocol: Request.Scheme)!;

        await _emailSender.SendEmailAsync(
            Input.Email,
            "Скидання пароля — BookSlot",
            $@"<div style='font-family:Arial,sans-serif;max-width:520px;margin:0 auto'>
                <div style='background:#4F46E5;padding:28px;text-align:center;border-radius:12px 12px 0 0'>
                    <h2 style='color:white;margin:0;font-size:1.4rem'>🔐 Скидання пароля</h2>
                </div>
                <div style='padding:28px;background:#f9f9f9;border-radius:0 0 12px 12px'>
                    <p>Ти отримав цей лист, тому що запросив скидання пароля для свого акаунту BookSlot.</p>
                    <div style='text-align:center;margin:24px 0'>
                        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'
                           style='background:#4F46E5;color:white;padding:14px 32px;border-radius:10px;text-decoration:none;font-weight:700;font-size:1rem;display:inline-block'>
                            Скинути пароль
                        </a>
                    </div>
                    <p style='color:#999;font-size:.85rem'>Якщо ти не робив цього запиту — просто проігноруй цей лист.</p>
                </div>
            </div>");

        EmailSent = true;
        return Page();
    }
}
