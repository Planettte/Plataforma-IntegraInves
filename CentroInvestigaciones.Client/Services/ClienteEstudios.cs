using System.Net.Http.Headers;
using System.Net.Http.Json;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteEstudios
{
    private readonly HttpClient _http;

    public ClienteEstudios(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<EstudioResumenDto>> ListarAsync()
    {
        return await _http.GetFromJsonAsync<List<EstudioResumenDto>>("api/estudios") ?? [];
    }

    public async Task<EstudioDto?> ObtenerAsync(Guid id)
    {
        using var respuesta = await _http.GetAsync($"api/estudios/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<EstudioDto>();
    }

    public async Task<EstudioDto?> ObtenerPublicoAsync(string token)
    {
        using var respuesta = await _http.GetAsync($"api/estudios/publico/{token}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<EstudioDto>();
    }

    public async Task<ResultadoEstudioDto> CrearPublicoAsync(GuardarFase1EstudioDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/estudios/publico");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task<(byte[] Bytes, string Nombre)> VistaPreviaPdfAsync(GuardarFase1EstudioDto solicitud, int formato)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/publico/pdf?formato={formato}");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "formato.pdf";
        return (bytes, nombre);
    }

    public async Task<ResultadoEstudioDto> CorregirPublicoAsync(string token, GuardarFase1EstudioDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/estudios/publico/{token}");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task SubirPublicoAsync(string token, string clave, IBrowserFile archivo)
    {
        await using var lectura = archivo.OpenReadStream(20 * 1024 * 1024);
        using var memoria = new MemoryStream();
        await lectura.CopyToAsync(memoria);
        await SubirPublicoAsync(token, clave, archivo.Name, archivo.ContentType, memoria.ToArray());
    }

    public async Task SubirPublicoAsync(string token, string clave, string nombre, string? tipo, byte[] bytes)
    {
        using var contenido = new MultipartFormDataContent();
        var parte = new ByteArrayContent(bytes);
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            parte.Headers.ContentType = MediaTypeHeaderValue.Parse(tipo);
        }

        contenido.Add(parte, "archivo", nombre);
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/publico/{token}/archivos?clave={Uri.EscapeDataString(clave)}");
        mensaje.Content = contenido;
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<ResultadoEstudioDto> CerrarFase2Async(string token, string numero)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/publico/{token}/fase2");
        mensaje.Content = JsonContent.Create(new Fase2EstudioDto { NumeroAprobacionCei = numero });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task<ResultadoEstudioDto> PedirCorreccionAsync(Guid id, string mensajeTexto)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/{id}/correccion");
        mensaje.Content = JsonContent.Create(new MensajeEstudioDto { Mensaje = mensajeTexto });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task<ResultadoEstudioDto> FinalizarAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/{id}/finalizar");
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task<ResultadoEstudioDto> ReenviarAsync(Guid id, string tipo)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/{id}/reenviar");
        mensaje.Content = JsonContent.Create(new ReenvioEstudioDto { Tipo = tipo });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task<ResultadoEstudioDto> EmitirAvalAsync(Guid id, string mensajeTexto)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/{id}/aval");
        mensaje.Content = JsonContent.Create(new MensajeEstudioDto { Mensaje = mensajeTexto });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoEstudioDto>() ?? new ResultadoEstudioDto();
    }

    public async Task SubirAvalAsync(Guid id, IBrowserFile archivo)
    {
        using var contenido = new MultipartFormDataContent();
        await using var lectura = archivo.OpenReadStream(20 * 1024 * 1024);
        using var memoria = new MemoryStream();
        await lectura.CopyToAsync(memoria);
        var parte = new ByteArrayContent(memoria.ToArray());
        if (!string.IsNullOrWhiteSpace(archivo.ContentType))
        {
            parte.Headers.ContentType = MediaTypeHeaderValue.Parse(archivo.ContentType);
        }

        contenido.Add(parte, "archivo", archivo.Name);
        using var mensaje = Solicitud(HttpMethod.Post, $"api/estudios/{id}/aval-archivo");
        mensaje.Content = contenido;
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
