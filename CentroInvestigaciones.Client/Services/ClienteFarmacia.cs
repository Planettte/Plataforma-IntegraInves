using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteFarmacia
{
    private readonly HttpClient _http;

    public ClienteFarmacia(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<FormatoFarmaciaDto>> ListarFormatosAsync()
    {
        return await _http.GetFromJsonAsync<List<FormatoFarmaciaDto>>("api/farmacia/formatos") ?? [];
    }

    public async Task<FormatoFarmaciaDto?> ObtenerFormatoAsync(Guid id)
    {
        var respuesta = await _http.GetAsync($"api/farmacia/formatos/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<FormatoFarmaciaDto>();
    }

    public async Task<FormatoFarmaciaDto> GuardarFormatoAsync(FormatoFarmaciaDto formato)
    {
        var nuevo = formato.Id == Guid.Empty;
        using var mensaje = Solicitud(nuevo ? HttpMethod.Post : HttpMethod.Put, nuevo ? "api/farmacia/formatos" : $"api/farmacia/formatos/{formato.Id}");
        mensaje.Content = JsonContent.Create(formato);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<FormatoFarmaciaDto>() ?? formato;
    }

    public async Task EliminarFormatoAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/farmacia/formatos/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task<(byte[] Bytes, string Nombre)> PdfAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/farmacia/formatos/{id}/pdf");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "contabilidad.pdf";
        return (bytes, nombre);
    }

    public async Task<IReadOnlyList<FilaFarmaciaDto>> ListarInventarioAsync()
    {
        return await _http.GetFromJsonAsync<List<FilaFarmaciaDto>>("api/farmacia/inventario") ?? [];
    }

    public async Task<FilaFarmaciaDto> GuardarInventarioAsync(FilaFarmaciaDto fila)
    {
        var nuevo = fila.Id == Guid.Empty;
        using var mensaje = Solicitud(nuevo ? HttpMethod.Post : HttpMethod.Put, nuevo ? "api/farmacia/inventario" : $"api/farmacia/inventario/{fila.Id}");
        mensaje.Content = JsonContent.Create(fila);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<FilaFarmaciaDto>() ?? fila;
    }

    public async Task EliminarInventarioAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/farmacia/inventario/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    private static async Task Asegurar(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        var texto = (await respuesta.Content.ReadAsStringAsync()).Trim().Trim('"');
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(texto) ? "No se pudo completar la operación." : texto);
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
