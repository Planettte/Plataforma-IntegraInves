using CentroInvestigaciones.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfRetencion
{
    private const string Tinta = "#1a1a2e";
    private const string CodigoFormato = "Estudios Clínicos- 006- MF –V5";

    private readonly byte[]? _logo;

    public GeneradorPdfRetencion(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logo-foscal-marca.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public byte[] Generar(RetencionDocumentalDto datos)
    {
        var contenido = (datos.Contenido ?? []).Concat(["", "", "", ""]).Take(4).ToList();
        var direcciones = ConMinimo(datos.Direcciones ?? [], 4, () => new DireccionRetencionDto());
        var prestamos = ConMinimo(datos.Prestamos ?? [], 7, () => new PrestamoRetencionDto());

        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(18);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Tinta));

                pagina.Content().Column(columna =>
                {
                    columna.Item().Element(caja => Encabezado(caja));
                    columna.Item().Element(caja => Datos(caja, datos, contenido));
                    columna.Item().PaddingTop(8).Element(caja => Direcciones(caja, direcciones));
                    columna.Item().PaddingTop(8).Element(caja => Prestamos(caja, prestamos));
                });

                pagina.Footer().AlignRight().Text(CodigoFormato).FontSize(8);
            });
        }).GeneratePdf();
    }

    private void Encabezado(IContainer contenedor)
    {
        contenedor.Border(1.2f).BorderColor(Tinta).Row(fila =>
        {
            if (_logo is { Length: > 0 })
            {
                fila.ConstantItem(120).Padding(4).Height(42).Image(_logo).FitArea();
            }

            fila.RelativeItem().AlignCenter().AlignMiddle()
                .Text("TABLA DE RETENCIÓN DOCUMENTAL").FontSize(14).Bold();
        });
    }

    private static void Datos(IContainer contenedor, RetencionDocumentalDto datos, IReadOnlyList<string> contenido)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(120);
                columnas.RelativeColumn();
                columnas.RelativeColumn();
                columnas.RelativeColumn();
                columnas.RelativeColumn();
            });

            tabla.Cell().Element(Etiqueta).Text("NÚMERO DEL ESTUDIO");
            tabla.Cell().ColumnSpan(4).Element(Valor).Text(datos.NumeroEstudio);
            tabla.Cell().Element(Etiqueta).Text("PATROCINADOR");
            tabla.Cell().ColumnSpan(4).Element(Valor).Text(datos.Patrocinador);
            tabla.Cell().Element(Etiqueta).Text("CONTENIDO");
            foreach (var caja in contenido)
            {
                tabla.Cell().Element(Valor).Text(caja);
            }
        });
    }

    private static void Direcciones(IContainer contenedor, IReadOnlyList<DireccionRetencionDto> filas)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(78);
                columnas.RelativeColumn();
                columnas.ConstantColumn(78);
                columnas.RelativeColumn();
            });

            tabla.Header(encabezado =>
            {
                foreach (var titulo in new[] { "FECHA", "DIRECCIÓN", "FECHA", "DIRECCIÓN" })
                {
                    encabezado.Cell().Element(Titulo).Text(titulo);
                }
            });

            foreach (var fila in filas)
            {
                tabla.Cell().Element(Celda).AlignCenter().Text(Fecha(fila.Fecha1));
                tabla.Cell().Element(Celda).Text(fila.Direccion1);
                tabla.Cell().Element(Celda).AlignCenter().Text(Fecha(fila.Fecha2));
                tabla.Cell().Element(Celda).Text(fila.Direccion2);
            }
        });
    }

    private static void Prestamos(IContainer contenedor, IReadOnlyList<PrestamoRetencionDto> filas)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(72);
                columnas.RelativeColumn(2);
                columnas.RelativeColumn(1.4f);
                columnas.RelativeColumn(2);
                columnas.RelativeColumn(1.6f);
                columnas.ConstantColumn(72);
                columnas.RelativeColumn(1.6f);
            });

            tabla.Header(encabezado =>
            {
                foreach (var titulo in new[]
                {
                    "Fecha préstamo", "Nombre del documento", "Motivo", "Nombre del solicitante",
                    "Firma del solicitante", "Fecha de devolución", "Firma de quien recibe"
                })
                {
                    encabezado.Cell().Element(Titulo).Text(titulo);
                }
            });

            foreach (var fila in filas)
            {
                tabla.Cell().Element(Celda).AlignCenter().Text(Fecha(fila.FechaPrestamo));
                tabla.Cell().Element(Celda).Text(fila.NombreDocumento);
                tabla.Cell().Element(Celda).Text(fila.Motivo);
                tabla.Cell().Element(Celda).Text(fila.NombreSolicitante);
                tabla.Cell().Element(caja => Firma(caja, fila.FirmaSolicitante));
                tabla.Cell().Element(Celda).AlignCenter().Text(Fecha(fila.FechaDevolucion));
                tabla.Cell().Element(caja => Firma(caja, fila.FirmaRecibe));
            }
        });
    }

    private static void Firma(IContainer contenedor, string valor)
    {
        var imagen = Imagen(valor);
        var celda = contenedor.Border(0.6f).BorderColor(Tinta).MinHeight(28).Padding(2);
        if (imagen is null)
        {
            celda.Text("");
            return;
        }

        celda.AlignCenter().AlignMiddle().Height(26).Image(imagen).FitArea();
    }

    private static byte[]? Imagen(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var datos = valor.Trim();
        var coma = datos.IndexOf(',');
        if (datos.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && coma >= 0)
        {
            datos = datos[(coma + 1)..];
        }

        try
        {
            var bytes = Convert.FromBase64String(datos);
            return bytes.Length is > 0 and <= 1_500_000 && EsImagen(bytes) ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool EsImagen(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return true;
        }

        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return true;
        }

        return bytes.Length >= 12
            && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
    }

    private static List<T> ConMinimo<T>(IReadOnlyList<T> filas, int minimo, Func<T> vacia)
    {
        var lista = filas.ToList();
        while (lista.Count < minimo)
        {
            lista.Add(vacia());
        }

        return lista;
    }

    private static string Fecha(string valor) =>
        DateOnly.TryParse(valor, out var fecha) ? fecha.ToString("dd/MM/yyyy") : valor;

    private static IContainer Etiqueta(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor(Tinta).Background("#f4f7fb").Padding(3).AlignMiddle();

    private static IContainer Valor(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor(Tinta).MinHeight(16).Padding(3).AlignMiddle();

    private static IContainer Titulo(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor(Tinta).Background("#f4f7fb").Padding(3).AlignMiddle();

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor(Tinta).MinHeight(18).Padding(3).AlignMiddle();
}
