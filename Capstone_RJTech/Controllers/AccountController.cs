using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_RJTech.Controllers;

[Route("Account")]
public sealed class AccountController : Controller
{
    private readonly OwnerAuthenticationService _authenticationService;
    private readonly IOwnerInvitationService _invitationService;
    private readonly ILogger<AccountController> _logger;
    private readonly ClerkSessionRejectionStore _rejectionStore;

    public AccountController(
        OwnerAuthenticationService authenticationService,
        IOwnerInvitationService invitationService,
        ILogger<AccountController> logger,
        ClerkSessionRejectionStore rejectionStore)
    {
        _authenticationService = authenticationService;
        _invitationService = invitationService;
        _logger = logger;
        _rejectionStore = rejectionStore;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // The Owner login must render normally for Owner flows even when the
        // browser also holds a Clerk session. Only when the visitor was actively
        // routed here from the app root or a Developer path (ReturnUrl="/" or
        // "/Developer/...") do we hand a valid SystemDeveloper session straight
        // to the Developer Portal, or send a non-SystemDeveloper Clerk session
        // to the access-denied page. A plain Owner logout or a direct visit to
        // /Account/Login always shows the Owner/Staff login form.
        var developerTargeted =
            returnUrl == "/" ||
            returnUrl?.StartsWith("/Developer", StringComparison.OrdinalIgnoreCase) == true;

        var clerkResult = await HttpContext.AuthenticateAsync("Clerk");

        // A Clerk session that was already denied (and is now dropped server-side)
        // must not strand the visitor on the Owner login. If they were routed here
        // from the root or a Developer path, send them to the Developer
        // access-denied page so they can switch accounts.
        var previouslyDeniedSession = HttpContext.Items[ClerkSessionRejectionStore.RejectedSessionItemKey] as string;
        if (developerTargeted && !string.IsNullOrWhiteSpace(previouslyDeniedSession))
        {
            _logger.LogWarning(
                "Clerk session {SessionId} was previously denied and is being routed through the Owner root; " +
                "sending to the Developer access-denied page.",
                previouslyDeniedSession);
            TempData["DeveloperAccessDenied"] =
                "Access Denied: Your account does not have System Developer permissions. Please sign in with an authorized account.";
            return RedirectToAction(nameof(DeveloperController.Login), "Developer", new { accessDenied = "true" });
        }

        if (clerkResult.Succeeded &&
            clerkResult.Principal?.IsInRole("SystemDeveloper") == true &&
            developerTargeted)
        {
            _logger.LogInformation(
                "Owner login: valid SystemDeveloper Clerk session {SessionId} for subject {Subject}; " +
                "bouncing ReturnUrl={ReturnUrl} to the Developer portal.",
                clerkResult.Principal?.FindFirst("sid")?.Value ?? "(none)",
                clerkResult.Principal?.FindFirst("sub")?.Value ?? "(none)",
                returnUrl ?? "(empty)");
            return RedirectToAction("Invitations", "Developer");
        }

        // A valid Clerk session that is NOT a SystemDeveloper must not be left
        // dangling on the Owner login (it locks the user out and blocks switching
        // accounts). Terminate it and send the visitor to the Developer login,
        // which shows a dismissible Access Denied alert.
        if (clerkResult.Succeeded &&
            clerkResult.Principal?.IsInRole("SystemDeveloper") != true &&
            developerTargeted)
        {
            _logger.LogWarning(
                "Clerk account {Subject} reached the Owner login without the SystemDeveloper role; terminating its Clerk session.",
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
            return RedirectToAction(nameof(DeveloperController.Login), "Developer", new { accessDenied = "true" });
        }

        _logger.LogInformation(
            "Owner login page rendered. ReturnUrl={ReturnUrl}, Clerk session present={HasClerk}, " +
            "SystemDeveloper={IsDev}, developerTargeted={Targeted}.",
            returnUrl ?? "(empty)",
            clerkResult.Succeeded,
            clerkResult.Principal?.IsInRole("SystemDeveloper"),
            developerTargeted);

        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return View();
    }

    private async Task TerminateClerkSessionAsync()
    {
        // The JWT Bearer scheme keeps no server-side session state; terminating
        // the Clerk session here means clearing its session cookie on the browser.
        // The Clerk SDK on the Developer login page then revokes the session at
        // Clerk and clears any remaining client-side state.
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

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string userName,
        string password,
        bool rememberMe = false,
        string? returnUrl = null)
    {
        var owner = await _authenticationService.AuthenticateAsync(userName, password, cancellationToken: HttpContext.RequestAborted);
        if (owner is not null)
        {
            await HttpContext.SignInAsync("OwnerCookie", OwnerAuthenticationService.CreatePrincipal(owner),
                new AuthenticationProperties { IsPersistent = rememberMe });
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return View();
    }

    [HttpPost("Logout")]
    [Authorize(AuthenticationSchemes = "OwnerCookie")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("OwnerCookie");
        return Redirect("/Account/Login");
    }

    [HttpGet("AcceptInvitation")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(
        string? token,
        CancellationToken cancellationToken)
    {
        var validation = await _invitationService.ValidateAsync(token ?? string.Empty, cancellationToken);
        if (!validation.IsValid || validation.Invitation is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("InvitationError", validation.Error ?? "The invitation is invalid.");
        }

        return View(new AcceptInvitationViewModel
        {
            Token = token!,
            Email = validation.Invitation.Email
        });
    }

    [HttpPost("CreateOwnerAccount")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOwnerAccount(
        CreateOwnerAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var validation = await _invitationService.ValidateAsync(model.Token, cancellationToken);
            ViewData["InvitationEmail"] = validation.Invitation?.Email;
            return View("CreateOwnerAccount", model);
        }

        var result = await _invitationService.CreateOwnerAccountAsync(
            model.Token,
            model.FullName,
            model.UserName,
            model.Password,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var validation = await _invitationService.ValidateAsync(model.Token, cancellationToken);
            ViewData["InvitationEmail"] = validation.Invitation?.Email;
            return View("CreateOwnerAccount", model);
        }

        TempData["AccountMessage"] = "Your Owner account was created. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => Forbid();
}