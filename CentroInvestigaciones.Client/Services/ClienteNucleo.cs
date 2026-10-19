using System.Net.Http.Json;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class ClienteNucleo
{
    private readonly HttpClient _http;

    public ClienteNucleo(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AreaDto>> ListarAreasAsync()
    {
        return await _http.GetFromJsonAsync<List<AreaDto>>("api/areas") ?? [];
    }

    public async Task CompletarPerfilAsync(CompletarIngresoDto perfil)
    {
        using var solicitud = Solicitud(HttpMethod.Post, "api/sesion/perfil-inicial");
        solicitud.Content = JsonContent.Create(perfil);
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<string>> ObtenerHerramientasUsuarioAsync(Guid usuarioId)
    {
        return await _http.GetFromJsonAsync<List<string>>($"api/usuarios/{usuarioId}/herramientas") ?? [];
    }

    public async Task GuardarHerramientasUsuarioAsync(Guid usuarioId, IReadOnlyList<string> codigos)
    {
        using var solicitud = Solicitud(HttpMethod.Put, $"api/usuarios/{usuarioId}/herramientas");
        solicitud.Content = JsonContent.Create(new AsignarHerramientasDto { CodigosPermiso = codigos });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<PaginaDto<UsuarioResumenDto>> ListarUsuariosAsync(int pagina, string? busqueda, int tamano = 10)
    {
        var ruta = $"api/usuarios?pagina={pagina}&tamano={tamano}";
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            ruta += "&busqueda=" + Uri.EscapeDataString(busqueda);
        }

        return await _http.GetFromJsonAsync<PaginaDto<UsuarioResumenDto>>(ruta) ?? new PaginaDto<UsuarioResumenDto>();
    }

    public async Task<PaginaDto<UsuarioResumenDto>> ListarUsuariosPendientesAsync()
    {
        return await _http.GetFromJsonAsync<PaginaDto<UsuarioResumenDto>>("api/usuarios?pagina=0&tamano=50&estado=PendienteAprobacion")
            ?? new PaginaDto<UsuarioResumenDto>();
    }

    public async Task ActualizarMiPerfilAsync(CompletarIngresoDto perfil)
    {
        using var solicitud = Solicitud(HttpMethod.Put, "api/sesion/perfil");
        solicitud.Content = JsonContent.Create(perfil);
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task ActualizarUsuarioAsync(Guid usuarioId, CompletarIngresoDto perfil)
    {
        using var solicitud = Solicitud(HttpMethod.Put, $"api/usuarios/{usuarioId}/datos");
        solicitud.Content = JsonContent.Create(perfil);
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task GuardarAccesoAsync(Guid usuarioId, string rol, IReadOnlyList<string> herramientas)
    {
        using var solicitud = Solicitud(HttpMethod.Put, $"api/usuarios/{usuarioId}/acceso");
        solicitud.Content = JsonContent.Create(new GuardarAccesoUsuarioDto { Rol = rol, Herramientas = herramientas });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task AsignarAreaCargoAsync(Guid id, Guid areaId, string cargo)
    {
        using var solicitud = Solicitud(HttpMethod.Put, $"api/usuarios/{id}/area-cargo");
        solicitud.Content = JsonContent.Create(new AsignarAreaCargoDto { AreaId = areaId, Cargo = cargo });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task CambiarEstadoAsync(Guid id, string estado)
    {
        using var solicitud = Solicitud(HttpMethod.Post, $"api/usuarios/{id}/estado");
        solicitud.Content = JsonContent.Create(new CambiarEstadoDto { Estado = estado });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task AsignarRolAsync(Guid usuarioId, Guid rolId, Guid? areaId)
    {
        using var solicitud = Solicitud(HttpMethod.Post, $"api/usuarios/{usuarioId}/roles");
        solicitud.Content = JsonContent.Create(new AsignarRolDto { RolId = rolId, AreaId = areaId });
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync()
    {
        return await _http.GetFromJsonAsync<List<RolDto>>("api/roles") ?? [];
    }

    public async Task<IReadOnlyList<PermisoDto>> ListarPermisosAsync()
    {
        return await _http.GetFromJsonAsync<List<PermisoDto>>("api/permisos") ?? [];
    }

    public async Task<PaginaDto<EventoAuditoriaDto>> ListarAuditoriaAsync(int pagina)
    {
        return await _http.GetFromJsonAsync<PaginaDto<EventoAuditoriaDto>>($"api/auditoria?pagina={pagina}&tamano=20")
            ?? new PaginaDto<EventoAuditoriaDto>();
    }

    public async Task<IReadOnlyList<NotificacionDto>> ListarTableroAsync()
    {
        return await _http.GetFromJsonAsync<List<NotificacionDto>>("api/notificaciones") ?? [];
    }

    public async Task MarcarVistaAsync(Guid id)
    {
        using var solicitud = Solicitud(HttpMethod.Post, $"api/notificaciones/{id}/vista");
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<NotaDto>> ListarNotasAsync()
    {
        return await _http.GetFromJsonAsync<List<NotaDto>>("api/notas") ?? [];
    }

    public async Task<NotaDto> CrearNotaAsync(GuardarNotaDto nota)
    {
        using var solicitud = Solicitud(HttpMethod.Post, "api/notas");
        solicitud.Content = JsonContent.Create(nota);
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<NotaDto>() ?? new NotaDto();
    }

    public async Task ActualizarNotaAsync(Guid id, GuardarNotaDto nota)
    {
        using var solicitud = Solicitud(HttpMethod.Put, $"api/notas/{id}");
        solicitud.Content = JsonContent.Create(nota);
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task EliminarNotaAsync(Guid id)
    {
        using var solicitud = Solicitud(HttpMethod.Delete, $"api/notas/{id}");
        using var respuesta = await _http.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add(ClienteSesion.EncabezadoSolicitud, "1");
        return solicitud;
    }
}
