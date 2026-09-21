using System.Security.Cryptography;
using System.Text;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services;

/// <summary>
/// Drives the forgot-password flow: issues a one-time password (OTP) by email for a
/// registered account, verifies the OTP, and resets the stored password hash.
/// </summary>
public sealed class PasswordResetService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly PasswordHashService _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ApplicationDbContext db,
        PasswordHashService passwordHasher,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> AccountExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var key = email.Trim();
        return await _db.Users.AnyAsync(user => user.Email == key, cancellationToken);
    }

    /// <summary>
    /// Invalidates older pending codes for the address, generates a new 6-digit OTP,
    /// stores its hash, and emails the code. Returns null when the email is not
    /// registered or the code could not be delivered.
    /// </summary>
    public async Task<PasswordResetCode?> CreateCodeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        var exists = await _db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (!exists)
            return null;

        var oldCodes = await _db.PasswordResetCodes
            .Where(code => code.Email == normalizedEmail && code.VerifiedAt == null)
            .ToListAsync(cancellationToken);
        _db.PasswordResetCodes.RemoveRange(oldCodes);

        var code = new PasswordResetCode
        {
            Email = normalizedEmail,
            OtpHash = string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.Add(CodeLifetime)
        };

        var otp = GenerateOtp();
        code.OtpHash = HashOtp(otp);
        _db.PasswordResetCodes.Add(code);
        await _db.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["Application:PublicBaseUrl"]?.TrimEnd('/');
        var verifyUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? $"/Account/ForgotPasswordVerify?id={code.Id}"
            : $"{baseUrl}/Account/ForgotPasswordVerify?id={code.Id}";
        var safeUrl = System.Net.WebUtility.HtmlEncode(verifyUrl);

        var body = "<p>You requested to reset your RJTech password.</p>" +
                   "<p>Your one-time verification code is:</p>" +
                   $"<p style=\"font-size: 28px; font-weight: bold; letter-spacing: 4px;\">{otp}</p>" +
                   $"<p>Enter this code on the verification page. It expires in 10 minutes.</p>" +
                   $"<p>If you did not request this, you can safely ignore this email.</p>" +
                   $"<p><a href=\"{safeUrl}\">{safeUrl}</a></p>";

        try
        {
            await _emailSender.SendAsync(
                normalizedEmail,
                "Your RJTech password reset code",
                body,
                cancellationToken);
            return code;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Net.Mail.SmtpException)
        {
            _logger.LogError(exception, "The password-reset OTP email could not be sent to {Email}.", normalizedEmail);
            _db.PasswordResetCodes.Remove(code);
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }
    }

    public async Task<PasswordResetCode?> GetPendingRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var code = await _db.PasswordResetCodes
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (code is null || code.VerifiedAt is not null || code.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;
        return code;
    }

    /// <summary>
    /// Marks the request as verified when the submitted OTP matches. Returns the request
    /// when successful, otherwise null.
    /// </summary>
    public async Task<PasswordResetCode?> VerifyCodeAsync(
        string requestId,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var code = await _db.PasswordResetCodes
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (code is null || code.VerifiedAt is not null || code.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        if (string.IsNullOrWhiteSpace(otp) || !HashesMatch(HashOtp(otp.Trim()), code.OtpHash))
            return null;

        code.VerifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return code;
    }

    public async Task<PasswordResetCode?> GetVerifiedRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var code = await _db.PasswordResetCodes
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (code is null || code.VerifiedAt is null || code.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;
        return code;
    }

    /// <summary>Sets the user's new password and consumes the reset request.</summary>
    public async Task<bool> ResetPasswordAsync(
        string requestId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var code = await GetVerifiedRequestAsync(requestId, cancellationToken);
        if (code is null)
            return false;

        var user = await _db.Users
            .SingleOrDefaultAsync(item => item.Email == code.Email, cancellationToken);
        if (user is null)
            return false;

        user.Password = _passwordHasher.Hash(newPassword);
        _db.PasswordResetCodes.Remove(code);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashOtp(string otp)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));

    private static bool HashesMatch(string actualHash, string expectedHash)
    {
        if (actualHash.Length != expectedHash.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHash),
            Encoding.UTF8.GetBytes(expectedHash));
    }
}