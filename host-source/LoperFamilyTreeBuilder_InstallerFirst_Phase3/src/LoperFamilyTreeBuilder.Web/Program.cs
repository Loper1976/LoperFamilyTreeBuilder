using LoperFamilyTreeBuilder.Data;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using LoperFamilyTreeBuilder.Web.Components;
using LoperFamilyTreeBuilder.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ApplicationPaths>();
builder.Services.AddSingleton<ArchiveConfigurationStore>();
builder.Services.AddSingleton<AiConfigurationStore>();
builder.Services.AddHttpClient();

builder.Services.AddFamilyTreeData();

builder.Services.AddSingleton<ActiveCircuitTracker>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>(
    serviceProvider =>
        serviceProvider.GetRequiredService<ActiveCircuitTracker>());
builder.Services.AddHostedService<IdleShutdownService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new
{
    application = "Loper Family Tree Builder",
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    await app.Services.InitializeFamilyTreeDatabaseAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "The local genealogy database could not be initialized.");

    throw;
}

app.Run();
