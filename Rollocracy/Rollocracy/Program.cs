using Microsoft.EntityFrameworkCore;
using Rollocracy.Client.Pages;
using Rollocracy.Components;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;
using Rollocracy.Infrastructure.Persistence;
using Rollocracy.Infrastructure.Services;

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

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Rollocracy.Client._Imports).Assembly);

app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.MapHub<SessionHub>("/sessionhub");

app.UseAntiforgery();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
