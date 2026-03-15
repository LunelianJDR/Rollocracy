using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Rollocracy.Client.Pages;
using Rollocracy.Components;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;
using Rollocracy.Infrastructure.Persistence;
using Rollocracy.Infrastructure.Services;
using Rollocracy.Localization;
using Rollocracy.Services;
using System.Globalization;
using System.Security.Claims;


//////////////////////////////
/////// VAR BUILDER //////////
//////////////////////////////


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7252/")
});

builder.Services.AddDbContextFactory<RollocracyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RollocracyDb")));

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameSystemService, GameSystemService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSignalR();

builder.Services.AddScoped<ICharacterEffectService, CharacterEffectService>();

// Authentification par cookie
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

// Permet aux composants Blazor d'accéder à l'utilisateur connecté
builder.Services.AddCascadingAuthenticationState();

// Active le système de localisation .NET
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<ICharacterService, CharacterService>();

builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();

builder.Services.AddScoped<IGameTestService, GameTestService>();
builder.Services.AddSingleton<GameTestAutoRollScheduler>();

builder.Services.AddScoped<ISessionNotifier, SignalRSessionNotifier>();

builder.Services.AddScoped<IPollService, PollService>();

//////////////////////////

var supportedCultures = new[]
{
    new CultureInfo("fr"),
    new CultureInfo("en")
};

//////////////////////////
/////// VAR APP //////////
//////////////////////////


var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Priorité 1 : langue du compte utilisateur stockée dans le cookie d'auth
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

// Configure the HTTP request pipeline.
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

// Le cookie doit être lu avant de choisir la culture
app.UseAuthentication();
app.UseAuthorization();

// Maintenant la culture peut lire le claim Language
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
