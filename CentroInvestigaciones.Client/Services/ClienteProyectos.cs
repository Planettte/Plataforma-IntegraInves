using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteProyectos
{
    private readonly HttpClient _http;

    public ClienteProyectos(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<ResponsableProyectoDto>> ResponsablesAsync() =>
        await _http.GetFromJsonAsync<List<ResponsableProyectoDto>>("api/proyectos/responsables") ?? [];

    public async Task<IReadOnlyList<ProyectoListaDto>> ListarAsync(Guid responsable) =>
        await _http.GetFromJsonAsync<List<ProyectoListaDto>>($"api/proyectos?responsable={responsable}") ?? [];

    public async Task<TableroProyectoDto?> ObtenerAsync(Guid id)
    {
        var respuesta = await _http.GetAsync($"api/proyectos/{id}");
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<TableroProyectoDto>();
    }

    public async Task<ProyectoListaDto> CrearAsync(CrearProyectoDto proyecto)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/proyectos");
        mensaje.Content = JsonContent.Create(proyecto);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<ProyectoListaDto>() ?? new ProyectoListaDto();
    }

    public async Task ActualizarAsync(Guid id, ActualizarProyectoDto proyecto)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/proyectos/{id}");
        mensaje.Content = JsonContent.Create(proyecto);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task GuardarFasesAsync(Guid id, IReadOnlyList<FaseEdicionDto> fases)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/proyectos/{id}/fases");
        mensaje.Content = JsonContent.Create(fases);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task GuardarActividadesAsync(Guid id, IReadOnlyList<ActividadEdicionDto> actividades)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/proyectos/{id}/actividades");
        mensaje.Content = JsonContent.Create(actividades);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task CambiarEstadoAsync(Guid id, string estado)
    {
        using var mensaje = Solicitud(HttpMethod.Put, $"api/proyectos/actividades/{id}/estado");
        mensaje.Content = JsonContent.Create(new EstadoActividadDto { Estado = estado });
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task<InvestigadorProyectoDto> CrearInvestigadorAsync(string nombre)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/proyectos/investigadores");
        mensaje.Content = JsonContent.Create(new InvestigadorProyectoDto { Nombre = nombre });
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<InvestigadorProyectoDto>() ?? new InvestigadorProyectoDto();
    }

    public async Task AsignarAsync(Guid actividadId, Guid investigadorId)
    {
        using var mensaje = Solicitud(HttpMethod.Post, "api/proyectos/asignaciones");
        mensaje.Content = JsonContent.Create(new AsignacionDto { ActividadId = actividadId, InvestigadorId = investigadorId });
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task QuitarAsignacionAsync(Guid actividadId, Guid investigadorId)
    {
        using var mensaje = Solicitud(HttpMethod.Delete, $"api/proyectos/asignaciones/{actividadId}/{investigadorId}");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task ProrrogarAsync(Guid actividadId, ProrrogaDto prorroga)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/proyectos/actividades/{actividadId}/prorroga");
        mensaje.Content = JsonContent.Create(prorroga);
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
    }

    public async Task<(byte[] Bytes, string Nombre)> PdfAsync(Guid id)
    {
        using var mensaje = Solicitud(HttpMethod.Post, $"api/proyectos/{id}/pdf");
        using var respuesta = await _http.SendAsync(mensaje);
        await Asegurar(respuesta);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "Seguimiento.pdf";
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
