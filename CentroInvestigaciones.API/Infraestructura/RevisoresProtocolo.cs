using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public static class RevisoresProtocolo
{
    public const string Permiso = "herramientas.udd.protocolos";

    public static async Task<List<RevisorProtocoloDto>> ListarAsync(IAlmacenNucleo nucleo, CancellationToken ct)
    {
        var (usuarios, _) = await nucleo.ListarUsuariosAsync(0, 500, null, nameof(EstadoCuenta.Activo), ct);
        var lista = new List<RevisorProtocoloDto>();
        foreach (var usuario in usuarios.OrderBy(item => item.Nombre))
        {
            if (await EsMedicoUddAsync(nucleo, usuario.Id, ct))
            {
                lista.Add(new RevisorProtocoloDto { Id = usuario.Id, Nombre = usuario.Nombre });
            }
        }

        return lista;
    }

    public static async Task<bool> TienePermisoAsync(IAlmacenNucleo nucleo, Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var usuario = await nucleo.BuscarPorIdAsync(id, ct);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return false;
        }

        var permisos = await nucleo.PermisosDeUsuarioAsync(id, ct);
        return permisos.Contains(Permiso, StringComparer.Ordinal);
    }

    public static async Task<bool> EsMedicoUddAsync(IAlmacenNucleo nucleo, Guid id, CancellationToken ct)
    {
        if (!await TienePermisoAsync(nucleo, id, ct))
        {
            return false;
        }

        var usuario = await nucleo.BuscarPorIdAsync(id, ct);
        return AccesoAgenda.Normalizar(usuario?.AreaInteres?.Codigo) == AccesoAgenda.AreaUdd
            && AccesoAgenda.Normalizar(usuario?.Cargo) == AccesoAgenda.CargoUdd;
    }

    public static async Task<(string Nombre, string Correo)> DatosAsync(IAlmacenNucleo nucleo, Guid id, CancellationToken ct)
    {
        var usuario = await nucleo.BuscarPorIdAsync(id, ct);
        return (usuario?.Nombre ?? "", usuario?.CorreoElectronico ?? "");
    }
}
