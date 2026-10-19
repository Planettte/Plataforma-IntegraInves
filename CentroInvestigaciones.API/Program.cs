using System.Security.Claims;
using CentroInvestigaciones.API.Autenticacion;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<FiltroPermiso>();
builder.Services.AddControllers(opciones => opciones.Filters.AddService<FiltroPermiso>());
builder.Services.AddOpenApi();

var opcionesGoogle = builder.Configuration.GetSection(OpcionesGoogle.Seccion).Get<OpcionesGoogle>() ?? new OpcionesGoogle();
builder.Services.AddSingleton(opcionesGoogle);

var correoAdministrador = builder.Configuration["Autenticacion:AdministradorInicial"];
if (string.IsNullOrWhiteSpace(correoAdministrador))
{
    correoAdministrador = DesarrolloLocal.CorreoAdministrador;
}

var conexion = builder.Configuration.GetConnectionString("Centro");
var sqlDisponible = !string.IsNullOrWhiteSpace(conexion) && await SondaSql.RespondeAsync(conexion);

if (sqlDisponible)
{
    builder.Services.AgregarDatos(conexion!);
    builder.Services.AddScoped<IAlmacenNucleo, AlmacenNucleo>();
}
else
{
    builder.Services.AddSingleton<IAlmacenNucleo, AlmacenArchivoLocal>();
}

builder.Services.AddScoped(sp => new ServicioIngreso(sp.GetRequiredService<IAlmacenNucleo>(), correoAdministrador));
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.HojasVida.IDepositoArchivos, DepositoCarpetaLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.HojasVida.IAlmacenHojasVida, AlmacenHojasVidaLocal>();
builder.Services.AddSingleton<GeneradorPdfHojaVida>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Estudios.IDepositoEstudios, DepositoEstudiosLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Estudios.IAlmacenEstudios, AlmacenEstudiosLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Estudios.IEnviadorCorreo, EnviadorCorreoSmtp>();
builder.Services.AddSingleton<GeneradorPdfEstudio>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Plantillas.IAlmacenPlantillas, AlmacenPlantillasLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Plantillas.IFirmasInvestigador, FirmasInvestigadorLocal>();
builder.Services.AddSingleton<GeneradorPdfCarta>();
builder.Services.AddSingleton<AlmacenNotificaciones>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Dispositivos.IAlmacenDispositivos, AlmacenDispositivosLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Farmacia.IAlmacenFarmacia, AlmacenFarmaciaLocal>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Proyectos.IAlmacenProyectos, AlmacenProyectosLocal>();
builder.Services.AddSingleton<GeneradorPdfProyectos>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Protocolos.IAlmacenProtocolos, AlmacenProtocolosLocal>();
builder.Services.AddSingleton<GeneradorPdfProtocolos>();
builder.Services.AddSingleton<AvisosProtocolo>();
builder.Services.AddSingleton<GeneradorPdfFarmacia>();
builder.Services.AddSingleton<GeneradorPdfRetencion>();
builder.Services.AddSingleton<CentroInvestigaciones.Application.Agenda.IAlmacenAgenda, AlmacenAgendaLocal>();
builder.Services.AddSingleton<AvisadorDispositivos>();
builder.Services.AddHostedService<VigilanciaDispositivos>();
builder.Services.AddScoped<ServicioFlujoEstudios>();

var autenticacion = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = "CentroInvestigaciones.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.LoginPath = "/login";
        opciones.Events.OnRedirectToLogin = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        opciones.Events.OnRedirectToAccessDenied = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

if (opcionesGoogle.EstaConfigurado)
{
    autenticacion.AddGoogle(opciones =>
    {
        opciones.ClientId = opcionesGoogle.ClientId!;
        opciones.ClientSecret = opcionesGoogle.ClientSecret!;
        opciones.CallbackPath = "/api/sesion/google-respuesta";
        opciones.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opciones.SaveTokens = false;
        opciones.Events.OnTicketReceived = async contexto =>
        {
            var identificador = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(identificador))
            {
                return;
            }

            var ingreso = contexto.HttpContext.RequestServices.GetRequiredService<ServicioIngreso>();
            var registro = contexto.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("IngresoGoogle");
            try
            {
                var resultado = await ingreso.RegistrarAsync(
                    identificador,
                    contexto.Principal?.FindFirstValue(ClaimTypes.Email) ?? contexto.Principal?.FindFirstValue("email"),
                    contexto.Principal?.FindFirstValue(ClaimTypes.Name) ?? contexto.Principal?.FindFirstValue("name"),
                    contexto.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    contexto.Request.Headers.UserAgent.ToString(),
                    contexto.HttpContext.RequestAborted);

                if (resultado.BaseDisponible && !string.IsNullOrWhiteSpace(resultado.Destino) && contexto.Properties is not null)
                {
                    contexto.Properties.RedirectUri = resultado.Destino;
                }
            }
            catch (Exception excepcion)
            {
                registro.LogWarning(excepcion, "Google identificó al usuario, pero la cuenta no se guardó porque SQL Server no está disponible.");
            }
        };
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

if (sqlDisponible)
{
    using var alcance = app.Services.CreateScope();
    var db = alcance.ServiceProvider.GetRequiredService<CentroDbContext>();
    await db.Database.MigrateAsync();
    await SemillaNucleo.AplicarAsync(db);
}
else
{
    app.Services.GetRequiredService<IAlmacenNucleo>();
    app.Logger.LogInformation("SQL Server no respondió. Las cuentas se guardan en datos-locales/nucleo.json.");
}

app.Services.GetRequiredService<CentroInvestigaciones.Application.HojasVida.IAlmacenHojasVida>();
app.Services.GetRequiredService<CentroInvestigaciones.Application.Estudios.IAlmacenEstudios>();
app.Services.GetRequiredService<CentroInvestigaciones.Application.Agenda.IAlmacenAgenda>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Use(async (contexto, siguiente) =>
{
    var metodoInseguro = HttpMethods.IsPost(contexto.Request.Method)
        || HttpMethods.IsPut(contexto.Request.Method)
        || HttpMethods.IsPatch(contexto.Request.Method)
        || HttpMethods.IsDelete(contexto.Request.Method);

    var esApi = contexto.Request.Path.StartsWithSegments("/api");
    var esRespuestaGoogle = contexto.Request.Path.StartsWithSegments("/api/sesion/google-respuesta");
    var traeEncabezado = contexto.Request.Headers["X-Centro-Solicitud"] == "1";

    if (metodoInseguro && esApi && !esRespuestaGoogle && !traeEncabezado)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    await siguiente(contexto);
});

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Map("/api/{**ruta}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
