using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LifeOrchestration.Web.Components;

var builder = WebAssemblyHostBuilder.CreateDefault([]);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://192.168.1.194:3080") });
await builder.Build().RunAsync();
