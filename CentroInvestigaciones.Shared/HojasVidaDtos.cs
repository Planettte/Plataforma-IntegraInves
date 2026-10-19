namespace CentroInvestigaciones.Shared;

public sealed class HojaVidaResumenDto
{
    public Guid Id { get; set; }

    public string NombreCompleto { get; set; } = "";

    public string TituloAcademico { get; set; } = "";

    public int Version { get; set; }

    public DateTime FechaEdicionUtc { get; set; }
}

public sealed class HojaVidaDto
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

    public List<FormacionHojaDto> Formacion { get; set; } = [];

    public List<LaboralHojaDto> ExperienciaLaboral { get; set; } = [];

    public List<InvestigacionHojaDto> ExperienciaInvestigacion { get; set; } = [];

    public string GcpSiNo { get; set; } = "No";

    public string GcpDetalle { get; set; } = "";

    public string OtrosSiNo { get; set; } = "No";

    public string OtrosDetalle { get; set; } = "";

    public bool TieneFirma { get; set; }

    public string? FirmaNombre { get; set; }

    public string FirmaFecha { get; set; } = "";

    public int Version { get; set; }

    public DateTime FechaEdicionUtc { get; set; }

    public List<SoporteHojaDto> Soportes { get; set; } = [];
}

public sealed class FormacionHojaDto
{
    public string Institucion { get; set; } = "";

    public string Titulo { get; set; } = "";

    public string Anio { get; set; } = "";

    public string Area { get; set; } = "";
}

public sealed class LaboralHojaDto
{
    public string Posicion { get; set; } = "";

    public string Institucion { get; set; } = "";

    public string Locacion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaFin { get; set; } = "";
}

public sealed class InvestigacionHojaDto
{
    public string Codigo { get; set; } = "";

    public string Indicacion { get; set; } = "";

    public string Fase { get; set; } = "";

    public string Papel { get; set; } = "";

    public string Anio { get; set; } = "";
}

public sealed class SoporteHojaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Tipo { get; set; } = "";

    public int Orden { get; set; }
}

public sealed class EventoHojaVidaDto
{
    public int Version { get; set; }

    public DateTime FechaUtc { get; set; }

    public string Usuario { get; set; } = "";

    public string Nota { get; set; } = "";
}

public sealed class CrearHojaVidaDto
{
    public string NombreCompleto { get; set; } = "";

    public string TituloAcademico { get; set; } = "";
}

public sealed class GuardarHojaVidaDto
{
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

    public List<FormacionHojaDto> Formacion { get; set; } = [];

    public List<LaboralHojaDto> ExperienciaLaboral { get; set; } = [];

    public List<InvestigacionHojaDto> ExperienciaInvestigacion { get; set; } = [];

    public string GcpSiNo { get; set; } = "No";

    public string GcpDetalle { get; set; } = "";

    public string OtrosSiNo { get; set; } = "No";

    public string OtrosDetalle { get; set; } = "";

    public string FirmaFecha { get; set; } = "";
}

public sealed class ResultadoGuardadoHojaDto
{
    public HojaVidaDto Hoja { get; set; } = new();

    public bool VersionNueva { get; set; }
}

public sealed class SolicitudPdfHojaDto
{
    public string Idioma { get; set; } = "es";

    public string? Nota { get; set; }

    public List<Guid> SoportesIds { get; set; } = [];
}

public sealed class OrdenSoportesDto
{
    public List<Guid> Ids { get; set; } = [];
}
