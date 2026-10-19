using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenNoDisponible : IAlmacenNucleo
{
    public bool Disponible => false;

    public Task<Usuario?> BuscarPorProveedorAsync(string identificador, CancellationToken ct = default) => Task.FromResult<Usuario?>(null);

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Usuario?>(null);

    public Task<IReadOnlyList<Area>> ListarAreasAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Area>>([]);

    public Task<Area?> BuscarAreaAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Area?>(null);

    public Task AgregarUsuarioAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> ExisteAdministradorPlataformaAsync(CancellationToken ct = default) => Task.FromResult(false);

    public Task<Rol?> BuscarRolPorCodigoAsync(string codigo, CancellationToken ct = default) => Task.FromResult<Rol?>(null);

    public Task<Rol?> BuscarRolPorIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Rol?>(null);

    public Task<bool> ExisteAsignacionAsync(Guid usuarioId, Guid rolId, Guid? areaId, CancellationToken ct = default) => Task.FromResult(false);

    public Task AgregarAsignacionAsync(AsignacionRol asignacion, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<string>> PermisosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> PermisosDirectosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task ReemplazarPermisosDirectosAsync(Guid usuarioId, IReadOnlyList<string> codigos, CancellationToken ct = default) => Task.CompletedTask;

    public Task ReemplazarRolPlataformaAsync(Guid usuarioId, string codigoRol, CancellationToken ct = default) => Task.CompletedTask;

    public Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarUsuariosAsync(int pagina, int tamano, string? busqueda, string? estado = null, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<Usuario>, int)>(([], 0));

    public Task<IReadOnlyList<Rol>> ListarRolesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Rol>>([]);

    public Task<IReadOnlyList<Permiso>> ListarPermisosAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Permiso>>([]);

    public Task AgregarEventoAsync(EventoAuditoria evento, CancellationToken ct = default) => Task.CompletedTask;

    public Task<(IReadOnlyList<EventoAuditoria> Elementos, int Total)> ListarEventosAsync(int pagina, int tamano, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<EventoAuditoria>, int)>(([], 0));

    public Task<IReadOnlyDictionary<Guid, string>> NombresUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    public Task<IReadOnlyList<Nota>> ListarNotasAsync(Guid usuarioId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nota>>([]);

    public Task<Nota?> BuscarNotaAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Nota?>(null);

    public Task AgregarNotaAsync(Nota nota, CancellationToken ct = default) => Task.CompletedTask;

    public Task EliminarNotaAsync(Nota nota, CancellationToken ct = default) => Task.CompletedTask;

    public Task GuardarAsync(CancellationToken ct = default) => Task.CompletedTask;
}
