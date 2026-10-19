namespace CentroInvestigaciones.Domain;

public class Usuario
{
    public Guid Id { get; set; }

    public string IdentificadorProveedor { get; set; } = "";

    public string CorreoElectronico { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Nombres { get; set; } = "";

    public string Apellidos { get; set; } = "";

    public string NumeroContacto { get; set; } = "";

    public string Extension { get; set; } = "";

    public string Cargo { get; set; } = "";

    public string TemaPreferido { get; set; } = "claro";

    public EstadoCuenta Estado { get; set; }

    public Guid? AreaInteresId { get; set; }

    public Area? AreaInteres { get; set; }

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime? UltimoAccesoUtc { get; set; }

    public bool PerfilCompleto =>
        !string.IsNullOrWhiteSpace(Nombres)
        && !string.IsNullOrWhiteSpace(Apellidos)
        && !string.IsNullOrWhiteSpace(NumeroContacto);

    public List<UsuarioArea> Areas { get; set; } = [];

    public List<AsignacionRol> Asignaciones { get; set; } = [];

    public List<AsignacionPermiso> PermisosDirectos { get; set; } = [];
}
