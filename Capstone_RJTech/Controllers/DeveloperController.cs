using Capstone_RJTech.ViewModels;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_RJTech.Controllers;

[Route("Developer")]
[Authorize(AuthenticationSchemes = "Clerk", Policy = "SystemDeveloper")]
public class DeveloperController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IOwnerInvitationService _invitationService;
    private readonly ILogger<DeveloperController> _logger;
    private readonly ClerkSessionRejectionStore _rejectionStore;

    public DeveloperController(
        IConfiguration configuration,
        IOwnerInvitationService invitationService,
        ILogger<DeveloperController> logger,
        ClerkSessionRejectionStore rejectionStore)
    {
        _configuration = configuration;
        _invitationService = invitationService;
        _logger = logger;
        _rejectionStore = rejectionStore;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var localReturnUrl = Url.IsLocalUrl(returnUrl) &&
            returnUrl.StartsWith("/Developer", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/Developer/Invitations";
        var clerkResult = await HttpContext.AuthenticateAsync("Clerk");

        var roleClaims = (clerkResult.Principal?.FindAll("role") ?? Enumerable.Empty<Claim>())
            .Concat(clerkResult.Principal?.FindAll(ClaimTypes.Role) ?? Enumerable.Empty<Claim>())
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation(
            "Developer login completion: Succeeded={Succeeded}, Failure={Failure}, " +
            "RoleClaims={Roles}, HasSystemDeveloperRole={HasRole}",
            clerkResult.Succeeded,
            clerkResult.Failure?.Message ?? "(none)",
            roleClaims.Length == 0 ? "(none)" : string.Join(", ", roleClaims),
            clerkResult.Principal?.IsInRole("SystemDeveloper"));

        if (clerkResult.Succeeded &&
            clerkResult.Principal?.IsInRole("SystemDeveloper") == true)
        {
            _logger.LogInformation("Developer login succeeded; redirecting to {Target}", localReturnUrl);
            return LocalRedirect(localReturnUrl);
        }

        if (clerkResult.Succeeded)
        {
            // Clerk session is valid but the account is not a System Developer.
            // Terminate the session (backend cookie + Clerk SDK on the client) so
            // the account is not locked out and the login screen can be reused
            // with a different account.
            _logger.LogWarning(
                "Clerk account {Subject} is authenticated but is not a SystemDeveloper; terminating its session.",
                clerkResult.Principal?.FindFirst("sub")?.Value ?? "(unknown)");

            // Permanently deny this Clerk session id: even if the Clerk client
            // re-issues the session cookie, the server will treat it as
            // unauthenticated (see OnMessageReceived in Program.cs).
            var rejectedSessionId = clerkResult.Principal?.FindFirst("sid")?.Value;
            if (!string.IsNullOrWhiteSpace(rejectedSessionId))
            {
                _rejectionStore.MarkRejected(rejectedSessionId);
            }

            await TerminateClerkSessionAsync();

            TempData["DeveloperAccessDenied"] =
                "Access Denied: Your account does not have System Developer permissions. Please sign in with an authorized account.";
            // accessDenied is also carried in the URL: Clerk's signOut() can reload the
            // page (consuming TempData), and the query flag keeps the Alert page's
            // redirect guard active so a restored Clerk session cannot bounce it.
            return RedirectToAction(nameof(Login), "Developer", new { accessDenied = "true" });
        }

        if (clerkResult.Failure is not null)
        {
            // The browser holds a Clerk session the server could not validate
            // (e.g. expired or invalid token). Ask the developer to sign in again.
            ViewData["AuthorizationError"] =
                "Your Clerk session could not be validated by the server. Please sign in again with the authorized System Developer account.";
        }

        // The request carried a previously-denied Clerk session that the server
        // dropped (see OnMessageReceived in Program.cs). The page renders stably,
        // but the Access Denied alert must still be shown so the visitor knows why
        // they are here and can switch accounts.
        if (HttpContext.Items[ClerkSessionRejectionStore.RejectedSessionItemKey] is string rejectedRender)
        {
            TempData["DeveloperAccessDenied"] =
                "Access Denied: Your account does not have System Developer permissions. Please sign in with an authorized account.";
        }

        ViewData["ReturnUrl"] = localReturnUrl;
        ViewData["ClerkPublishableKey"] = _configuration["Clerk:PublishableKey"];
        ViewData["ClerkAuthority"] = _configuration["Clerk:Authority"];
        return View();
    }

    private async Task TerminateClerkSessionAsync()
    {
        // The JWT Bearer scheme keeps no server-side session state; terminating
        // the Clerk session here means clearing its session cookie on the browser.
        // The Clerk SDK on the login page then revokes the session at Clerk and
        // clears any remaining client-side state.
        try
        {
            await HttpContext.SignOutAsync("Clerk");
        }
        catch (InvalidOperationException)
        {
            // JWT Bearer does not support scheme sign-out; the cookie clearing
            // below is what terminates the session on the backend.
        }

        if (HttpContext.Request.Cookies.ContainsKey("__session"))
        {
            HttpContext.Response.Cookies.Delete("__session");
        }
    }

    [HttpGet("Invitations")]
    [Authorize(AuthenticationSchemes = "Clerk", Policy = "SystemDeveloper")]
    public async Task<IActionResult> Invitations(CancellationToken cancellationToken)
    {
        var invitations = await _invitationService.GetInvitationsAsync(cancellationToken);
        var viewModel = new InvitationListViewModel
        {
            Invitations = invitations.Select(invitation => new DeveloperInvitationViewModel
            {
                InvitationID = invitation.InvitationID,
                Email = invitation.Email,
                CreatedAt = invitation.CreatedAt,
                ExpiresAt = invitation.ExpiresAt,
                Status = invitation.Status.ToString()
            }).ToArray()
        };

        return View(viewModel);
    }

    [HttpPost("SendInvitation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvitation(
        [Bind(Prefix = "NewInvitation")] SendInvitationViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvitationsWithModelAsync(model, cancellationToken);
        }

        var clerkUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(clerkUserId))
        {
            return Forbid();
        }

        var result = await _invitationService.CreateAsync(
            model.Email,
            clerkUserId,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["InvitationError"] = result.Error;
        }
        else
        {
            TempData["InvitationSuccess"] = "The invitation was sent successfully.";
        }

        return RedirectToAction(nameof(Invitations));
    }

    [HttpPost("ResendInvitation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation(
        InvitationActionViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.InvitationID <= 0)
        {
            return BadRequest("A valid invitation ID is required.");
        }

        var clerkUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(clerkUserId))
        {
            return Forbid();
        }

        var result = await _invitationService.ResendAsync(
            model.InvitationID,
            clerkUserId,
            cancellationToken);

        TempData[result.Succeeded ? "InvitationSuccess" : "InvitationError"] =
            result.Succeeded ? "The invitation was resent successfully." : result.Error;
        return RedirectToAction(nameof(Invitations));
    }

    [HttpPost("RevokeInvitation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeInvitation(
        InvitationActionViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.InvitationID <= 0)
        {
            return BadRequest("A valid invitation ID is required.");
        }

        var result = await _invitationService.RevokeAsync(model.InvitationID, cancellationToken);
        TempData[result.Succeeded ? "InvitationSuccess" : "InvitationError"] =
            result.Succeeded ? "The invitation was revoked." : result.Error;
        return RedirectToAction(nameof(Invitations));
    }

    // GET + POST: the access-denied login page links to /Developer/Logout as a
    // plain link for users whose Clerk session could not be revoked client-side,
    // so this must be reachable without a form POST. AllowAnonymous: a signed-out
    // (or denied) user must be able to reach the sign-out page.
    [HttpGet("Logout")]
    [HttpPost("Logout")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        ViewData["ClerkPublishableKey"] = _configuration["Clerk:PublishableKey"];
        ViewData["ClerkAuthority"] = _configuration["Clerk:Authority"];
        return View();
    }

    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task<IActionResult> InvitationsWithModelAsync(
        SendInvitationViewModel model,
        CancellationToken cancellationToken)
    {
        var invitations = await _invitationService.GetInvitationsAsync(cancellationToken);
        var viewModel = new InvitationListViewModel
        {
            Invitations = invitations.Select(invitation => new DeveloperInvitationViewModel
            {
                InvitationID = invitation.InvitationID,
                Email = invitation.Email,
                CreatedAt = invitation.CreatedAt,
                ExpiresAt = invitation.ExpiresAt,
                Status = invitation.Status.ToString()
            }).ToArray(),
            NewInvitation = model
        };

        return View("Invitations", viewModel);
    }
}
