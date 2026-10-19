using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteAgenda
{
    private readonly HttpClient _http;

    public ClienteAgenda(HttpClient http)
    {
        _http = http;
    }

    public async Task<DiaAgendaDto> DiaAsync(string ambito, DateOnly fecha, bool compartida, bool asignadas = false)
    {
        var ruta = $"api/agenda/{ambito}?fecha={fecha:yyyy-MM-dd}&compartida={compartida}&asignadas={asignadas}";
        return await _http.GetFromJsonAsync<DiaAgendaDto>(ruta) ?? new DiaAgendaDto();
    }

    public async Task<DiaAgendaDto> GeneralAsync(DateOnly fecha)
    {
        return await _http.GetFromJsonAsync<DiaAgendaDto>($"api/agenda/publico/dia?fecha={fecha:yyyy-MM-dd}") ?? new DiaAgendaDto();
    }

    public async Task<List<CitaAgendaDto>> BuscarAsync(string ambito, string texto, string desde, string hasta, bool compartida)
    {
        var ruta = $"api/agenda/{ambito}/buscar?texto={Uri.EscapeDataString(texto)}&desde={Uri.EscapeDataString(desde)}&hasta={Uri.EscapeDataString(hasta)}&compartida={compartida}";
        return await _http.GetFromJsonAsync<List<CitaAgendaDto>>(ruta) ?? [];
    }

    public async Task<List<CitaAgendaDto>> BuscarGeneralAsync(string texto, string desde, string hasta)
    {
        var ruta = $"api/agenda/publico/buscar?texto={Uri.EscapeDataString(texto)}&desde={Uri.EscapeDataString(desde)}&hasta={Uri.EscapeDataString(hasta)}";
        return await _http.GetFromJsonAsync<List<CitaAgendaDto>>(ruta) ?? [];
    }

    public async Task MarcarImportanteAsync(string ambito, Guid id, bool importante)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/agenda/{ambito}/citas/{id}/importante");
        mensaje.Content = JsonContent.Create(new MarcarImportanteDto { Importante = importante });
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await Texto(respuesta));
        }
    }

    public async Task<CitaAgendaDto> GuardarCitaAsync(string ambito, Guid? id, GuardarCitaAgendaDto solicitud)
    {
        var metodo = id is null ? HttpMethod.Post : HttpMethod.Put;
        var ruta = id is null ? $"api/agenda/{ambito}/citas" : $"api/agenda/{ambito}/citas/{id}";
        using var mensaje = Solicitud(metodo, ruta);
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await Texto(respuesta));
        }

        return await respuesta.Content.ReadFromJsonAsync<CitaAgendaDto>() ?? new CitaAgendaDto();
    }

    public async Task EliminarCitaAsync(string ambito, Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/agenda/{ambito}/citas/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await Texto(respuesta));
        }
    }

    public async Task<MonitoriaAgendaDto> GuardarMonitoriaAsync(string ambito, GuardarMonitoriaAgendaDto solicitud)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/agenda/{ambito}/monitorias");
        mensaje.Content = JsonContent.Create(solicitud);
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await Texto(respuesta));
        }

        return await respuesta.Content.ReadFromJsonAsync<MonitoriaAgendaDto>() ?? new MonitoriaAgendaDto();
    }

    public async Task EliminarMonitoriaAsync(string ambito, Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/agenda/{ambito}/monitorias/{id}");
        using var respuesta = await _http.SendAsync(mensaje);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await Texto(respuesta));
        }
    }

    private static async Task<string> Texto(HttpResponseMessage respuesta)
    {
        var texto = await respuesta.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(texto) ? "No se pudo completar la acción." : texto.Trim('"');
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
