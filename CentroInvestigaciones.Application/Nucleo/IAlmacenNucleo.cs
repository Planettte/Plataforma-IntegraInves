using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Nucleo;

public interface IAlmacenNucleo
{
    bool Disponible { get; }

    Task<Usuario?> BuscarPorProveedorAsync(string identificador, CancellationToken ct = default);

    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Area>> ListarAreasAsync(CancellationToken ct = default);

    Task<Area?> BuscarAreaAsync(Guid id, CancellationToken ct = default);

    Task AgregarUsuarioAsync(Usuario usuario, CancellationToken ct = default);

    Task<bool> ExisteAdministradorPlataformaAsync(CancellationToken ct = default);

    Task<Rol?> BuscarRolPorCodigoAsync(string codigo, CancellationToken ct = default);

    Task<Rol?> BuscarRolPorIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteAsignacionAsync(Guid usuarioId, Guid rolId, Guid? areaId, CancellationToken ct = default);

    Task AgregarAsignacionAsync(AsignacionRol asignacion, CancellationToken ct = default);

    Task<IReadOnlyList<string>> PermisosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> PermisosDirectosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task ReemplazarPermisosDirectosAsync(Guid usuarioId, IReadOnlyList<string> codigos, CancellationToken ct = default);

    Task ReemplazarRolPlataformaAsync(Guid usuarioId, string codigoRol, CancellationToken ct = default);

    Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarUsuariosAsync(int pagina, int tamano, string? busqueda, string? estado = null, CancellationToken ct = default);

    Task<IReadOnlyList<Rol>> ListarRolesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Permiso>> ListarPermisosAsync(CancellationToken ct = default);

    Task AgregarEventoAsync(EventoAuditoria evento, CancellationToken ct = default);

    Task<(IReadOnlyList<EventoAuditoria> Elementos, int Total)> ListarEventosAsync(int pagina, int tamano, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> NombresUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Nota>> ListarNotasAsync(Guid usuarioId, CancellationToken ct = default);

    Task<Nota?> BuscarNotaAsync(Guid id, CancellationToken ct = default);

    Task AgregarNotaAsync(Nota nota, CancellationToken ct = default);

    Task EliminarNotaAsync(Nota nota, CancellationToken ct = default);

    Task GuardarAsync(CancellationToken ct = default);
}
