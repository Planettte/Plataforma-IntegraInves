namespace CentroInvestigaciones.Domain;

public class AsignacionRol
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Guid RolId { get; set; }

    public Rol Rol { get; set; } = null!;

    public Guid? AreaId { get; set; }

    public Area? Area { get; set; }
}
