namespace CentroInvestigaciones.Shared;

public sealed class RetencionDocumentalDto
{
    public string NumeroEstudio { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public List<string> Contenido { get; set; } = [];

    public List<DireccionRetencionDto> Direcciones { get; set; } = [];

    public List<PrestamoRetencionDto> Prestamos { get; set; } = [];
}

public sealed class DireccionRetencionDto
{
    public string Fecha1 { get; set; } = "";

    public string Direccion1 { get; set; } = "";

    public string Fecha2 { get; set; } = "";

    public string Direccion2 { get; set; } = "";
}

public sealed class PrestamoRetencionDto
{
    public string FechaPrestamo { get; set; } = "";

    public string NombreDocumento { get; set; } = "";

    public string Motivo { get; set; } = "";

    public string NombreSolicitante { get; set; } = "";

    public string FirmaSolicitante { get; set; } = "";

    public string FechaDevolucion { get; set; } = "";

    public string FirmaRecibe { get; set; } = "";
}
