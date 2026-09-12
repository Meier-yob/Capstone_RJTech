using System.Security.Claims;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services;

public sealed class OwnerAuthenticationService
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHashService _passwordHasher;

    public OwnerAuthenticationService(ApplicationDbContext db, PasswordHashService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Owner?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var owner = await _db.Owners.SingleOrDefaultAsync(
            item => item.UserName == userName.Trim() && item.Role == "Owner",
            cancellationToken);

        if (owner is null)
            return null;

        return _passwordHasher.Verify(password, owner.PasswordHash)
            ? owner : null;
    }

    public static ClaimsPrincipal CreatePrincipal(Owner owner)
    {
        var identity = new ClaimsIdentity("OwnerCookie", ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, owner.OwnerID));
        identity.AddClaim(new Claim(ClaimTypes.Name, owner.UserName));
        identity.AddClaim(new Claim(ClaimTypes.Role, owner.Role));
        identity.AddClaim(new Claim("FullName", owner.FullName));
        return new ClaimsPrincipal(identity);
    }
}
