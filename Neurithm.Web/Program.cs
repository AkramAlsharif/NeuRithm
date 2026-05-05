using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neurithm.Audio;
using Neurithm.Web;
using Neurithm.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IMicrophoneService, WebMicrophoneService>();
builder.Services.AddScoped<IClientLogService, ClientLogService>();
builder.Services.AddScoped<ILevelCatalogService, LevelCatalogService>();
builder.Services.AddScoped<IAvatarCatalogService, AvatarCatalogService>();

await builder.Build().RunAsync();
