using System.Security.Claims;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services;

/// <summary>
/// Authenticates users stored in <see cref="AppUser"/> (tblUser) by username or email
/// plus password, and builds the claims principal used by the cookie authentication scheme.
/// </summary>
public sealed class UserAuthenticationService
{
    public const string AuthScheme = "AppCookie";

    private readonly ApplicationDbContext _db;
    private readonly PasswordHashService _passwordHasher;

    public UserAuthenticationService(ApplicationDbContext db, PasswordHashService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Finds the user whose username or email matches <paramref name="identifier"/> and
    /// verifies the password. Returns null when the account is unknown or the password
    /// is incorrect.
    /// </summary>
    public async Task<AppUser?> AuthenticateAsync(
        string identifier,
        string password,
        CancellationToken cancellationToken = default)
    {
        var key = identifier.Trim();
        var user = await _db.Users
            .SingleOrDefaultAsync(
                item => item.Username == key || item.Email == key,
                cancellationToken);

        if (user is null)
            return null;

        return _passwordHasher.Verify(password, user.Password) ? user : null;
    }

    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var identity = new ClaimsIdentity(AuthScheme, ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserID));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim("FullName", user.FullName));
        return new ClaimsPrincipal(identity);
    }
}