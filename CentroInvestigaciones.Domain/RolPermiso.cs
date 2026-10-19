namespace CentroInvestigaciones.Domain;

public class RolPermiso
{
    public Guid RolId { get; set; }

    public Rol Rol { get; set; } = null!;

    public Guid PermisoId { get; set; }

    public Permiso Permiso { get; set; } = null!;
}
