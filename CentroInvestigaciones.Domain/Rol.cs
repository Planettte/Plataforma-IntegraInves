namespace CentroInvestigaciones.Domain;

public class Rol
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public List<RolPermiso> Permisos { get; set; } = [];
}
