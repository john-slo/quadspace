using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Quadspace.Client;
using Quadspace.Core.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });

using var configClient = new HttpClient { BaseAddress = baseAddress };
var gameConfig = await configClient.GetFromJsonAsync<GameConfig>("game-config.json")
    ?? throw new InvalidOperationException("game-config.json could not be loaded.");
builder.Services.AddSingleton(gameConfig);

await builder.Build().RunAsync();
