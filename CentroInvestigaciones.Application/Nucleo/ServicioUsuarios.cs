using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Nucleo;

public sealed class ServicioUsuarios
{
    private readonly IAlmacenNucleo _almacen;

    public ServicioUsuarios(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    public async Task<bool> CambiarEstadoAsync(Guid usuarioId, EstadoCuenta estado, Guid actorId, string? direccion, CancellationToken ct = default)
    {
        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado == estado)
        {
            return false;
        }

        var anterior = usuario.Estado;
        usuario.Estado = estado;
        if (estado == EstadoCuenta.Activo && usuario.AreaInteresId is not null
            && usuario.Areas.All(vinculo => vinculo.AreaId != usuario.AreaInteresId))
        {
            usuario.Areas.Add(new UsuarioArea { UsuarioId = usuario.Id, AreaId = usuario.AreaInteresId.Value });
        }

        await RegistrarAsync(actorId, estado == EstadoCuenta.Activo ? "Aprobacion" : estado == EstadoCuenta.Rechazado ? "Rechazo" : "CambioEstado", usuario.Id, anterior.ToString(), estado.ToString(), direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    public async Task<bool> AsignarRolAsync(Guid usuarioId, Guid rolId, Guid? areaId, Guid actorId, string? direccion, CancellationToken ct = default)
    {
        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null
            || await _almacen.BuscarRolPorIdAsync(rolId, ct) is null
            || await _almacen.ExisteAsignacionAsync(usuarioId, rolId, areaId, ct))
        {
            return false;
        }

        if (areaId is not null && await _almacen.BuscarAreaAsync(areaId.Value, ct) is null)
        {
            return false;
        }

        var asignacion = new AsignacionRol
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            RolId = rolId,
            AreaId = areaId
        };
        await _almacen.AgregarAsignacionAsync(asignacion, ct);
        await RegistrarAsync(actorId, "CambioRol", usuarioId, null, rolId.ToString(), direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    public async Task<bool> AsignarHerramientasAsync(Guid usuarioId, IReadOnlyList<string> codigos, Guid actorId, string? direccion, CancellationToken ct = default)
    {
        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null)
        {
            return false;
        }

        var validos = CatalogoHerramientas.Todas.Select(item => item.CodigoPermiso).ToHashSet(StringComparer.Ordinal);
        var elegidos = codigos.Where(validos.Contains).Distinct(StringComparer.Ordinal).ToList();
        await _almacen.ReemplazarPermisosDirectosAsync(usuarioId, elegidos, ct);
        await SincronizarAgendaAsync(usuario, ct);
        var vigentes = await _almacen.PermisosDirectosDeUsuarioAsync(usuarioId, ct);
        await RegistrarAsync(actorId, "AsignacionHerramientas", usuarioId, null, string.Join(",", vigentes), direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    public async Task<bool> ActualizarDatosAsync(
        Guid usuarioId,
        string nombres,
        string apellidos,
        string numeroContacto,
        string extension,
        string cargo,
        Guid areaId,
        bool administraArea,
        Guid actorId,
        string? direccion,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombres)
            || string.IsNullOrWhiteSpace(apellidos)
            || string.IsNullOrWhiteSpace(numeroContacto))
        {
            return false;
        }

        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null)
        {
            return false;
        }

        if (administraArea)
        {
            var area = await _almacen.BuscarAreaAsync(areaId, ct);
            if (area is null)
            {
                return false;
            }

            usuario.AreaInteresId = area.Id;
            usuario.AreaInteres = area;
            usuario.Cargo = AccesoAgenda.CargoAlAsignarArea(area.Codigo, cargo);
            if (string.IsNullOrWhiteSpace(usuario.Cargo))
            {
                return false;
            }
        }

        usuario.Nombres = nombres.Trim();
        usuario.Apellidos = apellidos.Trim();
        usuario.NumeroContacto = numeroContacto.Trim();
        usuario.Extension = extension?.Trim() ?? "";
        usuario.Nombre = $"{usuario.Nombres} {usuario.Apellidos}".Trim();
        await SincronizarAgendaAsync(usuario, ct);
        await RegistrarAsync(actorId, "ActualizacionPerfil", usuarioId, null, usuario.AreaInteres?.Codigo ?? usuario.Cargo, direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    public async Task<bool> AsignarAreaCargoAsync(Guid usuarioId, Guid areaId, string cargo, Guid actorId, string? direccion, CancellationToken ct = default)
    {
        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        var area = await _almacen.BuscarAreaAsync(areaId, ct);
        if (usuario is null || area is null)
        {
            return false;
        }

        var asignado = AccesoAgenda.CargoAlAsignarArea(area.Codigo, cargo);
        if (string.IsNullOrWhiteSpace(asignado))
        {
            return false;
        }

        usuario.AreaInteresId = area.Id;
        usuario.AreaInteres = area;
        usuario.Cargo = asignado;
        await SincronizarAgendaAsync(usuario, ct);
        await RegistrarAsync(actorId, "AsignacionAreaCargo", usuarioId, null, area.Codigo + "|" + asignado, direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    public async Task<bool> EstablecerAccesoAsync(
        Guid usuarioId,
        string codigoRol,
        IReadOnlyList<string> herramientas,
        Guid actorId,
        string? direccion,
        CancellationToken ct = default)
    {
        if (!PermisosNucleo.RolesDePlataforma.Contains(codigoRol, StringComparer.Ordinal))
        {
            return false;
        }

        var usuario = await _almacen.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null || await _almacen.BuscarRolPorCodigoAsync(codigoRol, ct) is null)
        {
            return false;
        }

        var validos = CatalogoHerramientas.Todas.Select(item => item.CodigoPermiso).ToHashSet(StringComparer.Ordinal);
        var elegidos = herramientas.Where(validos.Contains).Distinct(StringComparer.Ordinal).ToList();
        await _almacen.ReemplazarRolPlataformaAsync(usuarioId, codigoRol, ct);
        await _almacen.ReemplazarPermisosDirectosAsync(usuarioId, elegidos, ct);
        await SincronizarAgendaAsync(usuario, ct);
        var vigentes = await _almacen.PermisosDirectosDeUsuarioAsync(usuarioId, ct);
        await RegistrarAsync(actorId, "AccesoUsuario", usuarioId, null, codigoRol + "|" + string.Join(",", vigentes), direccion, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    private async Task SincronizarAgendaAsync(Usuario usuario, CancellationToken ct)
    {
        var actuales = await _almacen.PermisosDirectosDeUsuarioAsync(usuario.Id, ct);
        var codigos = actuales
            .Where(codigo => !AccesoAgenda.Permisos.Contains(codigo, StringComparer.Ordinal))
            .ToList();
        var permiso = AccesoAgenda.PermisoPorCuenta(usuario.AreaInteres?.Codigo, usuario.Cargo);
        if (permiso is not null)
        {
            codigos.Add(permiso);
        }

        await _almacen.ReemplazarPermisosDirectosAsync(usuario.Id, codigos, ct);
    }

    private async Task RegistrarAsync(Guid actorId, string accion, Guid entidadId, string? anterior, string? nuevo, string? direccion, CancellationToken ct)
    {
        await _almacen.AgregarEventoAsync(new EventoAuditoria
        {
            Id = Guid.NewGuid(),
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorId,
            Accion = accion,
            Entidad = "Usuario",
            EntidadId = entidadId.ToString(),
            Resultado = "Realizado",
            ValoresAnteriores = anterior,
            ValoresNuevos = nuevo,
            DireccionRed = direccion
        }, ct);
    }
}
