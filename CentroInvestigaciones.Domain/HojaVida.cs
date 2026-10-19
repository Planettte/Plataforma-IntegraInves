namespace CentroInvestigaciones.Domain;

public sealed class HojaVida
{
    public Guid Id { get; set; }

    public string NombreCompleto { get; set; } = "";

    public string TituloAcademico { get; set; } = "";

    public string DirInstitucion { get; set; } = "";

    public string DirCompleta { get; set; } = "";

    public string DirCiudad { get; set; } = "";

    public string DirEstado { get; set; } = "";

    public string DirProvincia { get; set; } = "";

    public string DirPais { get; set; } = "";

    public string DirCodigo { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string Extension { get; set; } = "";

    public string VinculacionModalidad { get; set; } = "";

    public string VinculacionFechaInicio { get; set; } = "";

    public List<FormacionHoja> Formacion { get; set; } = [];

    public List<LaboralHoja> ExperienciaLaboral { get; set; } = [];

    public List<InvestigacionHoja> ExperienciaInvestigacion { get; set; } = [];

    public string GcpSiNo { get; set; } = "No";

    public string GcpDetalle { get; set; } = "";

    public string OtrosSiNo { get; set; } = "No";

    public string OtrosDetalle { get; set; } = "";

    public string? FirmaRuta { get; set; }

    public string? FirmaNombre { get; set; }

    public string? FirmaTipo { get; set; }

    public string FirmaFecha { get; set; } = "";

    public int Version { get; set; } = 1;

    public DateTime FechaEdicionUtc { get; set; }

    public List<SoporteHoja> Soportes { get; set; } = [];

    public List<EventoHojaVida> Historial { get; set; } = [];
}

public sealed class FormacionHoja
{
    public string Institucion { get; set; } = "";

    public string Titulo { get; set; } = "";

    public string Anio { get; set; } = "";

    public string Area { get; set; } = "";
}

public sealed class LaboralHoja
{
    public string Posicion { get; set; } = "";

    public string Institucion { get; set; } = "";

    public string Locacion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaFin { get; set; } = "";
}

public sealed class InvestigacionHoja
{
    public string Codigo { get; set; } = "";

    public string Indicacion { get; set; } = "";

    public string Fase { get; set; } = "";

    public string Papel { get; set; } = "";

    public string Anio { get; set; } = "";
}

public sealed class SoporteHoja
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Ruta { get; set; } = "";

    public string Tipo { get; set; } = "";

    public int Orden { get; set; }
}

public sealed class EventoHojaVida
{
    public int Version { get; set; }

    public DateTime FechaUtc { get; set; }

    public string Usuario { get; set; } = "";

    public string Nota { get; set; } = "";
}
