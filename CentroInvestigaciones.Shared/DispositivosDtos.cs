namespace CentroInvestigaciones.Shared;

public sealed class SemaforoDispositivoDto
{
    public string Estado { get; set; } = "";

    public string Etiqueta { get; set; } = "";
}

public sealed class DispositivoDto
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

    public string Area { get; set; } = "";

    public string Observaciones { get; set; } = "";

    public SemaforoDispositivoDto Mantenimiento { get; set; } = new();

    public SemaforoDispositivoDto Calibracion { get; set; } = new();

    public SemaforoDispositivoDto Calificacion { get; set; } = new();
}

public sealed class GuardarDispositivoDto
{
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

    public string Area { get; set; } = "";

    public string Observaciones { get; set; } = "";
}

public sealed class EncargadoDispositivosDto
{
    public string Correo { get; set; } = "";
}
