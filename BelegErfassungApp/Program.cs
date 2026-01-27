//using BelegErfassungApp.Components;
//using BelegErfassungApp.Components.Account;
//using BelegErfassungApp.Data;
//using BelegErfassungApp.Services;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;



//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddCascadingAuthenticationState();
//builder.Services.AddScoped<IdentityUserAccessor>();
//builder.Services.AddScoped<IdentityRedirectManager>();
//builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

//// Database
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
//    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//// Identity
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = IdentityConstants.ApplicationScheme;
//    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//})
//.AddIdentityCookies();

//builder.Services.AddIdentityCore<ApplicationUser>(options =>
//{
//    options.SignIn.RequireConfirmedAccount = false;
//    options.Password.RequireDigit = true;
//    options.Password.RequireLowercase = true;
//    options.Password.RequireUppercase = true;
//    options.Password.RequireNonAlphanumeric = true;
//    options.Password.RequiredLength = 8;

//})
//.AddRoles<IdentityRole>()
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddSignInManager()
//.AddDefaultTokenProviders();

//// Services
//builder.Services.AddScoped<IOcrService, AzureDocumentIntelligenceService>();

//builder.Services.AddScoped<IReceiptService, ReceiptService>();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

//builder.Services.AddScoped<IAuditLogService, AuditLogService>();

//builder.Services.AddScoped<IEmailService, EmailService>();

//builder.Services.AddScoped<IUserManagementService, UserManagementService>();

//builder.Services.AddScoped<IReceiptCommentService, ReceiptCommentService>();

//builder.Services.AddScoped<IStatisticsService, StatisticsService>();



//var app = builder.Build();

//// Seed Database
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        var context = services.GetRequiredService<ApplicationDbContext>();
//        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

//        // Migrationen ausführen
//        await context.Database.MigrateAsync();
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogInformation("Datenbank-Migration erfolgreich durchgeführt");

//        // Rollen erstellen
//        string[] roles = { "Administrator", "Mitglied" };
//        foreach (var role in roles)
//        {
//            if (!await roleManager.RoleExistsAsync(role))
//            {
//                await roleManager.CreateAsync(new IdentityRole(role));
//            }
//        }

//        // Admin-User erstellen
//        var adminEmail = "admin@belegverwaltung.de";
//        if (await userManager.FindByEmailAsync(adminEmail) == null)
//        {
//            var adminUser = new ApplicationUser
//            {
//                UserName = adminEmail,
//                Email = adminEmail,
//                EmailConfirmed = true
//            };

//            var result = await userManager.CreateAsync(adminUser, "Admin@123");
//            if (result.Succeeded)
//            {
//                await userManager.AddToRoleAsync(adminUser, "Administrator");
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "Fehler beim Seeding der Datenbank");
//    }
//}

//// Configure the HTTP request pipeline.

//if (app.Environment.IsDevelopment())
//{
//    //app.UseMigrationsEndPoint();
//    builder.Configuration.AddUserSecrets<Program>();
//}
//else
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}

//// Environment-spezifische appsettings laden
//builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

//// Environment-Variablen (höchste Priorität)
//builder.Configuration.AddEnvironmentVariables();

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseAntiforgery();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//// Add additional endpoints required by the Identity /Account Razor components.
//app.MapAdditionalIdentityEndpoints();

//app.Run();

using BelegErfassungApp.Components;
using BelegErfassungApp.Components.Account;
using BelegErfassungApp.Data;
using BelegErfassungApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

// Program.cs
var culture = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// Cookie-Sicherheit: Automatisches Logout nach Inaktivität
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Cookie nicht per JavaScript zugreifbar
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Nur über HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // CSRF-Schutz
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Nach 30 Min Inaktivität ausloggen
    options.SlidingExpiration = true; // Verlängert Cookie bei Aktivität
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Services
builder.Services.AddScoped<IOcrService, AzureDocumentIntelligenceService>();

builder.Services.AddScoped<IReceiptService, ReceiptService>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IUserManagementService, UserManagementService>();

builder.Services.AddScoped<IReceiptCommentService, ReceiptCommentService>();

builder.Services.AddScoped<IStatisticsService, StatisticsService>();

builder.Services.AddScoped<ISettingsService, SettingsService>();

var app = builder.Build();

// Forwarded Headers MUSS vor Authentication/Authorization
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Migrationen ausführen
        await context.Database.MigrateAsync();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Datenbank-Migration erfolgreich durchgeführt");

        // Rollen erstellen
        string[] roles = { "Administrator", "Mitglied" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Admin-User erstellen
        var adminEmail = "admin@belegverwaltung.de";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Fehler beim Seeding der Datenbank");
    }
}

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    //app.UseMigrationsEndPoint();
    builder.Configuration.AddUserSecrets<Program>();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Environment-spezifische appsettings laden
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Environment-Variablen (höchste Priorität)
builder.Configuration.AddEnvironmentVariables();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
