using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Data;
using CentroInvestigaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenNucleo : IAlmacenNucleo
{
    private readonly CentroDbContext _db;

    public AlmacenNucleo(CentroDbContext db)
    {
        _db = db;
    }

    public bool Disponible => true;

    public Task<Usuario?> BuscarPorProveedorAsync(string identificador, CancellationToken ct = default)
    {
        return _db.Usuarios
            .Include(usuario => usuario.AreaInteres)
            .Include(usuario => usuario.Areas)
            .FirstOrDefaultAsync(usuario => usuario.IdentificadorProveedor == identificador, ct);
    }

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Usuarios
            .Include(usuario => usuario.AreaInteres)
            .Include(usuario => usuario.Areas)
            .FirstOrDefaultAsync(usuario => usuario.Id == id, ct);
    }

    public async Task<IReadOnlyList<Area>> ListarAreasAsync(CancellationToken ct = default)
    {
        return await _db.Areas.OrderBy(area => area.Nombre).ToListAsync(ct);
    }

    public Task<Area?> BuscarAreaAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Areas.FirstOrDefaultAsync(area => area.Id == id, ct);
    }

    public Task AgregarUsuarioAsync(Usuario usuario, CancellationToken ct = default)
    {
        _db.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task<bool> ExisteAdministradorPlataformaAsync(CancellationToken ct = default)
    {
        return _db.AsignacionesRol.AnyAsync(
            asignacion => asignacion.Rol.Codigo == PermisosNucleo.RolAdministrador && asignacion.AreaId == null,
            ct);
    }

    public Task<Rol?> BuscarRolPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        return _db.Roles.FirstOrDefaultAsync(rol => rol.Codigo == codigo, ct);
    }

    public Task<Rol?> BuscarRolPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Roles.FirstOrDefaultAsync(rol => rol.Id == id, ct);
    }

    public Task<bool> ExisteAsignacionAsync(Guid usuarioId, Guid rolId, Guid? areaId, CancellationToken ct = default)
    {
        return _db.AsignacionesRol.AnyAsync(
            asignacion => asignacion.UsuarioId == usuarioId && asignacion.RolId == rolId && asignacion.AreaId == areaId,
            ct);
    }

    public Task AgregarAsignacionAsync(AsignacionRol asignacion, CancellationToken ct = default)
    {
        _db.AsignacionesRol.Add(asignacion);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> PermisosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var porRol = await _db.AsignacionesRol
            .Where(asignacion => asignacion.UsuarioId == usuarioId)
            .SelectMany(asignacion => asignacion.Rol.Permisos)
            .Select(vinculo => vinculo.Permiso.Codigo)
            .ToListAsync(ct);

        var directos = await _db.Set<AsignacionPermiso>()
            .Where(vinculo => vinculo.UsuarioId == usuarioId)
            .Select(vinculo => vinculo.Permiso.Codigo)
            .ToListAsync(ct);

        var codigos = porRol.Concat(directos).Distinct(StringComparer.Ordinal).ToList();
        var usuario = await _db.Usuarios.Include(item => item.AreaInteres).FirstOrDefaultAsync(item => item.Id == usuarioId, ct);
        var agenda = AccesoAgenda.PermisoPorCuenta(usuario?.AreaInteres?.Codigo, usuario?.Cargo);
        if (agenda is not null && !codigos.Contains(agenda, StringComparer.Ordinal))
        {
            codigos.Add(agenda);
        }

        return codigos;
    }

    public async Task<IReadOnlyList<string>> PermisosDirectosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        return await _db.Set<AsignacionPermiso>()
            .Where(vinculo => vinculo.UsuarioId == usuarioId)
            .Select(vinculo => vinculo.Permiso.Codigo)
            .ToListAsync(ct);
    }

    public async Task ReemplazarPermisosDirectosAsync(Guid usuarioId, IReadOnlyList<string> codigos, CancellationToken ct = default)
    {
        var actuales = await _db.Set<AsignacionPermiso>()
            .Where(vinculo => vinculo.UsuarioId == usuarioId)
            .ToListAsync(ct);
        _db.Set<AsignacionPermiso>().RemoveRange(actuales);

        var herramientas = CatalogoHerramientas.Todas.Select(item => item.CodigoPermiso).ToHashSet(StringComparer.Ordinal);
        var elegidos = codigos.Where(codigo => herramientas.Contains(codigo)).Distinct(StringComparer.Ordinal).ToList();
        var permisos = await _db.Permisos.Where(permiso => elegidos.Contains(permiso.Codigo)).ToListAsync(ct);
        foreach (var permiso in permisos)
        {
            _db.Set<AsignacionPermiso>().Add(new AsignacionPermiso { UsuarioId = usuarioId, PermisoId = permiso.Id });
        }
    }

    public async Task ReemplazarRolPlataformaAsync(Guid usuarioId, string codigoRol, CancellationToken ct = default)
    {
        var rol = await _db.Roles.FirstOrDefaultAsync(item => item.Codigo == codigoRol, ct);
        if (rol is null)
        {
            return;
        }

        var roles = new[] { PermisosNucleo.RolUsuario, PermisosNucleo.RolJefeArea, PermisosNucleo.RolAdministrador };
        var actuales = await _db.AsignacionesRol
            .Include(asignacion => asignacion.Rol)
            .Where(asignacion => asignacion.UsuarioId == usuarioId && roles.Contains(asignacion.Rol.Codigo))
            .ToListAsync(ct);
        _db.AsignacionesRol.RemoveRange(actuales);
        _db.AsignacionesRol.Add(new AsignacionRol
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            RolId = rol.Id
        });
    }

    public async Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarUsuariosAsync(int pagina, int tamano, string? busqueda, string? estado = null, CancellationToken ct = default)
    {
        var consulta = _db.Usuarios
            .Include(usuario => usuario.AreaInteres)
            .Include(usuario => usuario.Asignaciones)
            .ThenInclude(asignacion => asignacion.Rol)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(usuario =>
                usuario.Nombre.Contains(texto) || usuario.CorreoElectronico.Contains(texto));
        }

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoCuenta>(estado, out var estadoCuenta))
        {
            consulta = consulta.Where(usuario => usuario.Estado == estadoCuenta);
        }

        var total = await consulta.CountAsync(ct);
        var elementos = await consulta
            .OrderBy(usuario => usuario.Nombre)
            .Skip(pagina * tamano)
            .Take(tamano)
            .ToListAsync(ct);
        return (elementos, total);
    }

    public async Task<IReadOnlyList<Rol>> ListarRolesAsync(CancellationToken ct = default)
    {
        return await _db.Roles.Include(rol => rol.Permisos).ThenInclude(vinculo => vinculo.Permiso)
            .OrderBy(rol => rol.Nombre)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Permiso>> ListarPermisosAsync(CancellationToken ct = default)
    {
        return await _db.Permisos.OrderBy(permiso => permiso.Codigo).ToListAsync(ct);
    }

    public Task AgregarEventoAsync(EventoAuditoria evento, CancellationToken ct = default)
    {
        _db.EventosAuditoria.Add(evento);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<EventoAuditoria> Elementos, int Total)> ListarEventosAsync(int pagina, int tamano, CancellationToken ct = default)
    {
        var total = await _db.EventosAuditoria.CountAsync(ct);
        var elementos = await _db.EventosAuditoria
            .OrderByDescending(evento => evento.FechaUtc)
            .Skip(pagina * tamano)
            .Take(tamano)
            .ToListAsync(ct);
        return (elementos, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> NombresUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToArray();
        return await _db.Usuarios
            .Where(usuario => lista.Contains(usuario.Id))
            .ToDictionaryAsync(usuario => usuario.Id, usuario => usuario.Nombre, ct);
    }

    public async Task<IReadOnlyList<Nota>> ListarNotasAsync(Guid usuarioId, CancellationToken ct = default)
    {
        return await _db.Notas
            .Where(nota => nota.UsuarioId == usuarioId)
            .OrderByDescending(nota => nota.FechaActualizacionUtc ?? nota.FechaCreacionUtc)
            .ToListAsync(ct);
    }

    public Task<Nota?> BuscarNotaAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Notas.FirstOrDefaultAsync(nota => nota.Id == id, ct);
    }

    public Task AgregarNotaAsync(Nota nota, CancellationToken ct = default)
    {
        _db.Notas.Add(nota);
        return Task.CompletedTask;
    }

    public Task EliminarNotaAsync(Nota nota, CancellationToken ct = default)
    {
        _db.Notas.Remove(nota);
        return Task.CompletedTask;
    }

    public Task GuardarAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
