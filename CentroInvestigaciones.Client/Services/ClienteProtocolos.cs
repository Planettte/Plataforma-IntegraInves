using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteProtocolos
{
    private readonly HttpClient _http;

    public ClienteProtocolos(HttpClient http)
    {
        _http = http;
    }

    public async Task<byte[]> InstruccionesAsync()
    {
        var respuesta = await _http.GetAsync("api/protocolos/publico/instrucciones");
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadAsByteArrayAsync();
    }

    public async Task<IReadOnlyList<RevisorProtocoloDto>> RevisoresAsync() =>
        await _http.GetFromJsonAsync<List<RevisorProtocoloDto>>("api/protocolos/publico/revisores") ?? [];

    public async Task<ProtocoloDto?> ObtenerPublicoAsync(string token)
    {
        var respuesta = await _http.GetAsync($"api/protocolos/publico/{token}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ProtocoloDto>();
    }

    public async Task<ResultadoProtocoloDto> CrearAsync(GuardarProtocoloDto protocolo)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/protocolos/publico");
        mensaje.Content = JsonContent.Create(protocolo);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<ResultadoProtocoloDto>() ?? new ResultadoProtocoloDto();
    }

    public async Task<ResultadoProtocoloDto> ReenviarAsync(string token, GuardarProtocoloDto protocolo)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/protocolos/publico/{token}");
        mensaje.Content = JsonContent.Create(protocolo);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<ResultadoProtocoloDto>() ?? new ResultadoProtocoloDto();
    }

    public async Task<IReadOnlyList<ProtocoloListaDto>> ListarAsync() =>
        await _http.GetFromJsonAsync<List<ProtocoloListaDto>>("api/protocolos") ?? [];

    public async Task<ProtocoloDto?> ObtenerAsync(Guid id)
    {
        var respuesta = await _http.GetAsync($"api/protocolos/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ProtocoloDto>();
    }

    public async Task<ResultadoProtocoloDto> CorregirAsync(Guid id, Dictionary<string, string> comentarios)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/protocolos/{id}/correccion");
        mensaje.Content = JsonContent.Create(new CorreccionProtocoloDto { Comentarios = comentarios });
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<ResultadoProtocoloDto>() ?? new ResultadoProtocoloDto();
    }

    public async Task<ResultadoProtocoloDto> AceptarAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/protocolos/{id}/aceptar");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<ResultadoProtocoloDto>() ?? new ResultadoProtocoloDto();
    }

    public async Task<(byte[] Bytes, string Nombre)> PdfAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/protocolos/{id}/pdf");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "Protocolo.pdf";
        return (bytes, nombre);
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
