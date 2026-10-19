using CentroInvestigaciones.Shared;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfCarta
{
    private readonly byte[]? _fondo;
    private readonly bool _fondoCompleto;

    public GeneradorPdfCarta(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "formato-plantilla.pdf");
        if (!File.Exists(ruta))
        {
            return;
        }

        _fondo = ExtraerJpeg(ruta);
        _fondoCompleto = _fondo is not null && EsPagina(Ancho(_fondo));
    }

    public byte[] Generar(IReadOnlyList<LineaCartaDto> lineas, IReadOnlyDictionary<string, string> campos, byte[]? firma, string firmante)
    {
        var paginas = Partir(lineas);
        return Document.Create(contenedor =>
        {
            foreach (var bloque in paginas)
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter);
                    pagina.Margin(0);
                    pagina.DefaultTextStyle(estilo => estilo.FontSize(11).FontColor("#1a1a2e").LineHeight(1.35f));

                    if (_fondo is { Length: > 0 })
                    {
                        pagina.Background().Image(_fondo).FitArea();
                    }

                    pagina.Content()
                        .PaddingTop(_fondoCompleto ? 92 : 36)
                        .PaddingBottom(_fondoCompleto ? 128 : 36)
                        .PaddingHorizontal(54)
                        .ScaleToFit()
                        .Column(columna =>
                        {
                            foreach (var linea in bloque)
                            {
                                Pintar(columna, linea, campos, firma, firmante);
                            }
                        });
                });
            }
        }).GeneratePdf();
    }

    private static List<List<LineaCartaDto>> Partir(IReadOnlyList<LineaCartaDto> lineas)
    {
        var paginas = new List<List<LineaCartaDto>>();
        var actual = new List<LineaCartaDto>();
        foreach (var linea in lineas)
        {
            if (linea.Salto)
            {
                if (actual.Count > 0)
                {
                    paginas.Add(actual);
                }

                actual = [];
                continue;
            }

            actual.Add(linea);
        }

        if (actual.Count > 0)
        {
            paginas.Add(actual);
        }

        return paginas.Count == 0 ? [new List<LineaCartaDto>()] : paginas;
    }

    private static void Pintar(ColumnDescriptor columna, LineaCartaDto linea, IReadOnlyDictionary<string, string> campos, byte[]? firma, string firmante)
    {
        if (linea.Tabla is not null)
        {
            PintarTabla(columna, linea.Tabla, campos, linea.Espacio);
            return;
        }

        if (linea.Trozos.Count == 1 && linea.Trozos[0].Tipo == "firma")
        {
            if (firma is { Length: > 0 } && ImagenValida.Es(firma))
            {
                columna.Item().PaddingTop(8).Width(160).Height(48).Image(firma).FitArea();
            }
            else
            {
                columna.Item().PaddingTop(8).Height(42).Text(" ");
            }

            return;
        }

        if (linea.Trozos.Count == 1 && linea.Trozos[0].Tipo == "texto" && EsTrazoDeLinea(linea.Trozos[0].Texto))
        {
            columna.Item().PaddingTop(6).PaddingBottom(linea.Espacio ? 10 : 4).Width(210).LineHorizontal(1).LineColor("#1a1a2e");
            return;
        }

        if (linea.Vineta && VinetaVacia(linea, campos))
        {
            return;
        }

        if (linea.Trozos.Count == 1 && linea.Trozos[0].Tipo == "texto"
            && linea.Trozos[0].Texto.Trim().Equals("Investigador Principal", StringComparison.OrdinalIgnoreCase)
            && string.Equals(firmante, "sub", StringComparison.OrdinalIgnoreCase))
        {
            columna.Item().PaddingBottom(linea.Espacio ? 10 : 1).Text("Subinvestigador");
            return;
        }

        var amplio = linea.Trozos.Count == 1 && linea.Trozos[0].Amplio && linea.Trozos[0].Tipo == "campo";
        if (amplio)
        {
            var valor = Valor(campos, linea.Trozos[0].Clave);
            if (string.IsNullOrWhiteSpace(valor))
            {
                columna.Item().PaddingBottom(linea.Espacio ? 10 : 2).Text(texto =>
                {
                    if (linea.Vineta)
                    {
                        texto.Span("• ");
                    }

                    texto.Span("\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0").Underline();
                });
                return;
            }

            var partes = valor.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < partes.Length; i++)
            {
                var ultimo = i == partes.Length - 1;
                var escrito = string.IsNullOrWhiteSpace(partes[i]) ? " " : partes[i];
                columna.Item().PaddingBottom(ultimo && linea.Espacio ? 10 : 1).Text(texto =>
                {
                    if (i == 0 && linea.Vineta)
                    {
                        texto.Span("• ");
                    }

                    texto.Span(escrito);
                });
            }

            return;
        }

        if (linea.Trozos.Count == 1 && linea.Trozos[0].Tipo == "campo" && linea.Trozos[0].Clave == "investigador")
        {
            columna.Item().PaddingBottom(linea.Espacio ? 10 : 1).Text(texto =>
            {
                texto.Span("Nombre del investigador: ");
                texto.Span(Valor(campos, "investigador")).SemiBold();
            });
            return;
        }

        columna.Item().PaddingBottom(linea.Espacio ? 10 : 1).Text(texto =>
        {
            if (linea.Vineta)
            {
                texto.Span("• ");
            }

            foreach (var trozo in linea.Trozos)
            {
                if (trozo.Tipo == "firma")
                {
                    continue;
                }

                if (trozo.Tipo == "texto")
                {
                    texto.Span(trozo.Texto);
                    continue;
                }

                var escrito = Valor(campos, trozo.Clave);
                if (string.IsNullOrWhiteSpace(escrito))
                {
                    texto.Span("\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0").Underline();
                    texto.Span(" ");
                }
                else
                {
                    texto.Span(escrito.TrimEnd() + " ");
                }
            }
        });
    }

    private static void PintarTabla(ColumnDescriptor columna, TablaCartaDto tabla, IReadOnlyDictionary<string, string> campos, bool espacio)
    {
        columna.Item().PaddingTop(4).PaddingBottom(espacio ? 12 : 8).Table(cuadricula =>
        {
            cuadricula.ColumnsDefinition(def =>
            {
                foreach (var _ in tabla.Columnas)
                {
                    def.RelativeColumn();
                }
            });

            cuadricula.Header(encabezado =>
            {
                foreach (var nombre in tabla.Columnas)
                {
                    encabezado.Cell().Border(0.6f).BorderColor("#1a1a2e").Background("#f4f7fb").Padding(3)
                        .Text(nombre).FontSize(7).SemiBold();
                }
            });

            foreach (var fila in tabla.Filas)
            {
                foreach (var celda in fila)
                {
                    var escrito = Valor(campos, celda.Clave);
                    cuadricula.Cell().Border(0.6f).BorderColor("#1a1a2e").Padding(3).MinHeight(22)
                        .Text(string.IsNullOrWhiteSpace(escrito) ? " " : escrito).FontSize(8);
                }
            }
        });
    }

    private static bool VinetaVacia(LineaCartaDto linea, IReadOnlyDictionary<string, string> campos)
    {
        if (linea.Trozos.Any(trozo => trozo.Tipo == "texto" && !string.IsNullOrWhiteSpace(trozo.Texto)))
        {
            return false;
        }

        var camposVineta = linea.Trozos.Where(trozo => trozo.Tipo == "campo").ToList();
        return camposVineta.Count > 0 && camposVineta.All(trozo => string.IsNullOrWhiteSpace(Valor(campos, trozo.Clave)));
    }

    private static bool EsTrazoDeLinea(string texto)
    {
        var limpio = texto.Trim();
        return limpio.Length >= 4 && limpio.All(caracter => caracter is '_' or '-' or '—' or '–' or ' ');
    }

    private static string Valor(IReadOnlyDictionary<string, string> campos, string clave) =>
        campos.TryGetValue(clave, out var valor) ? valor.Trim() : "";

    private static byte[]? ExtraerJpeg(string ruta)
    {
        using var documento = PdfReader.Open(ruta, PdfDocumentOpenMode.Import);
        if (documento.PageCount == 0)
        {
            return null;
        }

        var recursos = documento.Pages[0].Elements.GetDictionary("/Resources");
        var objetos = recursos?.Elements.GetDictionary("/XObject");
        if (objetos is null)
        {
            return null;
        }

        byte[]? mejor = null;
        foreach (var clave in objetos.Elements.Keys)
        {
            var imagen = objetos.Elements.GetDictionary(clave);
            if (imagen is null)
            {
                continue;
            }

            if (!string.Equals(imagen.Elements.GetName("/Subtype"), "/Image", StringComparison.Ordinal))
            {
                continue;
            }

            if (!EsJpeg(imagen))
            {
                continue;
            }

            var datos = imagen.Stream?.Value;
            if (datos is { Length: > 0 } && (mejor is null || datos.Length > mejor.Length))
            {
                mejor = datos;
            }
        }

        return mejor;
    }

    private static bool EsJpeg(PdfDictionary imagen)
    {
        var filtro = imagen.Elements.GetName("/Filter");
        if (string.Equals(filtro, "/DCTDecode", StringComparison.Ordinal))
        {
            return true;
        }

        var lista = imagen.Elements.GetArray("/Filter");
        if (lista is null)
        {
            return false;
        }

        for (var i = 0; i < lista.Elements.Count; i++)
        {
            if (lista.Elements[i] is PdfName nombre && nombre.Value == "/DCTDecode")
            {
                return true;
            }
        }

        return false;
    }

    private static bool EsPagina(int ancho) => ancho >= 700;

    private static int Ancho(byte[] jpeg)
    {
        for (var i = 0; i < jpeg.Length - 9; i++)
        {
            if (jpeg[i] != 0xFF)
            {
                continue;
            }

            var marca = jpeg[i + 1];
            if (marca is not (0xC0 or 0xC1 or 0xC2))
            {
                continue;
            }

            return (jpeg[i + 7] << 8) | jpeg[i + 8];
        }

        return 0;
    }
}
