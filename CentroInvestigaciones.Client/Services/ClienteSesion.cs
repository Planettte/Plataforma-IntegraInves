using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteSesion
{
    public const string EncabezadoSolicitud = "X-Centro-Solicitud";

    private readonly HttpClient _http;

    public ClienteSesion(HttpClient http)
    {
        _http = http;
    }

    public async Task<EstadoAutenticacionDto> ObtenerEstadoAsync()
    {
        return await _http.GetFromJsonAsync<EstadoAutenticacionDto>("api/sesion/estado")
            ?? new EstadoAutenticacionDto();
    }

    public async Task<SesionActualDto> ObtenerActualAsync()
    {
        return await _http.GetFromJsonAsync<SesionActualDto>("api/sesion/yo")
            ?? new SesionActualDto();
    }

    public async Task<List<CuentaDesarrolloDto>> ListarCuentasDesarrolloAsync()
    {
        return await _http.GetFromJsonAsync<List<CuentaDesarrolloDto>>("api/sesion/cuentas-desarrollo") ?? [];
    }

    public async Task CerrarAsync()
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "api/sesion/cerrar");
        solicitud.Headers.Add(EncabezadoSolicitud, "1");
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task GuardarTemaAsync(string tema)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "api/sesion/tema");
        solicitud.Headers.Add(EncabezadoSolicitud, "1");
        solicitud.Content = JsonContent.Create(new TemaPreferidoDto { Tema = tema });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }
}
