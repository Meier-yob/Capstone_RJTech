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
builder.Services.AddSingleton<ReportUpdateTracker>();
builder.Services.AddScoped<ReportRefreshService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    database.Database.Migrate();
    SeedDefaultAdmin(database, scope.ServiceProvider.GetRequiredService<PasswordHashService>());
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

// Creates the initial administrator account (username "admin" / password "Admin123!")
// when the user table is empty. Every account in the system is an Owner — the RJTech
// business is shared by all signed-in users.
static void SeedDefaultAdmin(ApplicationDbContext database, PasswordHashService passwordHasher)
{
    if (!database.Users.Any())
    {
        database.Users.Add(new AppUser
        {
            FullName = "RJTech Administrator",
            Email = "admin@rjtech.ph",
            Username = "admin",
            Password = passwordHasher.Hash("Admin123!"),
            Role = "Owner",
            DateCreated = DateTime.UtcNow
        });
        database.SaveChanges();
    }
}