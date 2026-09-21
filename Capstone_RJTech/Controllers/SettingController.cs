using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_RJTech.Controllers;

[Route("Setting")]
[Authorize(AuthenticationSchemes = UserAuthenticationService.AuthScheme)]
public sealed class SettingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserAuthenticationService _authService;
    private readonly PasswordHashService _passwordHasher;
    private readonly ILogger<SettingController> _logger;

    public SettingController(
        ApplicationDbContext db,
        UserAuthenticationService authService,
        PasswordHashService passwordHasher,
        ILogger<SettingController> logger)
    {
        _db = db;
        _authService = authService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // ── GET /Setting/AccountMenu ────────────────────────────────────────
    [HttpGet("AccountMenu")]
    public async Task<IActionResult> AccountMenu(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Forbid();

        var model = new AccountSettingsViewModel
        {
            Profile = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                DateCreated = user.DateCreated
            }
        };

        return View(model);
    }

    // ── POST /Setting/AccountMenu/UpdateProfile ─────────────────────────
    [HttpPost("AccountMenu/UpdateProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [Bind(Prefix = "Profile")] EditProfileViewModel model,
        CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Forbid();

        // Preserve display-only fields across validation failures
        model.Role = user.Role;
        model.DateCreated = user.DateCreated;

        if (!ModelState.IsValid)
        {
            return View("AccountMenu", new AccountSettingsViewModel { Profile = model });
        }

        var normalizedUsername = model.Username.Trim();
        var normalizedEmail = model.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername && u.UserID != user.UserID, ct))
        {
            ModelState.AddModelError($"Profile.{nameof(EditProfileViewModel.Username)}", "That username is already in use.");
        }

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail && u.UserID != user.UserID, ct))
        {
            ModelState.AddModelError($"Profile.{nameof(EditProfileViewModel.Email)}", "An account with that email already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("AccountMenu", new AccountSettingsViewModel { Profile = model });
        }

        user.FullName = model.FullName.Trim();
        user.Email = normalizedEmail;
        user.Username = normalizedUsername;

        await _db.SaveChangesAsync(ct);

        // Re-issue auth cookie so claims reflect updated values
        await RefreshSignInAsync(user);

        _logger.LogInformation("User {UserId} updated profile.", user.UserID);
        TempData["ToastSuccess"] = "Profile updated successfully.";
        return RedirectToAction(nameof(AccountMenu));
    }

    // ── POST /Setting/AccountMenu/ChangePassword ────────────────────────
    [HttpPost("AccountMenu/ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [Bind(Prefix = "Security")] ChangePasswordViewModel model,
        CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Forbid();

        var profileVm = new EditProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            DateCreated = user.DateCreated
        };

        if (!ModelState.IsValid)
        {
            return View("AccountMenu", new AccountSettingsViewModel { Profile = profileVm, Security = model });
        }

        if (!_passwordHasher.Verify(model.CurrentPassword, user.Password))
        {
            ModelState.AddModelError($"Security.{nameof(ChangePasswordViewModel.CurrentPassword)}", "Incorrect password.");
            return View("AccountMenu", new AccountSettingsViewModel { Profile = profileVm, Security = model });
        }

        user.Password = _passwordHasher.Hash(model.NewPassword);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} changed password.", user.UserID);
        TempData["ToastSuccess"] = "Password changed successfully.";
        return RedirectToAction(nameof(AccountMenu));
    }

    // ── Helpers ─────────────────────────────────────────────────────────
    private async Task<AppUser?> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _db.Users.FindAsync(new object[] { userId }, ct);
    }

    private async Task RefreshSignInAsync(AppUser user)
    {
        var authResult = await HttpContext.AuthenticateAsync(UserAuthenticationService.AuthScheme);
        var authProperties = authResult?.Properties ?? new AuthenticationProperties();

        await HttpContext.SignOutAsync(UserAuthenticationService.AuthScheme);
        await HttpContext.SignInAsync(
            UserAuthenticationService.AuthScheme,
            UserAuthenticationService.CreatePrincipal(user),
            authProperties);
    }
}