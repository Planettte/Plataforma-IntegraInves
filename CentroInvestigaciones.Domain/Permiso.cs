namespace CentroInvestigaciones.Domain;

public class Permiso
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public List<RolPermiso> Roles { get; set; } = [];
}
