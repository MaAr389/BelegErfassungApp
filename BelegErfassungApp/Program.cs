using BelegErfassungApp.Components;
using BelegErfassungApp.Components.Account;
using BelegErfassungApp.Data;
using BelegErfassungApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Globalization;

// Culture-Einstellungen
var culture = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Environment-spezifische appsettings laden (VOR Build!)
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

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

// NUR DbContextFactory registrieren (nicht AddDbContext)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

// Cookie-Sicherheit
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Traefik terminiert HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Identity Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

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

// Forwarded Headers für Traefik (MUSS vor Authentication/Authorization)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Seed Database mit Retry-Logik
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var contextFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Retry-Logik: 10 Versuche mit 3 Sek Pause
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                // Migrationen ausführen
                await context.Database.MigrateAsync();
                logger.LogInformation("Datenbank-Migration erfolgreich durchgeführt");

                // Rollen erstellen (immer, auch in Production)
                string[] roles = { "Administrator", "Mitglied" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Rolle '{Role}' erstellt", role);
                    }
                }

                // Admin-User NUR in Development erstellen
                if (app.Environment.IsDevelopment())
                {
                    var adminEmail = "admin@belegverwaltung.de";
                    if (await userManager.FindByEmailAsync(adminEmail) == null)
                    {
                        var adminUser = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            FirstName = "Admin",
                            LastName = "User",
                            RegistrationDate = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(adminUser, "Admin@123");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, "Administrator");
                            logger.LogInformation("DEV: Admin-User '{Email}' erfolgreich erstellt", adminEmail);
                        }
                        else
                        {
                            logger.LogWarning("DEV: Admin-User konnte nicht erstellt werden: {Errors}",
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
                else
                {
                    logger.LogInformation("PRODUCTION: Admin-Seeding übersprungen");
                }

                // Erfolgreich, Schleife verlassen
                break;
            }
            catch (SqlException ex) when (i < 9)
            {
                logger.LogWarning("DB noch nicht bereit (Versuch {Attempt}/10): {Message}. Warte 3 Sekunden...",
                    i + 1, ex.Message);
                await Task.Delay(3000);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fehler beim Seeding der Datenbank nach allen Versuchen");
        // App startet trotzdem weiter
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();


//using BelegErfassungApp.Components;
//using BelegErfassungApp.Components.Account;
//using BelegErfassungApp.Data;
//using BelegErfassungApp.Services;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.HttpOverrides;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Data.SqlClient;
//using System.Globalization;

//// Culture-Einstellungen
//var culture = new CultureInfo("de-DE");
//CultureInfo.DefaultThreadCurrentCulture = culture;
//CultureInfo.DefaultThreadCurrentUICulture = culture;

//var builder = WebApplication.CreateBuilder(args);

//// Environment-spezifische appsettings laden (VOR Build!)
//builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
//builder.Configuration.AddEnvironmentVariables();

//if (builder.Environment.IsDevelopment())
//{
//    builder.Configuration.AddUserSecrets<Program>();
//}

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

//// NUR DbContextFactory registrieren (nicht AddDbContext)
//builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

//// Cookie-Sicherheit
//builder.Services.ConfigureApplicationCookie(options =>
//{
//    options.Cookie.HttpOnly = true;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Traefik terminiert HTTPS
//    options.Cookie.SameSite = SameSiteMode.Lax;
//    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
//    options.SlidingExpiration = true;
//    options.LoginPath = "/Account/Login";
//    options.LogoutPath = "/Account/Logout";
//    options.AccessDeniedPath = "/Account/AccessDenied";
//});

//// Identity Authentication
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = IdentityConstants.ApplicationScheme;
//    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//})
//.AddIdentityCookies();

//// Services
//builder.Services.AddScoped<IOcrService, AzureDocumentIntelligenceService>();
//builder.Services.AddScoped<IReceiptService, ReceiptService>();
//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
//builder.Services.AddScoped<IAuditLogService, AuditLogService>();
//builder.Services.AddScoped<IEmailService, EmailService>();
//builder.Services.AddScoped<IUserManagementService, UserManagementService>();
//builder.Services.AddScoped<IReceiptCommentService, ReceiptCommentService>();
//builder.Services.AddScoped<IStatisticsService, StatisticsService>();
//builder.Services.AddScoped<ISettingsService, SettingsService>();

//var app = builder.Build();

//// Forwarded Headers für Traefik (MUSS vor Authentication/Authorization)
//app.UseForwardedHeaders(new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//});

//// Seed Database mit Retry-Logik
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var logger = services.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        var contextFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
//        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

//        // Retry-Logik: 10 Versuche mit 3 Sek Pause
//        for (int i = 0; i < 10; i++)
//        {
//            try
//            {
//                await using var context = await contextFactory.CreateDbContextAsync();

//                // Migrationen ausführen
//                await context.Database.MigrateAsync();
//                logger.LogInformation("Datenbank-Migration erfolgreich durchgeführt");

//                // Rollen erstellen
//                string[] roles = { "Administrator", "Mitglied" };
//                foreach (var role in roles)
//                {
//                    if (!await roleManager.RoleExistsAsync(role))
//                    {
//                        await roleManager.CreateAsync(new IdentityRole(role));
//                        logger.LogInformation("Rolle '{Role}' erstellt", role);
//                    }
//                }

//                // Admin-User erstellen
//                var adminEmail = "admin@belegverwaltung.de";
//                if (await userManager.FindByEmailAsync(adminEmail) == null)
//                {
//                    var adminUser = new ApplicationUser
//                    {
//                        UserName = adminEmail,
//                        Email = adminEmail,
//                        EmailConfirmed = true,
//                        FirstName = "Admin",
//                        LastName = "User",
//                        RegistrationDate = DateTime.UtcNow
//                    };

//                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
//                    if (result.Succeeded)
//                    {
//                        await userManager.AddToRoleAsync(adminUser, "Administrator");
//                        logger.LogInformation("Admin-User '{Email}' erfolgreich erstellt", adminEmail);
//                    }
//                    else
//                    {
//                        logger.LogWarning("Admin-User konnte nicht erstellt werden: {Errors}",
//                            string.Join(", ", result.Errors.Select(e => e.Description)));
//                    }
//                }

//                // Erfolgreich, Schleife verlassen
//                break;
//            }
//            catch (SqlException ex) when (i < 9)
//            {
//                logger.LogWarning("DB noch nicht bereit (Versuch {Attempt}/10): {Message}. Warte 3 Sekunden...",
//                    i + 1, ex.Message);
//                await Task.Delay(3000);
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Fehler beim Seeding der Datenbank nach allen Versuchen");
//        // App startet trotzdem weiter
//    }
//}

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseMigrationsEndPoint();
//}
//else
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseAntiforgery();
//app.UseAuthorization();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.MapAdditionalIdentityEndpoints();

//app.Run();
