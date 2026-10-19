namespace CentroInvestigaciones.Domain;

public class AsignacionPermiso
{
    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Guid PermisoId { get; set; }

    public Permiso Permiso { get; set; } = null!;
}
