using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteDispositivos
{
    private readonly HttpClient _http;

    public ClienteDispositivos(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<DispositivoDto>> ListarAsync()
    {
        return await _http.GetFromJsonAsync<List<DispositivoDto>>("api/dispositivos") ?? [];
    }

    public async Task<DispositivoDto> CrearAsync(GuardarDispositivoDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/dispositivos");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<DispositivoDto>() ?? new DispositivoDto();
    }

    public async Task<DispositivoDto> ActualizarAsync(Guid id, GuardarDispositivoDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/dispositivos/{id}");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<DispositivoDto>() ?? new DispositivoDto();
    }

    public async Task<string> LeerEncargadoAsync()
    {
        return await _http.GetFromJsonAsync<EncargadoDispositivosDto>("api/dispositivos/encargado") is { } encargado
            ? encargado.Correo
            : "";
    }

    public async Task GuardarEncargadoAsync(string correo)
    {
        using var mensaje = Solicitud(HttpMethod.Put, "api/dispositivos/encargado");
        mensaje.Content = JsonContent.Create(new EncargadoDispositivosDto { Correo = correo });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
