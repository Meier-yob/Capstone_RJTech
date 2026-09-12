using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RJTechDatabase")));

const string clerkScheme = "Clerk";
var clerkAuthority = builder.Configuration["Clerk:Authority"];
var clerkAudience = builder.Configuration["Clerk:Audience"];
var clerkRoleClaim = builder.Configuration["Clerk:RoleClaim"] ?? "role";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "OwnerCookie";
    options.DefaultSignInScheme = "OwnerCookie";
    options.DefaultChallengeScheme = "OwnerCookie";
})
.AddCookie("OwnerCookie", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddJwtBearer(clerkScheme, options =>
{
    options.Authority = clerkAuthority;
    options.Audience = clerkAudience;
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = !string.IsNullOrWhiteSpace(clerkAudience),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = clerkRoleClaim,
        NameClaimType = "sub"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var principal = context.Principal;
            if (principal?.Identity is not ClaimsIdentity identity)
                return Task.CompletedTask;

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Capstone_RJTech.Clerk");

            // Clerk can emit metadata claims under snake_case shortcodes (public_metadata)
            // or camelCase names (publicMetadata) depending on how the session token was
            // configured. Support both when looking for the role.
            string[] metadataClaimTypes = ["publicMetadata", "public_metadata", "unsafeMetadata", "unsafe_metadata"];

            var roleValues = principal.Claims
                .Where(claim => claim.Type == clerkRoleClaim ||
                                claim.Type == ClaimTypes.Role ||
                                metadataClaimTypes.Contains(claim.Type, StringComparer.Ordinal))
                .SelectMany(claim =>
                {
                    if (metadataClaimTypes.Contains(claim.Type, StringComparer.Ordinal))
                    {
                        try
                        {
                            using var document = JsonDocument.Parse(claim.Value);
                            if (!document.RootElement.TryGetProperty("role", out var role))
                            {
                                return Array.Empty<string>();
                            }

                            return role.ValueKind == JsonValueKind.Array
                                ? role.EnumerateArray().Select(item => item.GetString() ?? string.Empty)
                                : new[] { role.GetString() ?? string.Empty };
                        }
                        catch (JsonException)
                        {
                            return Array.Empty<string>();
                        }
                    }

                    return claim.Value.StartsWith("[", StringComparison.Ordinal)
                        ? JsonSerializer.Deserialize<string[]>(claim.Value) ?? Array.Empty<string>()
                        : new[] { claim.Value };
                })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in roleValues)
            {
                var alreadyMapped = identity.Claims.Any(claim =>
                    (claim.Type == ClaimTypes.Role || claim.Type == identity.RoleClaimType) &&
                    string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase));

                if (alreadyMapped)
                {
                    continue;
                }

                identity.AddClaim(new Claim(ClaimTypes.Role, role));

                // Also map the role under the identity's role claim type (e.g. "role" from
                // Clerk:RoleClaim) so IsInRole(...) / [Authorize(Roles=...)] resolve correctly.
                if (!string.Equals(identity.RoleClaimType, ClaimTypes.Role, StringComparison.Ordinal))
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, role));
                }
            }

            logger.LogInformation(
                "Clerk token accepted for subject {Subject}. Token claims: {ClaimTypes}. Roles resolved: {Roles}",
                identity.FindFirst("sub")?.Value ?? "(none)",
                string.Join(", ", identity.Claims.Select(claim => claim.Type).Distinct()),
                roleValues.Length == 0 ? "(none)" : string.Join(", ", roleValues));

            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            if (string.IsNullOrWhiteSpace(context.Token) &&
                context.Request.Cookies.TryGetValue("__session", out var clerkSessionToken))
            {
                context.Token = clerkSessionToken;
            }

            // A Clerk session that was denied the Developer Portal (account is not
            // a SystemDeveloper) must stay denied no matter how many times the Clerk
            // client re-issues its cookie. Decode the JWT payload (no signature
            // validation needed for this check) and drop the token; without a token
            // the request is simply unauthenticated, so no redirect loop can form.
            if (!string.IsNullOrWhiteSpace(context.Token))
            {
                try
                {
                    var payload = ReadJwtPayload(context.Token);
                    if (payload is { } root &&
                        root.TryGetProperty("sid", out var sidProperty) &&
                        sidProperty.ValueKind == JsonValueKind.String)
                    {
                        var sessionId = sidProperty.GetString();
                        if (!string.IsNullOrWhiteSpace(sessionId))
                        {
                            var store = context.HttpContext.RequestServices
                                .GetRequiredService<ClerkSessionRejectionStore>();
                            if (store.IsRejected(sessionId))
                            {
                                context.HttpContext.RequestServices
                                    .GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("Capstone_RJTech.Clerk")
                                    .LogWarning("Dropping previously-denied Clerk session {SessionId}.", sessionId);
                                // Let controllers distinguish "no Clerk session" from
                                // "the Clerk session was already denied" so denied users
                                // can still be sent to the access-denied page.
                                context.HttpContext.Items[ClerkSessionRejectionStore.RejectedSessionItemKey] = sessionId;
                                context.Token = null;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Malformed token; fall through to the normal validation path.
                }
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (context.Request.Path.StartsWithSegments("/Developer") &&
                context.Request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true))
            {
                // Send developers to the Clerk login, not the Owner/Staff login, and
                // preserve the URL they were trying to reach so they return to it after signing in.
                context.HandleResponse();
                var developerReturnUrl = (context.Request.Path.Value ?? string.Empty) + context.Request.QueryString.Value;
                var redirectUrl = string.IsNullOrEmpty(developerReturnUrl)
                    ? "/Developer/Login"
                    : $"/Developer/Login?returnUrl={Uri.EscapeDataString(developerReturnUrl)}";
                context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Capstone_RJTech.Clerk")
                    .LogWarning("Clerk challenge on {Path}; redirecting to {RedirectUrl}", context.Request.Path, redirectUrl);
                context.Response.Redirect(redirectUrl);
            }

            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            if (context.Request.Path.StartsWithSegments("/Developer") &&
                context.Request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true))
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status302Found;
                var developerReturnUrl = (context.Request.Path.Value ?? string.Empty) + context.Request.QueryString.Value;
                var redirectUrl = string.IsNullOrEmpty(developerReturnUrl)
                    ? "/Developer/Login"
                    : $"/Developer/Login?returnUrl={Uri.EscapeDataString(developerReturnUrl)}";
                context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Capstone_RJTech.Clerk")
                    .LogWarning("Clerk forbidden on {Path}; redirecting to {RedirectUrl}", context.Request.Path, redirectUrl);
                context.Response.Redirect(redirectUrl);
                return Task.CompletedTask;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            return context.Response.WriteAsync("Access Denied");
        }
    };
});

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IOwnerInvitationService, OwnerInvitationService>();
builder.Services.AddScoped<OwnerAuthenticationService>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddSingleton<ClerkSessionRejectionStore>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("OwnerCookie")
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("SystemDeveloper", policy =>
    {
        policy.AddAuthenticationSchemes(clerkScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("SystemDeveloper");
    });
});

builder.Services.AddScoped<StockNotificationService>();
builder.Services.AddScoped<InstallmentService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddSingleton<ReportUpdateTracker>();
builder.Services.AddScoped<ReportRefreshService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    database.Database.Migrate();

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Dashboard}/{id?}");

// Decodes the (unvalidated) payload of a JWT so session id checks can run before
// signature validation. Returns null for anything that is not a JWT.
static JsonElement? ReadJwtPayload(string token)
{
    var parts = token.Split('.');
    if (parts.Length != 3)
    {
        return null;
    }

    var payload = parts[1]
        .Replace('-', '+')
        .Replace('_', '/');
    var remainder = payload.Length % 4;
    payload = remainder switch
    {
        2 => payload + "==",
        3 => payload + "=",
        _ => payload
    };

    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
    catch (FormatException)
    {
        return null;
    }
}

app.Run();
