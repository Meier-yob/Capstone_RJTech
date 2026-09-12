using System.Collections.Concurrent;

namespace Capstone_RJTech.Services;

/// <summary>
/// Tracks Clerk sessions that were denied entry to the Developer Portal because
/// the account is not a SystemDeveloper. A denied session keeps being treated as
/// unauthenticated for the rest of its life, even if the Clerk client re-issues
/// the session cookie, so the login page can never be bounced in a redirect loop.
/// </summary>
public sealed class ClerkSessionRejectionStore
{
    /// <summary>
    /// HttpContext.Items key set by OnMessageReceived (Program.cs) when the incoming
    /// Clerk token belonged to a previously-denied session and was dropped. Lets
    /// controllers distinguish "no Clerk session at all" from "the Clerk session
    /// was already denied", so they can route back to the access-denied page.
    /// </summary>
    public const string RejectedSessionItemKey = "ClerkRejectedSessionId";

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, DateTime> _rejected = new(StringComparer.Ordinal);

    public void MarkRejected(string sessionId) => _rejected[sessionId] = DateTime.UtcNow.Add(Lifetime);

    public bool IsRejected(string sessionId)
    {
        if (!_rejected.TryGetValue(sessionId, out var expiresUtc))
        {
            return false;
        }

        if (DateTime.UtcNow < expiresUtc)
        {
            return true;
        }

        _rejected.TryRemove(sessionId, out _);
        return false;
    }
}