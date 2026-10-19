using System.Net.Http.Headers;
using System.Net.Http.Json;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteHojasVida
{
    private readonly HttpClient _http;

    public ClienteHojasVida(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<HojaVidaResumenDto>> ListarAsync()
    {
        return await _http.GetFromJsonAsync<List<HojaVidaResumenDto>>("api/hojas-de-vida") ?? [];
    }

    public async Task<HojaVidaDto?> ObtenerAsync(Guid id)
    {
        using var respuesta = await _http.GetAsync($"api/hojas-de-vida/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<HojaVidaDto>();
    }

    public async Task<IReadOnlyList<EventoHojaVidaDto>> HistorialAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<List<EventoHojaVidaDto>>($"api/hojas-de-vida/{id}/historial") ?? [];
    }

    public async Task<HojaVidaDto> CrearAsync(CrearHojaVidaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/hojas-de-vida");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<HojaVidaDto>() ?? new HojaVidaDto();
    }

    public async Task<ResultadoGuardadoHojaDto> GuardarAsync(Guid id, GuardarHojaVidaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/hojas-de-vida/{id}");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ResultadoGuardadoHojaDto>() ?? new ResultadoGuardadoHojaDto();
    }

    public async Task EliminarAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/hojas-de-vida/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<SoporteHojaDto>> SubirSoportesAsync(Guid id, IReadOnlyList<IBrowserFile> archivos)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/hojas-de-vida/{id}/soportes");
        mensaje.Content = await ContenidoArchivos(archivos, "archivos");
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<List<SoporteHojaDto>>() ?? [];
    }

    public async Task OrdenarSoportesAsync(Guid id, IReadOnlyList<Guid> ids)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/hojas-de-vida/{id}/soportes/orden");
        mensaje.Content = JsonContent.Create(new OrdenSoportesDto { Ids = ids.ToList() });
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task EliminarSoporteAsync(Guid id, Guid soporteId)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/hojas-de-vida/{id}/soportes/{soporteId}");
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task SubirFirmaAsync(Guid id, IBrowserFile archivo, string fecha)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/hojas-de-vida/{id}/firma");
        var contenido = await ContenidoArchivos([archivo], "archivo");
        contenido.Add(new StringContent(fecha), "fecha");
        mensaje.Content = contenido;
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task QuitarFirmaAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/hojas-de-vida/{id}/firma");
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<(byte[] Bytes, string Nombre)> DescargarAsync(Guid id, SolicitudPdfHojaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/hojas-de-vida/{id}/pdf");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        respuesta.EnsureSuccessStatusCode();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName
            ?? "hoja-de-vida.pdf";
        return (await respuesta.Content.ReadAsByteArrayAsync(), nombre.Trim('"'));
    }

    private static async Task<MultipartFormDataContent> ContenidoArchivos(IReadOnlyList<IBrowserFile> archivos, string campo)
    {
        var contenido = new MultipartFormDataContent();
        foreach (var archivo in archivos)
        {
            await using var lectura = archivo.OpenReadStream(20 * 1024 * 1024);
            using var memoria = new MemoryStream();
            await lectura.CopyToAsync(memoria);
            var parte = new ByteArrayContent(memoria.ToArray());
            if (!string.IsNullOrWhiteSpace(archivo.ContentType))
            {
                parte.Headers.ContentType = MediaTypeHeaderValue.Parse(archivo.ContentType);
            }

            contenido.Add(parte, campo, archivo.Name);
        }

        return contenido;
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
