using CentroInvestigaciones.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfFarmacia
{
    private const string Azul = "#1E4A8A";
    private const string Texto = "#1a1a2e";

    private readonly byte[]? _logo;

    public GeneradorPdfFarmacia(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logo-foscal-marca.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public byte[] Generar(FormatoFarmacia formato)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A3.Landscape());
                pagina.Margin(16);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Texto));

                pagina.Header().Element(cabecera => Cabecera(cabecera, formato));
                pagina.Content().PaddingTop(8).Column(columna =>
                {
                    columna.Item().Table(tabla => Movimientos(tabla, formato.Filas));
                    columna.Item().PaddingTop(8).Element(pie => Cierre(pie, formato));
                });
            });
        }).GeneratePdf();
    }

    private void Cabecera(IContainer contenedor, FormatoFarmacia formato)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Row(fila =>
            {
                if (_logo is { Length: > 0 })
                {
                    fila.ConstantItem(78).Height(36).Image(_logo).FitArea();
                }

                fila.RelativeItem().AlignCenter().AlignMiddle().Text("CONTABILIDAD DE MEDICAMENTO EN INVESTIGACIÓN")
                    .FontSize(14).Bold().FontColor(Azul);
            });

            columna.Item().PaddingTop(8).Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3);
                    columnas.RelativeColumn(2);
                    columnas.RelativeColumn(2);
                    columnas.ConstantColumn(90);
                });

                Dato(tabla, "Número del estudio", formato.NumeroEstudio);
                Dato(tabla, "Patrocinador", formato.Patrocinador);
                Dato(tabla, "Coordinador", formato.Coordinador);
                Dato(tabla, "N° centro", formato.NumeroCentro);
                tabla.Cell().ColumnSpan(4).Element(Caja).Text(texto =>
                {
                    texto.Span("Título del estudio: ").Bold();
                    texto.Span(formato.Titulo);
                });
                tabla.Cell().ColumnSpan(2).Element(Caja).Text(texto =>
                {
                    texto.Span("Investigador principal: ").Bold();
                    texto.Span(formato.InvestigadorPrincipal);
                });
                tabla.Cell().ColumnSpan(2).Element(Caja).Text(texto =>
                {
                    texto.Span("Condiciones de almacenamiento: ").Bold();
                    texto.Span(formato.CondicionesAlmacenamiento);
                });
                tabla.Cell().ColumnSpan(3).Element(Caja).Text(texto =>
                {
                    texto.Span("Descripción del medicamento: ").Bold();
                    texto.Span(formato.DescripcionMedicamento);
                });
                tabla.Cell().Element(Caja).Text(texto =>
                {
                    texto.Span("Dosis: ").Bold();
                    texto.Span(formato.Dosis);
                });
            });
        });
    }

    private static void Movimientos(TableDescriptor tabla, IReadOnlyList<FilaFarmacia> filas)
    {
        tabla.ColumnsDefinition(columnas =>
        {
            for (var i = 0; i < 19; i++)
            {
                columnas.RelativeColumn();
            }
        });

        tabla.Header(encabezado =>
        {
            Sola(encabezado, "N° envío");
            Grupo(encabezado, 5, "RECEPCIÓN");
            Grupo(encabezado, 4, "ENTREGA");
            Grupo(encabezado, 5, "RETORNO");
            Sola(encabezado, "Control monitoría");
            Grupo(encabezado, 3, "DISPOSICIÓN FINAL");

            foreach (var titulo in Columnas)
            {
                encabezado.Cell().Element(CajaAzul).AlignCenter().Text(titulo).FontSize(7).FontColor("#ffffff").Bold();
            }
        });

        if (filas.Count == 0)
        {
            tabla.Cell().ColumnSpan(19).Element(Caja).Text("Sin medicamentos.");
            return;
        }

        foreach (var fila in filas)
        {
            foreach (var valor in Valores(fila))
            {
                tabla.Cell().Element(Caja).Text(valor).FontSize(8);
            }
        }
    }

    private static void Dato(TableDescriptor tabla, string etiqueta, string valor)
    {
        tabla.Cell().Element(Caja).Text(texto =>
        {
            texto.Span(etiqueta + ": ").Bold();
            texto.Span(valor);
        });
    }

    private static void Cierre(IContainer contenedor, FormatoFarmacia formato)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(2);
                columnas.RelativeColumn();
                columnas.RelativeColumn(1.4f);
                columnas.RelativeColumn();
            });

            tabla.Cell().Element(Caja).MinHeight(42).Text(texto =>
            {
                texto.Span("Observaciones: ").Bold();
                texto.Span(formato.Observaciones);
            });
            tabla.Cell().Element(Caja).Text(texto =>
            {
                texto.Span("Responsable farmacia: ").Bold();
                texto.Span(formato.ResponsableFarmacia);
            });
            tabla.Cell().Element(Caja).Text(texto =>
            {
                texto.Span("Firma y fecha de verificación o cierre: ").Bold();
                texto.Span(formato.FirmaCierre);
            });
            tabla.Cell().Element(Caja).Text(texto =>
            {
                texto.Span("Cantidad final: ").Bold();
                texto.Span(formato.CantidadFinal);
            });
        });
    }

    private static void Grupo(TableCellDescriptor celdas, uint span, string titulo)
    {
        celdas.Cell().ColumnSpan(span).Element(CajaAzul).AlignCenter().Text(titulo).FontSize(7).FontColor("#ffffff").Bold();
    }

    private static void Sola(TableCellDescriptor celdas, string titulo)
    {
        celdas.Cell().RowSpan(2).Element(CajaAzul).AlignCenter().AlignMiddle().Text(titulo).FontSize(6).FontColor("#ffffff").Bold();
    }

    private static IContainer Caja(IContainer contenedor) =>
        contenedor.Border(0.4f).BorderColor(Texto).Padding(2);

    private static IContainer CajaAzul(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor("#ffffff").Background(Azul).Padding(2);

    private static string Fecha(string valor) =>
        DateOnly.TryParse(valor, out var fecha) ? fecha.ToString("dd-MM-yy") : valor;

    private static readonly string[] Columnas =
    [
        "Fecha recibido\ndd-mm-aa", "Lote/Ref", "Fecha vencimiento\ndd-mm-aa", "Código\nmedicamento", "Conforme",
        "Sujeto código/\niniciales", "Fecha\ndd-mm-aa", "Entrega", "Recibe",
        "Fecha\ndd-mm-aa", "Empaque", "Und.\nmedicamento", "Entrega", "Recibe",
        "Fecha\ndd-mm-aa", "Retorno patrocinador\nN° acta", "Destrucción\nN° acta"
    ];

    private static IEnumerable<string> Valores(FilaFarmacia fila)
    {
        yield return fila.NumeroEnvio;
        yield return Fecha(fila.FechaRecibido);
        yield return fila.Lote;
        yield return Fecha(fila.FechaVencimiento);
        yield return fila.CodigoMedicamento;
        yield return fila.Conforme;
        yield return fila.SujetoCodigo;
        yield return Fecha(fila.FechaEntrega);
        yield return fila.Entrega;
        yield return fila.Recibe;
        yield return Fecha(fila.FechaRetorno);
        yield return fila.Empaque;
        yield return fila.Unidades;
        yield return fila.EntregaRetorno;
        yield return fila.RecibeRetorno;
        yield return fila.ControlMonitoria;
        yield return Fecha(fila.FechaDisposicion);
        yield return fila.RetornoPatrocinador;
        yield return fila.DestruccionActa;
    }
}
