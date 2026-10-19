namespace CentroInvestigaciones.Domain;

public class UsuarioArea
{
    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Guid AreaId { get; set; }

    public Area Area { get; set; } = null!;
}
