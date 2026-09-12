using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace Capstone_RJTech.Services;

public interface IOwnerInvitationService
{
    Task<IReadOnlyList<OwnerInvitation>> GetInvitationsAsync(CancellationToken cancellationToken = default);

    Task<InvitationOperationResult> CreateAsync(
        string email,
        string createdByClerkUserId,
        CancellationToken cancellationToken = default);

    Task<InvitationOperationResult> ResendAsync(
        int invitationId,
        string createdByClerkUserId,
        CancellationToken cancellationToken = default);

    Task<InvitationOperationResult> RevokeAsync(
        int invitationId,
        CancellationToken cancellationToken = default);

    Task<InvitationValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<OwnerAccountCreationResult> CreateOwnerAccountAsync(
        string token,
        string fullName,
        string userName,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record InvitationOperationResult(bool Succeeded, string? Error = null);

public sealed record InvitationValidationResult(
    bool IsValid,
    OwnerInvitation? Invitation = null,
    string? Error = null);

public sealed record OwnerAccountCreationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);

public sealed class OwnerInvitationService : IOwnerInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _dbContext;
    private readonly PasswordHashService _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OwnerInvitationService> _logger;

    public OwnerInvitationService(
        ApplicationDbContext dbContext,
        PasswordHashService passwordHasher,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<OwnerInvitationService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OwnerInvitation>> GetInvitationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = await _dbContext.OwnerInvitations
            .Where(invitation => invitation.Status == OwnerInvitationStatus.Pending &&
                                 invitation.ExpiresAt <= now &&
                                 invitation.UsedAt == null)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            foreach (var invitation in expired)
            {
                invitation.Status = OwnerInvitationStatus.Expired;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await _dbContext.OwnerInvitations
            .AsNoTracking()
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvitationOperationResult> CreateAsync(
        string email,
        string createdByClerkUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var existingUser = await _dbContext.Owners.AnyAsync(owner => owner.RecoveryEmail == normalizedEmail, cancellationToken);
        if (existingUser)
        {
            return new(false, "An account already exists for this email address.");
        }

        var now = DateTimeOffset.UtcNow;
        await ExpirePendingInvitationsAsync(normalizedEmail, now, cancellationToken);

        var hasPendingInvitation = await _dbContext.OwnerInvitations.AnyAsync(
            invitation => invitation.Email == normalizedEmail &&
                          invitation.Status == OwnerInvitationStatus.Pending &&
                          invitation.UsedAt == null &&
                          invitation.ExpiresAt > now,
            cancellationToken);

        if (hasPendingInvitation)
        {
            return new(false, "A valid pending invitation already exists for this email address.");
        }

        var rawToken = GenerateToken();
        var invitation = new OwnerInvitation
        {
            Email = normalizedEmail,
            TokenHash = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.Add(InvitationLifetime),
            Status = OwnerInvitationStatus.Pending,
            CreatedByClerkUserId = createdByClerkUserId
        };

        _dbContext.OwnerInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await SendInvitationAsync(invitation, rawToken, cancellationToken);
    }

    public async Task<InvitationOperationResult> ResendAsync(
        int invitationId,
        string createdByClerkUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var oldInvitation = await _dbContext.OwnerInvitations
            .SingleOrDefaultAsync(invitation => invitation.InvitationID == invitationId, cancellationToken);

        if (oldInvitation is null)
        {
            return new(false, "Invitation was not found.");
        }

        if (oldInvitation.Status == OwnerInvitationStatus.Used || oldInvitation.UsedAt is not null)
        {
            return new(false, "A used invitation cannot be resent.");
        }

        var existingUser = await _dbContext.Owners.AnyAsync(owner => owner.RecoveryEmail == oldInvitation.Email, cancellationToken);
        if (existingUser)
        {
            return new(false, "An account already exists for this email address.");
        }

        if (oldInvitation.Status == OwnerInvitationStatus.Pending)
        {
            oldInvitation.Status = OwnerInvitationStatus.Revoked;
        }

        var now = DateTimeOffset.UtcNow;
        var rawToken = GenerateToken();
        var replacement = new OwnerInvitation
        {
            Email = oldInvitation.Email,
            TokenHash = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.Add(InvitationLifetime),
            Status = OwnerInvitationStatus.Pending,
            CreatedByClerkUserId = createdByClerkUserId
        };

        _dbContext.OwnerInvitations.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SendInvitationAsync(replacement, rawToken, cancellationToken);
    }

    public async Task<InvitationOperationResult> RevokeAsync(
        int invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _dbContext.OwnerInvitations
            .SingleOrDefaultAsync(item => item.InvitationID == invitationId, cancellationToken);

        if (invitation is null)
        {
            return new(false, "Invitation was not found.");
        }

        if (invitation.Status != OwnerInvitationStatus.Pending || invitation.UsedAt is not null)
        {
            return new(false, "Only pending invitations can be revoked.");
        }

        invitation.Status = OwnerInvitationStatus.Revoked;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    public async Task<InvitationValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new(false, Error: "The invitation token is missing.");
        }

        var tokenHash = HashToken(token);
        var invitation = await _dbContext.OwnerInvitations
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (invitation is null)
        {
            return new(false, Error: "The invitation is invalid.");
        }

        if (!TokenMatches(token, invitation.TokenHash))
        {
            return new(false, Error: "The invitation is invalid.");
        }

        if (invitation.Status == OwnerInvitationStatus.Pending &&
            invitation.UsedAt is null &&
            DateTimeOffset.UtcNow < invitation.ExpiresAt)
        {
            return new(true, invitation);
        }

        if (invitation.Status == OwnerInvitationStatus.Pending && invitation.UsedAt is null)
        {
            invitation.Status = OwnerInvitationStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new(false, invitation, "This invitation has expired.");
        }

        return new(false, invitation, invitation.Status switch
        {
            OwnerInvitationStatus.Used => "This invitation has already been used.",
            OwnerInvitationStatus.Revoked => "This invitation has been revoked.",
            OwnerInvitationStatus.Expired => "This invitation has expired.",
            _ => "The invitation is invalid."
        });
    }

    public async Task<OwnerAccountCreationResult> CreateOwnerAccountAsync(
        string token,
        string fullName,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var validation = await ValidateAsync(token, cancellationToken);
        if (!validation.IsValid || validation.Invitation is null)
        {
            return new(false, new[] { validation.Error ?? "The invitation is invalid." });
        }

        var invitation = await _dbContext.OwnerInvitations
            .SingleAsync(item => item.InvitationID == validation.Invitation.InvitationID, cancellationToken);

        var existingUser = await _dbContext.Owners.AnyAsync(owner => owner.RecoveryEmail == invitation.Email, cancellationToken);
        if (existingUser)
        {
            return new(false, new[] { "An account already exists for this email address." });
        }

        if (await _dbContext.Owners.AnyAsync(owner => owner.UserName == userName.Trim(), cancellationToken))
        {
            return new(false, new[] { "That username is already in use." });
        }

        var user = new Owner
        {
            OwnerID = Guid.NewGuid().ToString("N"),
            UserName = userName.Trim(),
            RecoveryEmail = invitation.Email,
            FullName = fullName.Trim(),
        };

        user.PasswordHash = _passwordHasher.Hash(password);
        _dbContext.Owners.Add(user);

        invitation.Status = OwnerInvitationStatus.Used;
        invitation.UsedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, Array.Empty<string>());
    }

    private async Task<InvitationOperationResult> SendInvitationAsync(
        OwnerInvitation invitation,
        string rawToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Application:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new(false, "Application:PublicBaseUrl is not configured.");
        }

        var invitationUrl = $"{baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(rawToken)}";
        var safeUrl = WebUtility.HtmlEncode(invitationUrl);
        var safeEmail = WebUtility.HtmlEncode(invitation.Email);
        var body = $"<p>You have been invited to create an RJTech Owner account.</p>" +
                   $"<p>Use this link to register: <a href=\"{safeUrl}\">Accept the RJTech invitation</a></p>" +
                   $"<p>This invitation is for {safeEmail} and expires in 10 minutes.</p>";

        try
        {
            await _emailSender.SendAsync(
                invitation.Email,
                "RJTech Owner account invitation",
                body,
                cancellationToken);
            return new(true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or SmtpException)
        {
            _logger.LogError(exception, "The Owner invitation email could not be sent.");
            invitation.Status = OwnerInvitationStatus.Revoked;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new(false, "The invitation could not be sent. Verify the Gmail SMTP username and App Password in the Email configuration, then try again.");
        }
    }

    private async Task ExpirePendingInvitationsAsync(
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var expired = await _dbContext.OwnerInvitations
            .Where(invitation => invitation.Email == email &&
                                 invitation.Status == OwnerInvitationStatus.Pending &&
                                 invitation.UsedAt == null &&
                                 invitation.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var invitation in expired)
        {
            invitation.Status = OwnerInvitationStatus.Expired;
        }

        if (expired.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static string GenerateToken()
        => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TokenMatches(string token, string storedHash)
    {
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var expectedHash = Convert.FromHexString(storedHash);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
