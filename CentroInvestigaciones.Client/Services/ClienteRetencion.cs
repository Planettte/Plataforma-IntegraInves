using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteRetencion
{
    private readonly HttpClient _http;

    public ClienteRetencion(HttpClient http)
    {
        _http = http;
    }

    public async Task<(byte[] Bytes, string Nombre)> PdfAsync(RetencionDocumentalDto datos)
    {
        using var mensaje = new HttpRequestMessage(HttpMethod.Post, "api/retencion/pdf");
        mensaje.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        mensaje.Content = JsonContent.Create(datos);
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            var texto = (await respuesta.Content.ReadAsStringAsync()).Trim().Trim('"');
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(texto) ? "No se pudo generar el PDF." : texto);
        }

        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "Tabla_Retencion_Documental.pdf";
        return (bytes, nombre);
    }
}
