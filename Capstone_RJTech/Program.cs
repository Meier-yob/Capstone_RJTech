using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RJTechDatabase")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = UserAuthenticationService.AuthScheme;
    options.DefaultSignInScheme = UserAuthenticationService.AuthScheme;
    options.DefaultChallengeScheme = UserAuthenticationService.AuthScheme;
})
.AddCookie(UserAuthenticationService.AuthScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "RJTech.Auth";
    options.SlidingExpiration = true;
});

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<UserAuthenticationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddSingleton<PasswordHashService>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(UserAuthenticationService.AuthScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<StockNotificationService>();
builder.Services.AddScoped<InstallmentNotificationService>();
builder.Services.AddScoped<InstallmentService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<ReportComputationService>();

var app = builder.Build();
var seedAdmin = builder.Configuration.GetSection("SeedAdmin");

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    database.Database.Migrate();
    SeedDefaultAdmin(
        database,
        scope.ServiceProvider.GetRequiredService<PasswordHashService>(),
        seedAdmin["Username"] ?? "admin",
        seedAdmin["Email"] ?? "admin@rjtech.ph",
        seedAdmin["FullName"] ?? "RJTech Administrator",
        seedAdmin["Password"],
        app.Environment.IsDevelopment());
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

app.Run();

// Creates the initial administrator account when the user table is empty. The
// development fallback keeps a fresh clone easy to run; non-development environments
// must provide SeedAdmin:Password through user secrets or environment configuration.
static void SeedDefaultAdmin(
    ApplicationDbContext database,
    PasswordHashService passwordHasher,
    string username,
    string email,
    string fullName,
    string? configuredPassword,
    bool allowDevelopmentDefault)
{
    if (database.Users.Any())
        return;

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(fullName))
    {
        throw new InvalidOperationException(
            "Configure SeedAdmin:Username, SeedAdmin:Email, and SeedAdmin:FullName before starting with an empty database.");
    }

    var password = configuredPassword;
    if (string.IsNullOrWhiteSpace(password))
    {
        if (!allowDevelopmentDefault)
        {
            throw new InvalidOperationException(
                "Configure SeedAdmin:Password through user secrets or environment variables before starting Production with an empty database.");
        }

        password = "Admin123!";
    }

    database.Users.Add(new AppUser
    {
        FullName = fullName,
        Email = email,
        Username = username,
        Password = passwordHasher.Hash(password),
        Role = "Owner",
        DateCreated = DateTime.UtcNow
    });
    database.SaveChanges();
}