using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ViteKlub.Web;
using ViteKlub.Web.Authentication;
using ViteKlub.Web.Members;
using ViteKlub.Web.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IDemoDatasetStore, BrowserDemoDatasetStore>();
builder.Services.AddScoped<IMemberOperationValues, SystemMemberOperationValues>();
builder.Services.AddScoped<MemberCommandFactory>();
builder.Services.AddScoped<IDemoSessionStore, BrowserDemoSessionStore>();
builder.Services.AddScoped<DemoAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<DemoAuthenticationStateProvider>());
builder.Services.AddScoped<IDemoAuthenticationService>(provider =>
    provider.GetRequiredService<DemoAuthenticationStateProvider>());

await builder.Build().RunAsync();
