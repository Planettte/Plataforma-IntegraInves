using System.Net.Http.Json;
using System.Text.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClientePlantillas
{
    private readonly HttpClient _http;

    public ClientePlantillas(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<InvestigadorPlantillaDto>> InvestigadoresAsync() =>
        await _http.GetFromJsonAsync<List<InvestigadorPlantillaDto>>("api/plantillas/investigadores") ?? [];

    public async Task<(InvestigadorPlantillaDto? Persona, string? Error)> CrearInvestigadorAsync(string nombre, string titulo)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/plantillas/investigadores");
        mensaje.Content = JsonContent.Create(new NombrePlantillaDto { Nombre = nombre, Titulo = titulo });
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<InvestigadorPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<(InvestigadorPlantillaDto? Persona, string? Error)> GuardarInvestigadorAsync(Guid id, string nombre, string titulo)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/plantillas/investigadores/{id}");
        mensaje.Content = JsonContent.Create(new NombrePlantillaDto { Nombre = nombre, Titulo = titulo });
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<InvestigadorPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<string?> EliminarInvestigadorAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/plantillas/investigadores/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode ? null : await LeerError(respuesta);
    }

    public async Task<IReadOnlyList<CoordinadorPlantillaDto>> CoordinadoresAsync() =>
        await _http.GetFromJsonAsync<List<CoordinadorPlantillaDto>>("api/plantillas/coordinadores") ?? [];

    public async Task<(CoordinadorPlantillaDto? Persona, string? Error)> CrearCoordinadorAsync(string nombre, string correo)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/plantillas/coordinadores");
        mensaje.Content = JsonContent.Create(new CoordinadorAltaDto { Nombre = nombre, Correo = correo });
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<CoordinadorPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<(CoordinadorPlantillaDto? Persona, string? Error)> GuardarCoordinadorAsync(Guid id, string nombre, string correo)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/plantillas/coordinadores/{id}");
        mensaje.Content = JsonContent.Create(new CoordinadorAltaDto { Nombre = nombre, Correo = correo });
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<CoordinadorPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<string?> EliminarCoordinadorAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/plantillas/coordinadores/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode ? null : await LeerError(respuesta);
    }

    public async Task<IReadOnlyList<CartaCatalogoDto>> CartasAsync() =>
        await _http.GetFromJsonAsync<List<CartaCatalogoDto>>("api/plantillas/cartas") ?? [];

    public async Task<IReadOnlyList<EstudioPlantillaDto>> EstudiosAsync() =>
        await _http.GetFromJsonAsync<List<EstudioPlantillaDto>>("api/plantillas/estudios") ?? [];

    public async Task<EstudioPlantillaDto?> EstudioAsync(Guid id)
    {
        using var respuesta = await _http.GetAsync($"api/plantillas/estudios/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<EstudioPlantillaDto>();
    }

    public async Task<(EstudioPlantillaDto? Estudio, string? Error)> CrearEstudioAsync(GuardarEstudioPlantillaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/plantillas/estudios");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<EstudioPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<(EstudioPlantillaDto? Estudio, string? Error)> GuardarEstudioAsync(Guid id, GuardarEstudioPlantillaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/plantillas/estudios/{id}");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadFromJsonAsync<EstudioPlantillaDto>(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<bool> EliminarEstudioAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/plantillas/estudios/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode;
    }

    public async Task<EstudioPlantillaDto?> GuardarRevisadasAsync(Guid id, IReadOnlyList<string> codigos)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/plantillas/estudios/{id}/revisadas");
        mensaje.Content = JsonContent.Create(new RevisadasPlantillaDto { Codigos = codigos.ToList() });
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        return await respuesta.Content.ReadFromJsonAsync<EstudioPlantillaDto>();
    }

    public async Task<VistaCartaDto?> CartaAsync(Guid id, string codigo)
    {
        using var respuesta = await _http.GetAsync($"api/plantillas/estudios/{id}/cartas/{Uri.EscapeDataString(codigo)}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<VistaCartaDto>();
    }

    public async Task<(byte[]? Pdf, string? Error)> PdfAsync(Guid id, string codigo, GenerarCartaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/plantillas/estudios/{id}/cartas/{Uri.EscapeDataString(codigo)}/pdf");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode
            ? (await respuesta.Content.ReadAsByteArrayAsync(), null)
            : (null, await LeerError(respuesta));
    }

    public async Task<string?> EnviarAsync(Guid id, string codigo, EnviarCartaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/plantillas/estudios/{id}/cartas/{Uri.EscapeDataString(codigo)}/enviar");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        return respuesta.IsSuccessStatusCode ? null : await LeerError(respuesta);
    }

    private static async Task<string> LeerError(HttpResponseMessage respuesta)
    {
        try
        {
            var mensaje = await respuesta.Content.ReadFromJsonAsync<MensajeDto>();
            if (!string.IsNullOrWhiteSpace(mensaje?.Mensaje))
            {
                return mensaje.Mensaje;
            }
        }
        catch (JsonException)
        {
        }

        return "No se pudo completar la acción.";
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
