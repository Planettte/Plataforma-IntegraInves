namespace CentroInvestigaciones.Domain;

public sealed class DispositivoMedico
{
    public Guid Id { get; set; }

    public string Dm { get; set; } = "";

    public string Marca { get; set; } = "";

    public string Modelo { get; set; } = "";

    public string Riesgo { get; set; } = "";

    public string Serie { get; set; } = "";

    public string CodigoActivo { get; set; } = "";

    public string InventarioBiomedica { get; set; } = "";

    public string RegistroInvima { get; set; } = "";

    public string PeriodicidadMantenimiento { get; set; } = "";

    public string UltimoMantenimiento { get; set; } = "";

    public string MesCalificacion { get; set; } = "";

    public string MesVerificacionMetrologica { get; set; } = "";

    public string PeriodicidadCalibracion { get; set; } = "";

    public string UltimaCalibracion { get; set; } = "";

    public string Ubicacion { get; set; } = "";

    public List<MovimientoUbicacion> HistorialUbicacion { get; set; } = [];

    public string Area { get; set; } = "";

    public string Observaciones { get; set; } = "";

    public string CalificacionVentana1 { get; set; } = "";

    public string CalificacionVentana2 { get; set; } = "";
}

public sealed class MovimientoUbicacion
{
    public string Ubicacion { get; set; } = "";

    public DateTime FechaUtc { get; set; }

    public string Persona { get; set; } = "";
}
