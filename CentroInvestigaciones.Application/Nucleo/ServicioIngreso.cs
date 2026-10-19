using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Nucleo;

public sealed class ServicioIngreso
{
    private readonly IAlmacenNucleo _almacen;
    private readonly string? _correoAdministradorInicial;

    public ServicioIngreso(IAlmacenNucleo almacen, string? correoAdministradorInicial)
    {
        _almacen = almacen;
        _correoAdministradorInicial = correoAdministradorInicial;
    }

    public async Task<ResultadoIngreso> RegistrarAsync(
        string identificador,
        string? correo,
        string? nombre,
        string? direccion,
        string? agente,
        CancellationToken ct = default)
    {
        if (!_almacen.Disponible)
        {
            return new ResultadoIngreso(false, "/", false);
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, ct);
        var cuentaNueva = usuario is null;
        var ahora = DateTime.UtcNow;

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                IdentificadorProveedor = identificador,
                CorreoElectronico = correo?.Trim() ?? "",
                Nombre = nombre?.Trim() ?? "",
                Estado = EstadoCuenta.PendienteAprobacion,
                FechaCreacionUtc = ahora,
                UltimoAccesoUtc = ahora
            };
            await _almacen.AgregarUsuarioAsync(usuario, ct);
        }
        else
        {
            usuario.UltimoAccesoUtc = ahora;
            if (!string.IsNullOrWhiteSpace(correo))
            {
                usuario.CorreoElectronico = correo.Trim();
            }

            if (!string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(usuario.Nombre))
            {
                usuario.Nombre = nombre.Trim();
            }
        }

        await PromoverAdministradorInicialAsync(usuario, ct);
        await AsegurarRolUsuarioAsync(usuario, ct);

        await _almacen.AgregarEventoAsync(new EventoAuditoria
        {
            Id = Guid.NewGuid(),
            FechaUtc = ahora,
            UsuarioId = usuario.Id,
            Accion = cuentaNueva ? "Creacion" : "InicioSesion",
            Entidad = "Usuario",
            EntidadId = usuario.Id.ToString(),
            Resultado = "Realizado",
            ValoresNuevos = usuario.Estado.ToString(),
            DireccionRed = direccion,
            AgenteUsuario = agente is { Length: > 256 } ? agente[..256] : agente
        }, ct);

        await _almacen.GuardarAsync(ct);

        var destino = !usuario.PerfilCompleto
            ? "/primer-ingreso"
            : usuario.Estado == EstadoCuenta.Activo
                ? "/"
                : "/estado-cuenta";

        return new ResultadoIngreso(true, destino, cuentaNueva);
    }

    public async Task<bool> CompletarPerfilAsync(
        string identificador,
        string nombres,
        string apellidos,
        string numeroContacto,
        string extension,
        string cargo,
        Guid areaId,
        CancellationToken ct = default)
    {
        if (!_almacen.Disponible)
        {
            return false;
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, ct);
        if (usuario is null || usuario.PerfilCompleto)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(nombres)
            || string.IsNullOrWhiteSpace(apellidos)
            || string.IsNullOrWhiteSpace(numeroContacto))
        {
            return false;
        }

        usuario.Nombres = nombres.Trim();
        usuario.Apellidos = apellidos.Trim();
        usuario.NumeroContacto = numeroContacto.Trim();
        usuario.Extension = extension?.Trim() ?? "";
        usuario.Nombre = $"{usuario.Nombres} {usuario.Apellidos}".Trim();

        await _almacen.AgregarEventoAsync(new EventoAuditoria
        {
            Id = Guid.NewGuid(),
            FechaUtc = DateTime.UtcNow,
            UsuarioId = usuario.Id,
            Accion = "CompletarPerfil",
            Entidad = "Usuario",
            EntidadId = usuario.Id.ToString(),
            Resultado = "Realizado",
            ValoresNuevos = usuario.Nombre,
            DireccionRed = null,
            AgenteUsuario = null
        }, ct);
        await _almacen.GuardarAsync(ct);
        return true;
    }

    private async Task PromoverAdministradorInicialAsync(Usuario usuario, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_correoAdministradorInicial))
        {
            return;
        }

        if (!string.Equals(usuario.CorreoElectronico, _correoAdministradorInicial.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (await _almacen.ExisteAdministradorPlataformaAsync(ct))
        {
            return;
        }

        var rol = await _almacen.BuscarRolPorCodigoAsync(PermisosNucleo.RolAdministrador, ct);
        if (rol is null || await _almacen.ExisteAsignacionAsync(usuario.Id, rol.Id, null, ct))
        {
            return;
        }

        usuario.Estado = EstadoCuenta.Activo;
        await _almacen.AgregarAsignacionAsync(new AsignacionRol
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            RolId = rol.Id
        }, ct);
    }

    private async Task AsegurarRolUsuarioAsync(Usuario usuario, CancellationToken ct)
    {
        foreach (var codigo in PermisosNucleo.RolesDePlataforma)
        {
            var existente = await _almacen.BuscarRolPorCodigoAsync(codigo, ct);
            if (existente is not null && await _almacen.ExisteAsignacionAsync(usuario.Id, existente.Id, null, ct))
            {
                return;
            }
        }

        var rol = await _almacen.BuscarRolPorCodigoAsync(PermisosNucleo.RolUsuario, ct);
        if (rol is null)
        {
            return;
        }

        await _almacen.AgregarAsignacionAsync(new AsignacionRol
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            RolId = rol.Id
        }, ct);
    }
}
