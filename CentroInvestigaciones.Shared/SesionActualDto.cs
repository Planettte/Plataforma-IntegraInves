namespace CentroInvestigaciones.Shared;

public sealed class SesionActualDto
{
    public bool Autenticado { get; set; }

    public string? Nombre { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? NumeroContacto { get; set; }

    public string? Extension { get; set; }

    public Guid? AreaInteresId { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? IdentificadorProveedor { get; set; }

    public string? EstadoCuenta { get; set; }

    public string? AreaInteres { get; set; }

    public string? AreaCodigo { get; set; }

    public string? Cargo { get; set; }

    public string? TemaPreferido { get; set; }

    public bool RequiereCompletarIngreso { get; set; }

    public bool RequiereElegirCargo { get; set; }

    public IReadOnlyList<string> Permisos { get; set; } = [];
}
