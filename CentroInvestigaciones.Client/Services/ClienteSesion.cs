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

    public async Task<bool> CaptchaListoAsync()
    {
        var estado = await _http.GetFromJsonAsync<EstadoCaptchaDto>("api/captcha/estado");
        return estado?.Listo == true;
    }

    public async Task<bool> ConfirmarCaptchaAsync(string respuesta)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "api/captcha");
        solicitud.Headers.Add(EncabezadoSolicitud, "1");
        solicitud.Content = JsonContent.Create(new RespuestaCaptchaDto { Respuesta = respuesta });
        using var resultado = await _http.SendAsync(solicitud);
        return resultado.IsSuccessStatusCode;
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
