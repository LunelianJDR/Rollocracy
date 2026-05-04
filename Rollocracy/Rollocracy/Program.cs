
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Rollocracy.Client.Pages;
using Rollocracy.Components;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;
using Rollocracy.Infrastructure.Options;
using Rollocracy.Infrastructure.Persistence;
using Rollocracy.Infrastructure.Services;
using Rollocracy.Localization;
using Rollocracy.Services;
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

// HttpClient pour les composants Razor existants
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();

    return new HttpClient
    {
        // Utilise automatiquement l’URL courante :
        // - en local : https://localhost:7252/
        // - en préprod/prod : https://rollocracy.com/
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/www/rollocracy/dataprotection-keys"))
    .SetApplicationName("Rollocracy");

// HttpClientFactory pour les appels serveur -> Twitch
builder.Services.AddHttpClient();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<TwitchOptions>(builder.Configuration.GetSection("Twitch"));

builder.Services.AddDbContextFactory<RollocracyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RollocracyDb")));

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();
builder.Services.AddScoped<ITwitchAuthService, TwitchAuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IGameSystemService, GameSystemService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<ICharacterEffectService, CharacterEffectService>();
builder.Services.AddScoped<IGameTestService, GameTestService>();
builder.Services.AddScoped<ISessionNotifier, SignalRSessionNotifier>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IMassDistributionService, MassDistributionService>();
builder.Services.AddScoped<IAdministrationService, AdministrationService>();

builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
builder.Services.AddSingleton<GameTestAutoRollScheduler>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "RollocracyAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[]
{
    new CultureInfo("fr"),
    new CultureInfo("en")
};

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
{
    var languageClaim = context.User?.FindFirst("Language")?.Value;

    if (!string.IsNullOrWhiteSpace(languageClaim))
    {
        ProviderCultureResult result = new ProviderCultureResult(languageClaim, languageClaim);
        return Task.FromResult<ProviderCultureResult?>(result);
    }

    return Task.FromResult<ProviderCultureResult?>(null);
}));

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLocalization(localizationOptions);

app.MapControllers();
app.MapHub<SessionHub>("/sessionhub");

app.UseAntiforgery();

app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Rollocracy.Client._Imports).Assembly);

app.Run();
