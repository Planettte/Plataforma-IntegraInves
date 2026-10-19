namespace CentroInvestigaciones.Shared;

public sealed class EstudioResumenDto
{
    public Guid Id { get; set; }

    public int Conteo { get; set; }

    public string TipoRegistro { get; set; } = "";

    public string Estado { get; set; } = "";

    public int Anio { get; set; }

    public string Titulo { get; set; } = "";

    public string Institucion { get; set; } = "";

    public string InvestigadorPrincipal { get; set; } = "";

    public string Correo { get; set; } = "";

    public DateTime FechaCreacionUtc { get; set; }
}

public sealed class EstudioDto
{
    public Guid Id { get; set; }

    public int Conteo { get; set; }

    public string TipoRegistro { get; set; } = "";

    public string Token { get; set; } = "";

    public string Estado { get; set; } = "";

    public int Anio { get; set; }

    public string Titulo { get; set; } = "";

    public string Especialidad { get; set; } = "";

    public string Tipo { get; set; } = "";

    public string Acronimo { get; set; } = "";

    public string TipoInvestigacion { get; set; } = "";

    public string Institucion { get; set; } = "";

    public string InvestigadorPrincipal { get; set; } = "";

    public string Correo { get; set; } = "";

    public string CoInvestigadores { get; set; } = "";

    public string Coordinador { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public string Paises { get; set; } = "";

    public string Objetivo { get; set; } = "";

    public string NumeroAprobacionCei { get; set; } = "";

    public string MensajeCorreccion { get; set; } = "";

    public string MensajeAval { get; set; } = "";

    public bool? AvisoRecibidoEnviado { get; set; }

    public DateTime FechaCreacionUtc { get; set; }

    public FormatosAcademicosDto Formatos { get; set; } = new();

    public List<ArchivoEstudioDto> Archivos { get; set; } = [];
}

public sealed class ArchivoEstudioDto
{
    public Guid Id { get; set; }

    public string Clave { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string TipoContenido { get; set; } = "";
}

public sealed class GuardarFase1EstudioDto
{
    public string TipoRegistro { get; set; } = "";

    public int Anio { get; set; }

    public string Titulo { get; set; } = "";

    public string Especialidad { get; set; } = "";

    public string Tipo { get; set; } = "";

    public string Acronimo { get; set; } = "";

    public string TipoInvestigacion { get; set; } = "";

    public string Institucion { get; set; } = "";

    public string InvestigadorPrincipal { get; set; } = "";

    public string Correo { get; set; } = "";

    public string CoInvestigadores { get; set; } = "";

    public string Coordinador { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public string Paises { get; set; } = "";

    public string Objetivo { get; set; } = "";

    public FormatosAcademicosDto Formatos { get; set; } = new();

    public string? FirmaCartaSolicitud { get; set; }

    public string? FirmaCartaIntencion { get; set; }

    public string? FirmaAcuerdo { get; set; }

    public string? FirmaRegistro { get; set; }

    public string? FirmaHojaVida { get; set; }
}

public sealed class MensajeEstudioDto
{
    public string Mensaje { get; set; } = "";
}

public sealed class ReenvioEstudioDto
{
    public string Tipo { get; set; } = "";
}

public sealed class Fase2EstudioDto
{
    public string NumeroAprobacionCei { get; set; } = "";
}

public sealed class ResultadoEstudioDto
{
    public EstudioDto? Estudio { get; set; }

    public bool CorreoEnviado { get; set; }

    public string? Aviso { get; set; }
}

public sealed class FormatosAcademicosDto
{
    public CartaSolicitudEstudioDto CartaSolicitud { get; set; } = new();

    public CartaIntencionEstudioDto CartaIntencion { get; set; } = new();

    public AcuerdoConfidencialidadEstudioDto Acuerdo { get; set; } = new();

    public FormularioRegistroEstudioDto Registro { get; set; } = new();

    public HojaVidaEstudioDto HojaVida { get; set; } = new();

    public ResumenAnaliticoEstudioDto Resumen { get; set; } = new();
}

public sealed class CartaSolicitudEstudioDto
{
    public string Protocolo { get; set; } = "";
    public string VersionProtocolo { get; set; } = "";
    public string FechaProtocolo { get; set; } = "";
    public string IdiomaProtocolo { get; set; } = "";
    public string VersionConsentimiento { get; set; } = "";
    public string FechaConsentimiento { get; set; } = "";
    public string IdiomaConsentimiento { get; set; } = "";
    public string VersionInstrumentos { get; set; } = "";
    public string FechaInstrumentos { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public sealed class CartaIntencionEstudioDto
{
    public string TituloProtocolo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public sealed class AcuerdoConfidencialidadEstudioDto
{
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Cedula { get; set; } = "";
    public string Expedicion { get; set; } = "";
    public string Protocolo { get; set; } = "";
    public string NombreFirma { get; set; } = "";
}

public sealed class FormularioRegistroEstudioDto
{
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string Titulos { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Institucion { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Formacion { get; set; } = "";
    public string Servicio { get; set; } = "";
    public string Experiencia { get; set; } = "";
    public string Publicaciones { get; set; } = "";
}

public sealed class HojaVidaEstudioDto
{
    public string Nombre { get; set; } = "";
    public string Titulos { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Fax { get; set; } = "";
    public string Academica { get; set; } = "";
    public string Laboral { get; set; } = "";
    public string Investigacion { get; set; } = "";
    public string Gcp { get; set; } = "";
}

public sealed class ResumenAnaliticoEstudioDto
{
    public string Titulo { get; set; } = "";
    public string Linea { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string Area { get; set; } = "";
    public string Institucion1 { get; set; } = "";
    public string Institucion2 { get; set; } = "";
    public string Institucion3 { get; set; } = "";
    public string AnioResidencia { get; set; } = "";
    public List<PersonaRolEstudioDto> Investigadores { get; set; } = [];
    public string Problema { get; set; } = "";
    public string Justificacion { get; set; } = "";
    public string ObjetivoGeneral { get; set; } = "";
    public string Objetivo1 { get; set; } = "";
    public string Objetivo2 { get; set; } = "";
    public string Objetivo3 { get; set; } = "";
    public string EstadoArte { get; set; } = "";
    public string TipoEstudio { get; set; } = "";
    public string Poblacion { get; set; } = "";
    public string Muestra { get; set; } = "";
    public string Inclusion { get; set; } = "";
    public string Exclusion { get; set; } = "";
    public string Recoleccion { get; set; } = "";
    public string Analisis { get; set; } = "";
    public string Divulgacion { get; set; } = "";
    public string FechaInicio { get; set; } = "";
    public string Riesgo { get; set; } = "";
    public string Confidencialidad { get; set; } = "";
    public string Seguridad { get; set; } = "";
    public string Consentimiento { get; set; } = "";
    public List<PartidaPresupuestoEstudioDto> Presupuesto { get; set; } = [];
    public List<ActividadCronogramaEstudioDto> Cronograma { get; set; } = [];
}

public sealed class PersonaRolEstudioDto
{
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
}

public sealed class PartidaPresupuestoEstudioDto
{
    public string Item { get; set; } = "";
    public string Horas { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Responsable { get; set; } = "";
    public string Observaciones { get; set; } = "";
}

public sealed class ActividadCronogramaEstudioDto
{
    public string Actividad { get; set; } = "";
    public string MesInicial { get; set; } = "";
    public string MesFinal { get; set; } = "";
}
