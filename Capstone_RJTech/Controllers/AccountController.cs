using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Controllers;

[Route("Account")]
public sealed class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserAuthenticationService _authenticationService;
    private readonly PasswordResetService _passwordResetService;
    private readonly PasswordHashService _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext db,
        UserAuthenticationService authenticationService,
        PasswordResetService passwordResetService,
        PasswordHashService passwordHasher,
        ILogger<AccountController> logger)
    {
        _db = db;
        _authenticationService = authenticationService;
        _passwordResetService = passwordResetService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    private static string SafeReturnUrl(string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl) || !UrlValidation.IsLocalUrl(returnUrl) ? "/" : returnUrl;

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(SafeReturnUrl(returnUrl));
        }

        ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
        return View(new LoginViewModel());
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authenticationService.AuthenticateAsync(
            model.Identifier,
            model.Password,
            HttpContext.RequestAborted);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt. Check your username/email and password.");
            return View(model);
        }

        await HttpContext.SignInAsync(
            UserAuthenticationService.AuthScheme,
            UserAuthenticationService.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        _logger.LogInformation("User {UserId} signed in.", user.UserID);
        return LocalRedirect(ViewData["ReturnUrl"] as string ?? "/");
    }

    [HttpGet("SignUp")]
    [AllowAnonymous]
    public IActionResult SignUp()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/");
        }

        return View(new SignUpViewModel());
    }

    [HttpPost("SignUp")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(
        SignUpViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedUsername = model.Username.Trim();
        var normalizedEmail = model.Email.Trim();

        if (await _db.Users.AnyAsync(user => user.Username == normalizedUsername, cancellationToken))
        {
            ModelState.AddModelError(nameof(SignUpViewModel.Username), "That username is already in use.");
        }

        if (await _db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            ModelState.AddModelError(nameof(SignUpViewModel.Email), "An account with that email already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            Username = normalizedUsername,
            Password = _passwordHasher.Hash(model.Password),
            Role = "Owner",
            DateCreated = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New account registered for {Email}.", normalizedEmail);
        TempData["AccountMessage"] = "Your account was created. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("ForgotPassword")]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
        => View(new ForgotPasswordViewModel());

    [HttpPost("ForgotPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var accountExists = await _passwordResetService.AccountExistsAsync(model.Email, cancellationToken);
        if (!accountExists)
        {
            ModelState.AddModelError(string.Empty, "We could not find an account with that email address. Check the email you registered with and try again.");
            return View(model);
        }

        var code = await _passwordResetService.CreateCodeAsync(model.Email, cancellationToken);
        if (code is null)
        {
            ModelState.AddModelError(string.Empty, "The verification code could not be sent. Verify the SMTP email settings (Email:Host, Email:Username, Email:Password), then try again.");
            return View(model);
        }

        TempData["AccountMessage"] = "A verification code was sent to the email on file.";
        return RedirectToAction(nameof(ForgotPasswordVerify), new { id = code.Id });
    }

    [HttpGet("ForgotPasswordVerify")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordVerify(string id, CancellationToken cancellationToken)
    {
        var code = await _passwordResetService.GetPendingRequestAsync(id, cancellationToken);
        if (code is null)
        {
            TempData["ToastError"] = "This password reset request is invalid or has expired. Please start again.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        ViewData["ResetEmail"] = MaskEmail(code.Email);
        return View(new VerifyOtpViewModel { RequestId = code.Id });
    }

    [HttpPost("ForgotPasswordVerify")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordVerify(
        VerifyOtpViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pending = await _passwordResetService.GetPendingRequestAsync(model.RequestId, cancellationToken);
            if (pending is null)
            {
                TempData["ToastError"] = "This password reset request is invalid or has expired. Please start again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewData["ResetEmail"] = MaskEmail(pending.Email);
            return View(model);
        }

        var verified = await _passwordResetService.VerifyCodeAsync(model.RequestId, model.Otp, cancellationToken);
        if (verified is null)
        {
            var pending = await _passwordResetService.GetPendingRequestAsync(model.RequestId, cancellationToken);
            if (pending is null)
            {
                TempData["ToastError"] = "This password reset request has expired. Please start again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            ModelState.AddModelError(nameof(VerifyOtpViewModel.Otp), "The code is incorrect or has expired. Check the code in your email and try again.");
            ViewData["ResetEmail"] = MaskEmail(pending.Email);
            return View(model);
        }

        TempData["AccountMessage"] = "Code confirmed. You can now create a new password.";
        return RedirectToAction(nameof(ResetPassword), new { id = verified.Id });
    }

    [HttpGet("ResetPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string id, CancellationToken cancellationToken)
    {
        var code = await _passwordResetService.GetVerifiedRequestAsync(id, cancellationToken);
        if (code is null)
        {
            TempData["ToastError"] = "This password reset request is invalid or has expired. Please start again.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { RequestId = code.Id });
    }

    [HttpPost("ResetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reset = await _passwordResetService.ResetPasswordAsync(model.RequestId, model.Password, cancellationToken);
        if (!reset)
        {
            TempData["ToastError"] = "This password reset request is invalid or has expired. Please start again.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData["AccountMessage"] = "Your password has been reset. You can now sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost("Logout")]
    [Authorize(AuthenticationSchemes = UserAuthenticationService.AuthScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(UserAuthenticationService.AuthScheme);
        return Redirect("/Account/Login");
    }

    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return email;
        }

        var head = email[..at];
        var maskedHead = head.Length <= 2
            ? head[..1] + "***"
            : head[..2] + "***";
        return maskedHead + email[at..];
    }

    private static class UrlValidation
    {
        public static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("/\\", StringComparison.Ordinal))
            {
                return false;
            }

            if (url.StartsWith('/'))
            {
                return true;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.IsFile;
        }
    }
}