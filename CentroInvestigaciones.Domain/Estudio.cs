namespace CentroInvestigaciones.Domain;

public static class TiposEstudio
{
    public const string Academico = "academico";
    public const string Clinico = "clinico";
}

public static class EstadosEstudio
{
    public const string RecepcionIncompleta = "Recepción incompleta de documentos para el comité de investigaciones";
    public const string AprobadoComite = "Aprobado por el Comité de Investigaciones";
    public const string Fase2EnRevision = "Documentos de la fase 2 en revisión";
    public const string AprobadoCei = "Aprobado por el CEI";
}

public static class ClavesArchivoEstudio
{
    public const string Adicional = "adicional";
    public const string AvalComite = "aval-comite";
    public const string Fase2Aval = "fase2-aval";
    public const string Fase2Cei = "fase2-cei";
    public const string Fase2Acta = "fase2-acta";
    public const string PdfCartaSolicitud = "pdf-carta-solicitud";
    public const string PdfCartaIntencion = "pdf-carta-intencion";
    public const string PdfAcuerdo = "pdf-acuerdo";
    public const string PdfRegistro = "pdf-registro";
    public const string PdfHojaVida = "pdf-hoja-vida";
    public const string PdfResumen = "pdf-resumen";
    public const string FirmaCartaSolicitud = "firma-carta-solicitud";
    public const string FirmaCartaIntencion = "firma-carta-intencion";
    public const string FirmaAcuerdo = "firma-acuerdo";
    public const string FirmaRegistro = "firma-registro";
    public const string FirmaHojaVida = "firma-hoja-vida";

    public static bool EsPdf(string clave) => codigoEmpieza(clave, "pdf-");

    public static bool EsFirma(string clave) => codigoEmpieza(clave, "firma-");

    private static bool codigoEmpieza(string clave, string prefijo) =>
        clave.StartsWith(prefijo, StringComparison.Ordinal);
}

public sealed class Estudio
{
    public Guid Id { get; set; }

    public int Conteo { get; set; }

    public string TipoRegistro { get; set; } = TiposEstudio.Academico;

    public string Token { get; set; } = "";

    public string Estado { get; set; } = EstadosEstudio.RecepcionIncompleta;

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

    public DateTime FechaEdicionUtc { get; set; }

    public FormatosAcademicos Formatos { get; set; } = new();

    public List<ArchivoEstudio> Archivos { get; set; } = [];
}

public sealed class ArchivoEstudio
{
    public Guid Id { get; set; }

    public string Clave { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string TipoContenido { get; set; } = "application/octet-stream";

    public string Ruta { get; set; } = "";

    public DateTime FechaUtc { get; set; }
}

public sealed class FormatosAcademicos
{
    public CartaSolicitudEstudio CartaSolicitud { get; set; } = new();

    public CartaIntencionEstudio CartaIntencion { get; set; } = new();

    public AcuerdoConfidencialidadEstudio Acuerdo { get; set; } = new();

    public FormularioRegistroEstudio Registro { get; set; } = new();

    public HojaVidaEstudio HojaVida { get; set; } = new();

    public ResumenAnaliticoEstudio Resumen { get; set; } = new();
}

public sealed class CartaSolicitudEstudio
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

public sealed class CartaIntencionEstudio
{
    public string TituloProtocolo { get; set; } = "";

    public string Nombre { get; set; } = "";
}

public sealed class AcuerdoConfidencialidadEstudio
{
    public string Nombre { get; set; } = "";

    public string Rol { get; set; } = "";

    public string Cedula { get; set; } = "";

    public string Expedicion { get; set; } = "";

    public string Protocolo { get; set; } = "";

    public string NombreFirma { get; set; } = "";
}

public sealed class FormularioRegistroEstudio
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

public sealed class HojaVidaEstudio
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

public sealed class ResumenAnaliticoEstudio
{
    public string Titulo { get; set; } = "";

    public string Linea { get; set; } = "";

    public string Grupo { get; set; } = "";

    public string Area { get; set; } = "";

    public string Institucion1 { get; set; } = "";

    public string Institucion2 { get; set; } = "";

    public string Institucion3 { get; set; } = "";

    public string AnioResidencia { get; set; } = "";

    public List<PersonaRolEstudio> Investigadores { get; set; } = [];

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

    public List<PartidaPresupuestoEstudio> Presupuesto { get; set; } = [];

    public List<ActividadCronogramaEstudio> Cronograma { get; set; } = [];
}

public sealed class PersonaRolEstudio
{
    public string Nombre { get; set; } = "";

    public string Rol { get; set; } = "";
}

public sealed class PartidaPresupuestoEstudio
{
    public string Item { get; set; } = "";

    public string Horas { get; set; } = "";

    public string Valor { get; set; } = "";

    public string Responsable { get; set; } = "";

    public string Observaciones { get; set; } = "";
}

public sealed class ActividadCronogramaEstudio
{
    public string Actividad { get; set; } = "";

    public string MesInicial { get; set; } = "";

    public string MesFinal { get; set; } = "";
}

public sealed class CostosComite
{
    public const string ItemEtica = "COSTO COMITÉ DE ETICA";

    public const string ItemInvestigacion = "COSTOS COMITÉ DE INVESTIGACION";

    public string ValorEtica { get; set; } = "";

    public string ResponsableEtica { get; set; } = "";

    public string ValorInvestigacion { get; set; } = "";

    public string ResponsableInvestigacion { get; set; } = "";
}
