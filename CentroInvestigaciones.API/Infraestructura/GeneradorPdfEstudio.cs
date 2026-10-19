using CentroInvestigaciones.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfEstudio
{
    private const string Azul = "#3d6fa8";
    private const string AzulClaro = "#e7f0f8";
    private const string Borde = "#d5e1ee";
    private const string Texto = "#1a1a2e";

    private readonly byte[]? _logo;

    public GeneradorPdfEstudio(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logo-foscal-marca.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public IReadOnlyList<(string Clave, string Nombre, byte[] Pdf)> GenerarAcademicos(Estudio estudio, IReadOnlyDictionary<string, byte[]> firmas)
    {
        var fecha = DateTime.Now.ToString("dd/MM/yyyy");
        var formatos = estudio.Formatos ?? new FormatosAcademicos();
        return
        [
            (ClavesArchivoEstudio.PdfCartaSolicitud, "Carta solicitud CIF.pdf", Carta(estudio, formatos.CartaSolicitud, fecha, Firma(firmas, ClavesArchivoEstudio.FirmaCartaSolicitud))),
            (ClavesArchivoEstudio.PdfCartaIntencion, "Carta de intención.pdf", Intencion(estudio, formatos.CartaIntencion, fecha, Firma(firmas, ClavesArchivoEstudio.FirmaCartaIntencion))),
            (ClavesArchivoEstudio.PdfAcuerdo, "Acuerdo de confidencialidad.pdf", Acuerdo(formatos.Acuerdo, fecha, Firma(firmas, ClavesArchivoEstudio.FirmaAcuerdo))),
            (ClavesArchivoEstudio.PdfRegistro, "Formulario de registro.pdf", Registro(formatos.Registro, Firma(firmas, ClavesArchivoEstudio.FirmaRegistro))),
            (ClavesArchivoEstudio.PdfHojaVida, "Hoja de vida del investigador.pdf", Hoja(formatos.HojaVida, fecha, Firma(firmas, ClavesArchivoEstudio.FirmaHojaVida))),
            (ClavesArchivoEstudio.PdfResumen, "Resumen analítico.pdf", Resumen(formatos.Resumen))
        ];
    }

    private byte[] Carta(Estudio estudio, CartaSolicitudEstudio carta, string fecha, byte[]? firma)
    {
        return Pagina("Carta de solicitud CIF", fecha, columna =>
        {
            columna.Item().Text("Comité de Investigación").SemiBold();
            columna.Item().Text("Fundación Oftalmológica de Santander - FOSCAL");
            columna.Item().PaddingTop(10).Text(texto =>
            {
                texto.Span("Asunto: ").SemiBold();
                texto.Span($"Carta de solicitud de evaluación y aprobación de {Valor(carta.Protocolo, estudio.Titulo)}.");
            });
            columna.Item().PaddingTop(8).Text("Respetados señores:");
            columna.Item().PaddingTop(6).Text("Solicito la evaluación y aprobación de los documentos adjuntos relacionados con el protocolo, proyecto o programa:");
            columna.Item().PaddingTop(4).Text($"• Protocolo (versión: {Valor(carta.VersionProtocolo)}, fecha: {Valor(carta.FechaProtocolo)}, idioma: {Valor(carta.IdiomaProtocolo)}).");
            columna.Item().Text($"• Consentimiento, si aplica (versión: {Valor(carta.VersionConsentimiento)}, fecha: {Valor(carta.FechaConsentimiento)}, idioma: {Valor(carta.IdiomaConsentimiento)}).");
            columna.Item().Text($"• Instrumentos de recolección (versión: {Valor(carta.VersionInstrumentos)}, fecha: {Valor(carta.FechaInstrumentos)}).");
            columna.Item().PaddingTop(8).Text("Quedo atento a cualquier inquietud adicional sobre la documentación o la investigación.");
            FirmaBloque(columna, firma, Valor(carta.Nombre, estudio.InvestigadorPrincipal), "Investigador principal");
        });
    }

    private byte[] Intencion(Estudio estudio, CartaIntencionEstudio carta, string fecha, byte[]? firma)
    {
        return Pagina("Carta de intención", fecha, columna =>
        {
            columna.Item().Text("Señores");
            columna.Item().Text("División de Investigación").SemiBold();
            columna.Item().Text("Subdirección de Investigación, Innovación y Educación");
            columna.Item().Text("Fundación Oftalmológica de Santander - FOSCAL");
            columna.Item().PaddingTop(8).Text(texto =>
            {
                texto.Span("Título del protocolo: ").SemiBold();
                texto.Span(Valor(carta.TituloProtocolo, estudio.Titulo));
            });
            columna.Item().PaddingTop(8).Text("Solicito autorización para iniciar las actividades del protocolo una vez sea aprobado por el Comité de Investigación y, cuando aplique, por el Comité de Ética en Investigación de la Clínica FOSCAL.");
            columna.Item().PaddingTop(6).Text("Como investigador principal me responsabilizo de respetar las normas éticas de investigación en seres humanos, el Código de Nüremberg, la Declaración de Helsinki, las guías de buenas prácticas clínicas y la normatividad vigente. Certifico que no tengo conflicto de interés con los resultados de la investigación.");
            columna.Item().PaddingTop(6).Text("No iniciaré el protocolo ni el enrolamiento hasta contar con las autorizaciones correspondientes. Presentaré los informes y la documentación que los comités requieran.");
            FirmaBloque(columna, firma, Valor(carta.Nombre, estudio.InvestigadorPrincipal), "Investigador principal");
        });
    }

    private byte[] Acuerdo(AcuerdoConfidencialidadEstudio acuerdo, string fecha, byte[]? firma)
    {
        return Pagina("Acuerdo de confidencialidad", fecha, columna =>
        {
            columna.Item().Text("Acuerdo de confidencialidad para investigadores y coinvestigadores").SemiBold();
            columna.Item().PaddingTop(8).Text($"Yo {Valor(acuerdo.Nombre)}, en mi rol como {Valor(acuerdo.Rol)}, identificado con cédula No. {Valor(acuerdo.Cedula)} de {Valor(acuerdo.Expedicion)}, adscrito a la División de Investigación de la Fundación Oftalmológica de Santander (FOSCAL), en relación con el protocolo {Valor(acuerdo.Protocolo)}, me comprometo a custodiar la confidencialidad y a no hacer un uso inadecuado de documentos, registros, reportes, archivos y demás información del estudio.");
            columna.Item().PaddingTop(6).Text("Mantendré la información en reserva, instruiré al personal que la reciba, la divulgaré solo a personas autorizadas y no la usaré para un fin distinto al de este acuerdo, salvo autorización escrita de FOSCAL.");
            columna.Item().PaddingTop(6).Text("El incumplimiento de la Ley 1581 de 2012 puede dar lugar a las sanciones que correspondan.");
            FirmaBloque(columna, firma, Valor(acuerdo.NombreFirma, acuerdo.Nombre), "Investigador(a)");
        });
    }

    private byte[] Registro(FormularioRegistroEstudio registro, byte[]? firma)
    {
        return Pagina("Formulario de registro para investigadores", null, columna =>
        {
            Seccion(columna, "Información personal");
            Fila(columna, "Nombres", registro.Nombres);
            Fila(columna, "Apellidos", registro.Apellidos);
            Fila(columna, "Títulos académicos", registro.Titulos);
            Fila(columna, "Correo", registro.Correo);
            Fila(columna, "Institución", registro.Institucion);
            Fila(columna, "Dirección", registro.Direccion);
            Fila(columna, "Teléfono", registro.Telefono);
            Fila(columna, "Celular", registro.Celular);
            Seccion(columna, "Formación académica");
            columna.Item().Text(Valor(registro.Formacion));
            Seccion(columna, "Filiación institucional");
            Fila(columna, "Servicio", registro.Servicio);
            Seccion(columna, "Experiencia en investigación");
            columna.Item().Text(Valor(registro.Experiencia));
            Seccion(columna, "Publicaciones científicas");
            columna.Item().Text(Valor(registro.Publicaciones));
            FirmaBloque(columna, firma, $"{registro.Nombres} {registro.Apellidos}".Trim(), null);
        });
    }

    private byte[] Hoja(HojaVidaEstudio hoja, string fecha, byte[]? firma)
    {
        return Pagina("Hoja de vida", null, columna =>
        {
            Seccion(columna, "Información personal");
            Fila(columna, "Nombres y apellidos", hoja.Nombre);
            Fila(columna, "Títulos académicos", hoja.Titulos);
            Fila(columna, "Dirección institucional", hoja.Direccion);
            Fila(columna, "Teléfono", hoja.Telefono);
            Fila(columna, "Fax", hoja.Fax);
            Seccion(columna, "Información académica");
            columna.Item().Text(Valor(hoja.Academica));
            Seccion(columna, "Experiencia laboral");
            columna.Item().Text(Valor(hoja.Laboral));
            Seccion(columna, "Experiencia en investigación");
            columna.Item().Text(Valor(hoja.Investigacion));
            Seccion(columna, "Entrenamiento ICH / GCP");
            columna.Item().Text(Valor(hoja.Gcp));
            FirmaBloque(columna, firma, hoja.Nombre, fecha);
        });
    }

    private byte[] Resumen(ResumenAnaliticoEstudio resumen)
    {
        return Pagina("Resumen analítico — GID-INV-FO-02", null, columna =>
        {
            Seccion(columna, "Identificación del proyecto");
            Fila(columna, "Título", resumen.Titulo);
            Fila(columna, "Línea de investigación", resumen.Linea);
            Fila(columna, "Grupo de investigación", resumen.Grupo);
            Fila(columna, "Área, servicio o programa", resumen.Area);
            Fila(columna, "Instituciones", string.Join(" · ", new[] { resumen.Institucion1, resumen.Institucion2, resumen.Institucion3 }.Where(item => !string.IsNullOrWhiteSpace(item))));
            Fila(columna, "Año de residencia", resumen.AnioResidencia);
            Seccion(columna, "Investigadores");
            foreach (var persona in resumen.Investigadores.Where(item => !string.IsNullOrWhiteSpace(item.Nombre) || !string.IsNullOrWhiteSpace(item.Rol)))
            {
                Fila(columna, Valor(persona.Nombre), persona.Rol);
            }

            Seccion(columna, "Problema de investigación");
            columna.Item().Text(Valor(resumen.Problema));
            Seccion(columna, "Justificación");
            columna.Item().Text(Valor(resumen.Justificacion));
            Seccion(columna, "Objetivos");
            Fila(columna, "General", resumen.ObjetivoGeneral);
            Fila(columna, "Específicos", string.Join(" ", new[] { resumen.Objetivo1, resumen.Objetivo2, resumen.Objetivo3 }.Where(item => !string.IsNullOrWhiteSpace(item))));
            Seccion(columna, "Estado del arte");
            columna.Item().Text(Valor(resumen.EstadoArte));
            Seccion(columna, "Diseño metodológico");
            Fila(columna, "Tipo de estudio", resumen.TipoEstudio);
            Fila(columna, "Población", resumen.Poblacion);
            Fila(columna, "Muestra", resumen.Muestra);
            Fila(columna, "Inclusión", resumen.Inclusion);
            Fila(columna, "Exclusión", resumen.Exclusion);
            Fila(columna, "Recolección", resumen.Recoleccion);
            Fila(columna, "Análisis", resumen.Analisis);
            Seccion(columna, "Divulgación");
            columna.Item().Text(Valor(resumen.Divulgacion));
            Seccion(columna, "Presupuesto estimado");
            foreach (var partida in resumen.Presupuesto.Where(item => TieneDato(item)))
            {
                Fila(columna, partida.Item, $"Horas {Valor(partida.Horas)} · Valor {Valor(partida.Valor)} · {Valor(partida.Responsable)} · {Valor(partida.Observaciones)}");
            }

            Seccion(columna, "Cronograma");
            Fila(columna, "Fecha de inicio", resumen.FechaInicio);
            foreach (var actividad in resumen.Cronograma.Where(item => !string.IsNullOrWhiteSpace(item.Actividad)))
            {
                Fila(columna, actividad.Actividad, $"{Valor(actividad.MesInicial)} – {Valor(actividad.MesFinal)}");
            }

            Seccion(columna, "Aspectos éticos");
            Fila(columna, "Riesgo", resumen.Riesgo);
            Fila(columna, "Confidencialidad", resumen.Confidencialidad);
            Fila(columna, "Seguridad de los datos", resumen.Seguridad);
            Fila(columna, "Consentimiento informado", resumen.Consentimiento);
        });
    }

    private byte[] Pagina(string titulo, string? fecha, Action<ColumnDescriptor> cuerpo)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(36);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(Texto));
                pagina.Content().Column(columna =>
                {
                    columna.Item().Border(0.6f).BorderColor(Borde).Padding(8).Row(fila =>
                    {
                        if (_logo is { Length: > 0 })
                        {
                            fila.ConstantItem(46).Height(52).Image(_logo).FitArea();
                        }

                        fila.RelativeItem().AlignMiddle().AlignRight().Column(texto =>
                        {
                            texto.Item().AlignRight().Text(titulo).FontSize(12).SemiBold().FontColor(Azul);
                            texto.Item().AlignRight().Text("Fundación Oftalmológica de Santander").FontSize(8).FontColor(Azul);
                            if (!string.IsNullOrWhiteSpace(fecha))
                            {
                                texto.Item().AlignRight().Text($"Floridablanca, {fecha}").FontSize(8);
                            }
                        });
                    });
                    columna.Item().PaddingTop(12).Column(cuerpo);
                });
            });
        }).GeneratePdf();
    }

    private static void Seccion(ColumnDescriptor columna, string titulo)
    {
        columna.Item().PaddingTop(8).Background(Azul).Padding(4).PaddingHorizontal(6)
            .Text(titulo.ToUpperInvariant()).FontSize(8).SemiBold().FontColor(Colors.White);
    }

    private static void Fila(ColumnDescriptor columna, string etiqueta, string? valor)
    {
        columna.Item().BorderBottom(0.4f).BorderColor(Borde).PaddingVertical(3).Row(fila =>
        {
            fila.ConstantItem(150).Text(etiqueta).SemiBold().FontSize(9).FontColor(Azul);
            fila.RelativeItem().Text(Valor(valor)).FontSize(9);
        });
    }

    private static void FirmaBloque(ColumnDescriptor columna, byte[]? firma, string nombre, string? cargo)
    {
        columna.Item().PaddingTop(16).Text("Atentamente,");
        if (firma is { Length: > 0 } && ImagenValida.Es(firma))
        {
            columna.Item().PaddingTop(6).Height(56).Width(168).Image(firma).FitArea();
        }
        else
        {
            columna.Item().PaddingTop(18).Text(" ");
        }

        columna.Item().PaddingTop(4).Text(Valor(nombre)).SemiBold();
        if (!string.IsNullOrWhiteSpace(cargo))
        {
            columna.Item().Text(cargo).FontSize(9);
        }
    }

    private static byte[]? Firma(IReadOnlyDictionary<string, byte[]> firmas, string clave) =>
        firmas.TryGetValue(clave, out var bytes) ? bytes : null;

    private static string Valor(string? texto, string? respaldo = null)
    {
        if (!string.IsNullOrWhiteSpace(texto))
        {
            return texto.Trim();
        }

        return string.IsNullOrWhiteSpace(respaldo) ? "_____________" : respaldo.Trim();
    }

    private static bool TieneDato(PartidaPresupuestoEstudio partida) =>
        !string.IsNullOrWhiteSpace(partida.Horas)
        || !string.IsNullOrWhiteSpace(partida.Valor)
        || !string.IsNullOrWhiteSpace(partida.Responsable)
        || !string.IsNullOrWhiteSpace(partida.Observaciones);
}
