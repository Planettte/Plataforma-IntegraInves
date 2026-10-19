using System.Globalization;
using CentroInvestigaciones.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfProtocolos
{
    private const string Tinta = "#1a1a2e";
    private const string Azul = "#1E4A8A";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    private readonly byte[]? _logo;

    public GeneradorPdfProtocolos(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logo-foscal-marca.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public byte[] Generar(ProtocoloDto protocolo)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(18);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Tinta));
                pagina.Header().Element(caja => Encabezado(caja, protocolo));
                pagina.Content().PaddingTop(8).Column(columna => Cuerpo(columna, protocolo));
                pagina.Footer().AlignRight().Text(texto =>
                {
                    texto.Span("Formulario de protocolos · ").FontSize(8);
                    texto.CurrentPageNumber().FontSize(8);
                    texto.Span(" / ").FontSize(8);
                    texto.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private void Encabezado(IContainer caja, ProtocoloDto protocolo)
    {
        caja.Row(fila =>
        {
            if (_logo is not null)
            {
                fila.ConstantItem(92).Height(36).Image(_logo).FitArea();
            }

            fila.RelativeItem().PaddingLeft(8).Column(columna =>
            {
                columna.Item().Text(CatalogoProtocolo.NombreFormato(protocolo.Formato)).FontSize(14).Bold().FontColor(Azul);
                columna.Item().Text(protocolo.Nombre).FontSize(11);
                columna.Item().Text($"{protocolo.Correo} · {protocolo.Estado}").FontSize(8).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void Cuerpo(ColumnDescriptor columna, ProtocoloDto protocolo)
    {
        string? seccion = null;
        foreach (var campo in CatalogoProtocolo.Campos(protocolo.Formato))
        {
            if (seccion != campo.Seccion)
            {
                seccion = campo.Seccion;
                if (protocolo.Formato == "FOSCAL" && campo.Seccion == "Presupuesto")
                {
                    columna.Item().Element(caja => Cronograma(caja, "Cronograma de actividades", protocolo.CronogramaFoscal, conResponsable: true));
                }

                if (protocolo.Formato == "UDES" && campo.Seccion == "Presupuesto")
                {
                    columna.Item().Element(caja => Cronograma(caja, "Cronograma de actividades", protocolo.CronogramaUdes, conResponsable: false));
                }

                columna.Item().PaddingTop(10).Text(campo.Seccion).FontSize(12).Bold().FontColor(Azul);
            }

            columna.Item().PaddingTop(6).Element(caja => Texto(caja, campo.Etiqueta, Valor(protocolo, campo.Clave), Comentario(protocolo, campo.Clave)));

            if (protocolo.Formato == "FOSCAL" && campo.Clave == "foscal_depuracion_analisis")
            {
                columna.Item().Element(caja => Conocimiento(caja, protocolo));
                columna.Item().Element(caja => Productos(caja, "Desarrollos tecnológicos e innovación", protocolo.Tecnologico, Comentario(protocolo, "foscal_tecnologico")));
                columna.Item().Element(caja => Productos(caja, "Fortalecimiento de la capacidad científica institucional", protocolo.Capacidad, Comentario(protocolo, "foscal_capacidad")));
            }

            if (protocolo.Formato == "UDES" && campo.Clave == "udes_consideraciones_eticas")
            {
                columna.Item().Element(caja => Etica(caja, protocolo));
            }

            if (protocolo.Formato == "UDES" && campo.Clave == "udes_presupuesto")
            {
                columna.Item().Element(caja => Presupuesto(caja, protocolo));
            }
        }
    }

    private static void Texto(IContainer caja, string etiqueta, string valor, string comentario)
    {
        caja.Column(columna =>
        {
            columna.Item().Text(etiqueta).SemiBold();
            columna.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor);
            if (!string.IsNullOrWhiteSpace(comentario))
            {
                columna.Item().PaddingTop(2).Text("Comentario: " + comentario).Italic().FontColor(Colors.Brown.Darken2);
            }
        });
    }

    private static void Conocimiento(IContainer caja, ProtocoloDto protocolo)
    {
        caja.PaddingTop(8).Column(columna =>
        {
            columna.Item().Text("Generación de conocimiento").SemiBold();
            columna.Item().PaddingTop(3).Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                });
                Encabezados(tabla, "Resultado / producto esperado", "Indicador", "Beneficiario");
                foreach (var fila in protocolo.Conocimiento)
                {
                    Celda(tabla, fila.Resultado);
                    Celda(tabla, fila.Indicador);
                    Celda(tabla, fila.Beneficiario);
                }
            });
            Nota(columna, Comentario(protocolo, "foscal_conocimiento_general"));
        });
    }

    private static void Productos(IContainer caja, string titulo, IReadOnlyList<FilaProductoProtocoloDto> filas, string comentario)
    {
        caja.PaddingTop(8).Column(columna =>
        {
            columna.Item().Text(titulo).SemiBold();
            columna.Item().PaddingTop(3).Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                });
                Encabezados(tabla, "Objetivo de la propuesta", "Resultado / producto esperado", "Indicador", "Beneficiario");
                foreach (var fila in filas)
                {
                    Celda(tabla, fila.Objetivo);
                    Celda(tabla, fila.Resultado);
                    Celda(tabla, fila.Indicador);
                    Celda(tabla, fila.Beneficiario);
                }
            });
            Nota(columna, comentario);
        });
    }

    private static void Cronograma(IContainer caja, string titulo, IReadOnlyList<FilaCronogramaProtocoloDto> filas, bool conResponsable)
    {
        caja.PaddingTop(8).Column(columna =>
        {
            columna.Item().Text(titulo).FontSize(12).Bold().FontColor(Azul);
            columna.Item().PaddingTop(3).Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3);
                    for (var mes = 0; mes < 12; mes++)
                    {
                        columnas.ConstantColumn(16);
                    }

                    if (conResponsable)
                    {
                        columnas.RelativeColumn(2);
                    }
                });
                tabla.Header(encabezado =>
                {
                    encabezado.Cell().Element(CeldaEncabezado).Text("Actividad");
                    for (var mes = 1; mes <= 12; mes++)
                    {
                        encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text(mes.ToString(Cultura));
                    }

                    if (conResponsable)
                    {
                        encabezado.Cell().Element(CeldaEncabezado).Text("Responsable");
                    }
                });
                foreach (var fila in filas)
                {
                    Celda(tabla, fila.Actividad);
                    for (var mes = 0; mes < 12; mes++)
                    {
                        Celda(tabla, fila.Meses.Count > mes && fila.Meses[mes] ? "●" : "");
                    }

                    if (conResponsable)
                    {
                        Celda(tabla, fila.Responsable);
                    }
                }
            });
        });
    }

    private static void Etica(IContainer caja, ProtocoloDto protocolo)
    {
        caja.PaddingTop(8).Column(columna =>
        {
            columna.Item().Text("El proyecto involucra").SemiBold();
            Marcas(columna, "Población", CatalogoProtocolo.EticaPoblacion, protocolo.EticaPoblacion);
            Marcas(columna, "Intervención", CatalogoProtocolo.EticaIntervencion, protocolo.EticaIntervencion);
            Marcas(columna, "Instrumentos de recolección", CatalogoProtocolo.EticaInstrumentos, protocolo.EticaInstrumentos);
        });
    }

    private static void Marcas(ColumnDescriptor columna, string titulo, IReadOnlyList<string> opciones, IReadOnlyList<bool> marcas)
    {
        var elegidas = opciones.Where((_, indice) => indice < marcas.Count && marcas[indice]).ToList();
        columna.Item().PaddingTop(4).Text(titulo).SemiBold();
        columna.Item().Text(elegidas.Count == 0 ? "—" : string.Join(", ", elegidas));
    }

    private static void Presupuesto(IContainer caja, ProtocoloDto protocolo)
    {
        var columnasTotales = PresupuestoProtocolo.TotalesColumna(protocolo.Presupuesto);
        caja.PaddingTop(8).Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(2);
                for (var indice = 0; indice < CatalogoProtocolo.Fuentes.Length; indice++)
                {
                    columnas.RelativeColumn();
                }

                columnas.RelativeColumn();
            });
            tabla.Header(encabezado =>
            {
                encabezado.Cell().Element(CeldaEncabezado).Text("Rubros");
                foreach (var fuente in CatalogoProtocolo.Fuentes)
                {
                    encabezado.Cell().Element(CeldaEncabezado).Text(fuente);
                }

                encabezado.Cell().Element(CeldaEncabezado).Text("Total");
            });
            foreach (var fila in protocolo.Presupuesto)
            {
                Celda(tabla, fila.Rubro);
                foreach (var valor in fila.Valores)
                {
                    Celda(tabla, valor);
                }

                Celda(tabla, PresupuestoProtocolo.TotalFila(fila).ToString("N0", Cultura));
            }

            Celda(tabla, "Total");
            foreach (var total in columnasTotales)
            {
                Celda(tabla, total.ToString("N0", Cultura));
            }

            Celda(tabla, PresupuestoProtocolo.TotalGeneral(protocolo.Presupuesto).ToString("N0", Cultura));
        });
    }

    private static void Encabezados(TableDescriptor tabla, params string[] titulos)
    {
        tabla.Header(encabezado =>
        {
            foreach (var titulo in titulos)
            {
                encabezado.Cell().Element(CeldaEncabezado).Text(titulo);
            }
        });
    }

    private static void Celda(TableDescriptor tabla, string texto) =>
        tabla.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(texto);

    private static IContainer CeldaEncabezado(IContainer caja) =>
        caja.Background(Azul).Padding(3).DefaultTextStyle(estilo => estilo.FontColor(Colors.White).FontSize(7).SemiBold());

    private static void Nota(ColumnDescriptor columna, string comentario)
    {
        if (!string.IsNullOrWhiteSpace(comentario))
        {
            columna.Item().PaddingTop(2).Text("Comentario: " + comentario).Italic().FontColor(Colors.Brown.Darken2);
        }
    }

    private static string Valor(ProtocoloDto protocolo, string clave) =>
        protocolo.Respuestas.GetValueOrDefault(clave) ?? "";

    private static string Comentario(ProtocoloDto protocolo, string clave) =>
        protocolo.Comentarios.GetValueOrDefault(clave) ?? "";
}
