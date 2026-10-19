using CentroInvestigaciones.Client;
using CentroInvestigaciones.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddScoped<ClienteSesion>();
builder.Services.AddScoped<ClienteNucleo>();
builder.Services.AddScoped<EstadoSesion>();
builder.Services.AddScoped<TemaInterfaz>();
builder.Services.AddScoped<ClienteHojasVida>();
builder.Services.AddScoped<ClienteEstudios>();
builder.Services.AddScoped<ClientePlantillas>();
builder.Services.AddScoped<ClienteDispositivos>();
builder.Services.AddScoped<ClienteFarmacia>();
builder.Services.AddScoped<ClienteRetencion>();
builder.Services.AddScoped<ClienteProyectos>();
builder.Services.AddScoped<ClienteProtocolos>();
builder.Services.AddScoped<ClienteAgenda>();
builder.Services.AddScoped<Avisos>();

await builder.Build().RunAsync();
