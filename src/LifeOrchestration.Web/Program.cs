using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LifeOrchestration.Web;
using LifeOrchestration.Web.Pages;

var builder = WebAssemblyBlazorWebAssemblyHost.CreateDefaultBuilder();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.AddJavaScriptInitializers("./framework/initBlazor.js");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://192.168.1.194:3080") });

await builder.Build().RunAsync();
