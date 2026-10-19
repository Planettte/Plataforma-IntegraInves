namespace CentroInvestigaciones.Shared;

public sealed class EstadoAutenticacionDto
{
    public bool GoogleConfigurado { get; set; }

    public bool IngresoDesarrollo { get; set; }
}

public sealed class CuentaDesarrolloDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Cargo { get; set; } = "";

    public string Estado { get; set; } = "";
}
