using System.Globalization;

namespace CentroInvestigaciones.Shared;

public sealed class RevisorProtocoloDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";
}

public sealed class ProtocoloListaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";

    public string Formato { get; set; } = "";

    public Guid RevisorId { get; set; }

    public string RevisorNombre { get; set; } = "";

    public string Estado { get; set; } = "";

    public DateTimeOffset CreadoEl { get; set; }
}

public sealed class ProtocoloDto
{
    public Guid Id { get; set; }

    public string Token { get; set; } = "";

    public string Estado { get; set; } = "";

    public string Formato { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";

    public Guid RevisorId { get; set; }

    public string RevisorNombre { get; set; } = "";

    public Dictionary<string, string> Respuestas { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Comentarios { get; set; } = new(StringComparer.Ordinal);

    public List<FilaConocimientoProtocoloDto> Conocimiento { get; set; } = [];

    public List<FilaProductoProtocoloDto> Tecnologico { get; set; } = [];

    public List<FilaProductoProtocoloDto> Capacidad { get; set; } = [];

    public List<FilaCronogramaProtocoloDto> CronogramaFoscal { get; set; } = [];

    public List<FilaCronogramaProtocoloDto> CronogramaUdes { get; set; } = [];

    public List<FilaPresupuestoProtocoloDto> Presupuesto { get; set; } = [];

    public List<bool> EticaPoblacion { get; set; } = [];

    public List<bool> EticaIntervencion { get; set; } = [];

    public List<bool> EticaInstrumentos { get; set; } = [];

    public DateTimeOffset CreadoEl { get; set; }

    public DateTimeOffset EditadoEl { get; set; }
}

public sealed class GuardarProtocoloDto
{
    public string Formato { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";

    public Guid RevisorId { get; set; }

    public Dictionary<string, string> Respuestas { get; set; } = new(StringComparer.Ordinal);

    public List<FilaConocimientoProtocoloDto> Conocimiento { get; set; } = [];

    public List<FilaProductoProtocoloDto> Tecnologico { get; set; } = [];

    public List<FilaProductoProtocoloDto> Capacidad { get; set; } = [];

    public List<FilaCronogramaProtocoloDto> CronogramaFoscal { get; set; } = [];

    public List<FilaCronogramaProtocoloDto> CronogramaUdes { get; set; } = [];

    public List<FilaPresupuestoProtocoloDto> Presupuesto { get; set; } = [];

    public List<bool> EticaPoblacion { get; set; } = [];

    public List<bool> EticaIntervencion { get; set; } = [];

    public List<bool> EticaInstrumentos { get; set; } = [];
}

public sealed class CorreccionProtocoloDto
{
    public Dictionary<string, string> Comentarios { get; set; } = new(StringComparer.Ordinal);
}

public sealed class ResultadoProtocoloDto
{
    public ProtocoloDto? Protocolo { get; set; }

    public bool CorreoEnviado { get; set; }

    public string? Aviso { get; set; }
}

public sealed class FilaConocimientoProtocoloDto
{
    public string Resultado { get; set; } = "";

    public string Indicador { get; set; } = "";

    public string Beneficiario { get; set; } = "";
}

public sealed class FilaProductoProtocoloDto
{
    public string Objetivo { get; set; } = "";

    public string Resultado { get; set; } = "";

    public string Indicador { get; set; } = "";

    public string Beneficiario { get; set; } = "";
}

public sealed class FilaCronogramaProtocoloDto
{
    public string Actividad { get; set; } = "";

    public string Responsable { get; set; } = "";

    public List<bool> Meses { get; set; } = [];
}

public sealed class FilaPresupuestoProtocoloDto
{
    public string Rubro { get; set; } = "";

    public List<string> Valores { get; set; } = [];
}

public sealed class CampoProtocolo
{
    public string Clave { get; init; } = "";

    public string Seccion { get; init; } = "";

    public string Etiqueta { get; init; } = "";

    public string Ayuda { get; init; } = "";
}

public static class CatalogoProtocolo
{
    public const string AyudaAnexos = "Incluye los enlaces a los documentos anexos. El archivo debe estar disponible para cualquiera con el enlace. Separa varios enlaces por línea.";

    public static readonly string[] Rubros =
    [
        "Recurso humano",
        "Servicios técnicos y asesorías",
        "Equipos y software",
        "Gastos operacionales",
        "Impresos y publicaciones",
        "Viajes",
        "Materiales e Insumos",
        "Comunicaciones y Transportes",
        "Otros"
    ];

    public static readonly string[] Fuentes =
    [
        "Convocatoria interna · Efectivo",
        "Convocatoria interna · Especie",
        "Programa o facultad · Efectivo",
        "Programa o facultad · Especie",
        "Institución 1 · Efectivo",
        "Institución 1 · Especie",
        "Institución 2 · Efectivo",
        "Institución 2 · Especie"
    ];

    public static readonly string[] EticaPoblacion =
    [
        "Mujeres en embarazo",
        "Menores de edad",
        "Estudiantes",
        "Personas sometidas a alguna forma de detención o prisión",
        "Militares",
        "Personas pertenecientes a minorías o colectivos",
        "Personas en situación de vulnerabilidad",
        "Animales, plantas, virus y microorganismos"
    ];

    public static readonly string[] EticaIntervencion =
    [
        "Uso de datos personales",
        "Microorganismos y organismos silvestres",
        "Personas con limitaciones en sus capacidades funcionales",
        "Medicamentos",
        "Dispositivos médicos",
        "Ejercicio físico",
        "Cambios en el estilo de vida"
    ];

    public static readonly string[] EticaInstrumentos =
    [
        "Instrumentos",
        "Utilización de agentes biológicos de riesgo",
        "Programas o estrategias temporales psicológicas o sociales",
        "Dispositivos generadores de radiaciones ionizantes",
        "Utilización de muestras biológicas humanas o información genética",
        "Grabaciones de audio",
        "Grabaciones en video",
        "Cuestionarios con preguntas sensibles",
        "Estrategias participativas o comunitarias"
    ];

    public static readonly (string Clave, string Etiqueta)[] ComentariosTablasFoscal =
    [
        ("foscal_conocimiento_general", "Generación de conocimiento"),
        ("foscal_tecnologico", "Desarrollos tecnológicos e innovación"),
        ("foscal_capacidad", "Fortalecimiento de la capacidad científica institucional")
    ];

    private static readonly CampoProtocolo[] Uis =
    [
        Campo("uis_titulo", "Formato UIS", "Título", "Máximo veinte palabras."),
        Campo("uis_resumen", "Formato UIS", "Resumen de la propuesta", "Máximo una página. Información básica de la propuesta."),
        Campo("uis_marco", "Formato UIS", "Marco Teórico y estado del arte", "Máximo tres páginas. Teorías, estado del conocimiento y originalidad de la propuesta."),
        Campo("uis_problema", "Formato UIS", "Planteamiento del Problema de investigación", "Máximo una página. Pregunta de investigación, importancia y pertinencia."),
        Campo("uis_objetivos", "Formato UIS", "Objetivos", "Máximo una página. Un objetivo general y los específicos, coherentes con la metodología."),
        Campo("uis_metodologia", "Formato UIS", "Metodología", "Cómo se alcanzarán los objetivos, el enfoque y el análisis. Coherente con presupuesto y cronograma."),
        Campo("uis_etica", "Formato UIS", "Aspectos Éticos", "Riesgos, garantías y principios éticos. Si no aplica, declararlo y justificarlo."),
        Campo("uis_cronograma", "Formato UIS", "Cronograma", "Actividades mes a mes, coherentes con objetivos y metodología."),
        Campo("uis_resultados", "Formato UIS", "Resultados Esperados", "Resultados según objetivos y metodología, en las categorías de Minciencias."),
        Campo("uis_impacto", "Formato UIS", "Impactos esperados a partir del uso de los resultados", "Incidencia del uso de los resultados y riesgos favorables o desfavorables."),
        Campo("uis_bibliografia", "Formato UIS", "Bibliografía", "Solo la referida en el texto, con el sistema de citación que utilice."),
        Campo("uis_presupuesto", "Formato UIS", "Presupuesto", "Información de la pestaña Presupuesto Global. Mínimo 5% para apropiación social del conocimiento."),
        Campo("uis_anexos", "Formato UIS", "Anexos", AyudaAnexos)
    ];

    private static readonly CampoProtocolo[] Foscal =
    [
        Campo("foscal_titulo_protocolo", "información general del proyecto", "Título del protocolo", ""),
        Campo("foscal_area_conocimiento", "información general del proyecto", "Área del Conocimiento", ""),
        Campo("foscal_tematica_proyecto", "información general del proyecto", "Temática del proyecto", ""),
        Campo("foscal_duracion", "información general del proyecto", "Duración", ""),
        Campo("foscal_investigadores_principales", "información general del proyecto", "Investigador(es) principal(es)", ""),
        Campo("foscal_equipo_investigador", "información general del proyecto", "Equipo investigador", ""),
        Campo("foscal_grupo_investigacion", "información general del proyecto", "Grupo de investigación (si aplica)", ""),
        Campo("foscal_investigador_responsable", "información general del proyecto", "Investigador responsable", ""),
        Campo("foscal_introduccion", "Resumen", "Introducción", ""),
        Campo("foscal_objetivo", "Resumen", "Objetivo", ""),
        Campo("foscal_metodologia", "Resumen", "Metodología", ""),
        Campo("foscal_resultados", "Resumen", "Resultados esperados", ""),
        Campo("foscal_palabras_clave", "Resumen", "Palabras clave", ""),
        Campo("foscal_planteamiento_problema", "Descripción del proyecto", "Planteamiento del problema y justificación en términos de necesidades y pertinencia", ""),
        Campo("foscal_pregunta_investigacion", "Descripción del proyecto", "Pregunta de investigación", ""),
        Campo("foscal_marco_teorico", "Descripción del proyecto", "Marco teórico", ""),
        Campo("foscal_estado_arte", "Descripción del proyecto", "Estado del arte", ""),
        Campo("foscal_objetivos_especificos", "Descripción del proyecto", "Objetivos específicos", ""),
        Campo("foscal_diseno", "Metodología", "Diseño", ""),
        Campo("foscal_universo_poblacion", "Metodología", "Universo y población de referencia", ""),
        Campo("foscal_poblacion_muestra_muestreo", "Metodología", "Población elegible, muestra y muestreo", ""),
        Campo("foscal_criterios_inclusion", "Criterios de elegibilidad", "Criterios de inclusión", ""),
        Campo("foscal_criterios_exclusion", "Criterios de elegibilidad", "Criterios de exclusión", ""),
        Campo("foscal_descripcion_procedimiento", "Criterios de elegibilidad", "Descripción del procedimiento", ""),
        Campo("foscal_depuracion_analisis", "Criterios de elegibilidad", "Depuración de datos y plan de análisis", ""),
        Campo("foscal_impacto_ambiental", "Disposiciones vigentes", "Impacto ambiental", ""),
        Campo("foscal_consideraciones_eticas", "Disposiciones vigentes", "Consideraciones éticas", ""),
        Campo("foscal_presupuesto", "Presupuesto", "Presupuesto", ""),
        Campo("foscal_referencias_bibliograficas", "Referencias", "Referencias bibliográficas", ""),
        Campo("foscal_anexos", "Anexos", "Anexos", AyudaAnexos)
    ];

    private static readonly CampoProtocolo[] Udes =
    [
        Campo("udes_descripcion_problema", "Planteamiento del problema", "Descripción del Problema", "Máximo 3000 palabras."),
        Campo("udes_pregunta_investigacion", "Planteamiento del problema", "Pregunta de Investigación", "Máximo 3000 palabras."),
        Campo("udes_hipotesis", "Planteamiento del problema", "Hipótesis (si aplica)", "Máximo 3000 palabras."),
        Campo("udes_pertinencia_viabilidad_factibilidad", "Justificación", "Pertinencia, viabilidad y factibilidad en la realización del proyecto.", "Máximo 3000 palabras."),
        Campo("udes_objetivo_general", "Objetivos", "General", ""),
        Campo("udes_objetivos_especificos", "Objetivos", "Específicos", ""),
        Campo("udes_marco_teorico", "Marco referencial", "Marco Teórico", "Antecedentes, conceptos y estado del arte."),
        Campo("udes_marco_contextual", "Marco referencial", "Marco contextual", "Escenario donde se va a ejecutar el proyecto (si aplica)."),
        Campo("udes_marco_legal", "Marco referencial", "Marco legal", "Aspectos legales que soportan la investigación (si aplica)."),
        Campo("udes_tipo_estudio", "Metodología", "Tipo de estudio", ""),
        Campo("udes_poblacion_muestra", "Metodología", "Población y Muestra", "Cálculo del tamaño de muestra y tipo de muestreo."),
        Campo("udes_criterios_inclusion_exclusion", "Metodología", "Criterios de inclusión y exclusión", ""),
        Campo("udes_procedimientos", "Metodología", "Procedimientos", ""),
        Campo("udes_analisis_estadistico", "Metodología", "Análisis estadístico", ""),
        Campo("udes_consideraciones_eticas", "Metodología", "Consideraciones Éticas", "Máximo 1000 palabras. Riesgo según la Resolución 8430 de 1993 y principios bioéticos."),
        Campo("udes_resultados_esperados", "Resultados", "Resultados esperados", "Máximo 500 palabras. Relacionados con los objetivos."),
        Campo("udes_productos_esperados", "Resultados", "Productos esperados", "Máximo 300 palabras."),
        Campo("udes_presupuesto", "Presupuesto", "Presupuesto", "Formato de presupuesto de la UDES. Coherente con objetivos, actividades y cronograma."),
        Campo("udes_bibliografia", "Bibliografía", "Bibliografía", "Mínimo 20 referentes; al menos la mitad de los últimos tres años."),
        Campo("udes_anexos", "Anexos", "Anexos", AyudaAnexos)
    ];

    public static IReadOnlyList<CampoProtocolo> Campos(string formato) => formato switch
    {
        "UIS" => Uis,
        "FOSCAL" => Foscal,
        "UDES" => Udes,
        _ => []
    };

    public static string NombreFormato(string formato) => formato switch
    {
        "UIS" => "Universidad Industrial de Santander (UIS)",
        "FOSCAL" => "Foscal",
        "UDES" => "Universidad de Santander (UDES)",
        _ => formato
    };

    public static bool ClaveTexto(string formato, string clave) =>
        Campos(formato).Any(item => item.Clave == clave);

    public static bool ClaveComentario(string formato, string clave) =>
        ClaveTexto(formato, clave)
        || (formato == "FOSCAL" && ComentariosTablasFoscal.Any(item => item.Clave == clave));

    private static CampoProtocolo Campo(string clave, string seccion, string etiqueta, string ayuda) => new()
    {
        Clave = clave,
        Seccion = seccion,
        Etiqueta = etiqueta,
        Ayuda = ayuda
    };
}

public static class PresupuestoProtocolo
{
    public static decimal Leer(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return 0;
        }

        var texto = valor.Trim().Replace(" ", "");
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariante))
        {
            return invariante;
        }

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("es-CO"), out var local)
            ? local
            : 0;
    }

    public static decimal TotalFila(FilaPresupuestoProtocoloDto fila) =>
        fila.Valores.Sum(Leer);

    public static decimal[] TotalesColumna(IReadOnlyList<FilaPresupuestoProtocoloDto> filas)
    {
        var totales = new decimal[CatalogoProtocolo.Fuentes.Length];
        foreach (var fila in filas)
        {
            for (var columna = 0; columna < totales.Length && columna < fila.Valores.Count; columna++)
            {
                totales[columna] += Leer(fila.Valores[columna]);
            }
        }

        return totales;
    }

    public static decimal TotalGeneral(IReadOnlyList<FilaPresupuestoProtocoloDto> filas) =>
        TotalesColumna(filas).Sum();
}
