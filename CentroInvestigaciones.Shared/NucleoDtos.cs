namespace CentroInvestigaciones.Shared;

public sealed class AreaDto
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";
}

public sealed class CompletarIngresoDto
{
    public string Nombres { get; set; } = "";

    public string Apellidos { get; set; } = "";

    public string NumeroContacto { get; set; } = "";

    public string Extension { get; set; } = "";

    public string Cargo { get; set; } = "";

    public Guid AreaInteresId { get; set; }
}

public sealed class CambiarEstadoDto
{
    public string Estado { get; set; } = "";
}

public sealed class AsignarAreaCargoDto
{
    public Guid AreaId { get; set; }

    public string Cargo { get; set; } = "";
}

public sealed class AsignarRolDto
{
    public Guid RolId { get; set; }

    public Guid? AreaId { get; set; }
}

public sealed class AsignarHerramientasDto
{
    public IReadOnlyList<string> CodigosPermiso { get; set; } = [];
}

public sealed class GuardarAccesoUsuarioDto
{
    public string Rol { get; set; } = "";

    public IReadOnlyList<string> Herramientas { get; set; } = [];
}

public sealed class TemaPreferidoDto
{
    public string Tema { get; set; } = "claro";
}

public sealed class ElegirCargoDto
{
    public string Cargo { get; set; } = "";
}

public sealed class HerramientaDto
{
    public string CodigoPermiso { get; set; } = "";

    public string AreaCodigo { get; set; } = "";

    public string AreaNombre { get; set; } = "";

    public string Titulo { get; set; } = "";

    public string Ruta { get; set; } = "";

    public string? Icono { get; set; }
}

public sealed class UsuarioResumenDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Nombres { get; set; } = "";

    public string Apellidos { get; set; } = "";

    public string Extension { get; set; } = "";

    public Guid? AreaInteresId { get; set; }

    public string Rol { get; set; } = "";

    public string CorreoElectronico { get; set; } = "";

    public string EstadoCuenta { get; set; } = "";

    public string? AreaInteres { get; set; }

    public string? Cargo { get; set; }

    public string? NumeroContacto { get; set; }

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime? UltimoAccesoUtc { get; set; }
}

public sealed class RolDto
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public IReadOnlyList<string> Permisos { get; set; } = [];
}

public sealed class PermisoDto
{
    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";
}

public sealed class EventoAuditoriaDto
{
    public DateTime FechaUtc { get; set; }

    public string? Actor { get; set; }

    public string Accion { get; set; } = "";

    public string Entidad { get; set; } = "";

    public string Resultado { get; set; } = "";
}

public sealed class NotaDto
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = "";

    public string Contenido { get; set; } = "";

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime? FechaActualizacionUtc { get; set; }
}

public sealed class GuardarNotaDto
{
    public string Titulo { get; set; } = "";

    public string Contenido { get; set; } = "";
}

public sealed class NotificacionDto
{
    public Guid? Id { get; set; }

    public string Origen { get; set; } = "";

    public string Titulo { get; set; } = "";

    public string Mensaje { get; set; } = "";

    public string? Enlace { get; set; }

    public DateTime FechaUtc { get; set; }

    public bool Vista { get; set; }
}

public sealed class PaginaDto<T>
{
    public IReadOnlyList<T> Elementos { get; set; } = [];

    public int Total { get; set; }
}
