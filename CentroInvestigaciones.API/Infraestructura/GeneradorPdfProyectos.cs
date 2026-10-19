using CentroInvestigaciones.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfProyectos
{
    private const string Tinta = "#1a1a2e";
    private const string Azul = "#1E4A8A";

    private readonly byte[]? _logo;

    public GeneradorPdfProyectos(IWebHostEnvironment entorno)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logo-foscal-marca.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public byte[] Generar(TableroProyectoDto tablero)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(16);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Tinta));
                pagina.Content().Column(columna =>
                {
                    columna.Item().Element(caja => Banner(caja, tablero));
                    if (tablero.Fases.Count > 0)
                    {
                        columna.Item().PaddingTop(8).Element(caja => Tarjetas(caja, tablero.Fases));
                    }

                    foreach (var fase in tablero.Fases)
                    {
                        columna.Item().PaddingTop(8).Element(caja => Tabla(caja, fase));
                    }
                });
            });
        }).GeneratePdf();
    }

    private void Banner(IContainer contenedor, TableroProyectoDto tablero)
    {
        contenedor.Background(Azul).Padding(10).Row(fila =>
        {
            if (_logo is { Length: > 0 })
            {
                fila.ConstantItem(78).Height(28).Image(_logo).FitArea();
            }

            fila.RelativeItem().PaddingLeft(8).Column(columna =>
            {
                columna.Item().Text(tablero.Nombre).FontSize(14).Bold().FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(tablero.Semillero))
                {
                    columna.Item().Text(tablero.Semillero).FontSize(8).FontColor(Colors.White);
                }

                columna.Item().Text($"{tablero.Progreso:0.0}% completado del proyecto").FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(tablero.ResponsableNombre))
                {
                    columna.Item().Text(tablero.ResponsableNombre).FontSize(8).FontColor(Colors.White);
                }
            });

            fila.ConstantItem(54).Height(54).Svg(Anillo(tablero.Progreso)).FitArea();
        });
    }

    private static void Tarjetas(IContainer contenedor, IReadOnlyList<TableroFaseDto> fases)
    {
        contenedor.Column(columna =>
        {
            for (var inicio = 0; inicio < fases.Count; inicio += 4)
            {
                var grupo = fases.Skip(inicio).Take(4).ToList();
                columna.Item().PaddingTop(inicio == 0 ? 0 : 6).Row(fila =>
                {
                    foreach (var fase in grupo)
                    {
                        fila.RelativeItem().PaddingRight(4).Border(0.6f).BorderColor("#d5dde8").Padding(6).Column(tarjeta =>
                        {
                            tarjeta.Item().Text(fase.Nombre).Bold();
                            tarjeta.Item().Text($"{fase.Progreso:0.0}%").FontSize(14).Bold().FontColor(Azul);
                            tarjeta.Item().PaddingTop(2).Height(4).Background("#e6eef8").Row(barra =>
                            {
                                var avance = Math.Clamp(fase.Progreso, 0, 100);
                                if (avance > 0)
                                {
                                    barra.RelativeItem((float)avance).Background(Azul);
                                }

                                if (avance < 100)
                                {
                                    barra.RelativeItem((float)(100 - avance));
                                }
                            });
                            tarjeta.Item().PaddingTop(3).Text(fase.Investigadores.Count == 0
                                ? "Sin investigadores"
                                : string.Join("  ·  ", fase.Investigadores.Select(item => $"{item.Nombre} {item.Porcentaje:0}%")))
                                .FontSize(7);
                        });
                    }

                    for (var i = grupo.Count; i < 4; i++)
                    {
                        fila.RelativeItem();
                    }
                });
            }
        });
    }

    private static void Tabla(IContainer contenedor, TableroFaseDto fase)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Background("#f4f7fb").Border(0.6f).BorderColor(Tinta).Padding(4)
                .Text($"{fase.Nombre}  ·  {fase.Actividades.Count} actividad{(fase.Actividades.Count == 1 ? "" : "es")}").Bold();
            if (fase.Actividades.Count == 0)
            {
                columna.Item().Border(0.6f).BorderColor(Tinta).Padding(6).Text("Sin actividades registradas");
                return;
            }

            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(definicion =>
                {
                    definicion.RelativeColumn(3);
                    definicion.ConstantColumn(68);
                    definicion.ConstantColumn(78);
                    definicion.ConstantColumn(40);
                    definicion.ConstantColumn(68);
                    definicion.RelativeColumn(2);
                });
                foreach (var titulo in new[] { "Actividad", "Inicio", "Fecha límite", "Peso", "Estado", "Investigadores" })
                {
                    tabla.Cell().Border(0.6f).BorderColor(Tinta).Background("#f4f7fb").Padding(3).Text(titulo).Bold();
                }

                foreach (var actividad in fase.Actividades)
                {
                    tabla.Cell().Element(Celda).Text(actividad.Descripcion);
                    tabla.Cell().Element(Celda).AlignCenter().Text(Fecha(actividad.FechaInicio));
                    tabla.Cell().Element(Celda).AlignCenter().Text(texto =>
                    {
                        foreach (var anterior in actividad.FechasAnteriores)
                        {
                            texto.Line(Fecha(anterior)).FontColor("#94a3b8").Strikethrough();
                        }

                        texto.Span(Fecha(actividad.FechaLimite));
                    });
                    tabla.Cell().Element(Celda).AlignCenter().Text($"{actividad.Peso}%");
                    tabla.Cell().Element(Celda).AlignCenter().Text(actividad.Estado);
                    tabla.Cell().Element(Celda).Text(actividad.Asignados.Count == 0
                        ? "Sin asignar"
                        : string.Join(", ", actividad.Asignados.Select(item =>
                            item.Prorrogas > 0 ? $"{item.Nombre} ({item.Prorrogas}p)" : item.Nombre)));
                }
            });
        });
    }

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.6f).BorderColor(Tinta).Padding(3).AlignMiddle();

    private static string Fecha(string valor) =>
        DateOnly.TryParse(valor, out var fecha) ? fecha.ToString("dd/MM/yyyy") : valor;

    private static string Anillo(decimal progreso)
    {
        var porcion = (double)Math.Clamp(progreso, 0, 100) / 100d;
        var circunferencia = 2 * Math.PI * 15;
        var avance = (circunferencia * porcion).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var resto = circunferencia.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36">
              <circle cx="18" cy="18" r="15" fill="none" stroke="#ffffff55" stroke-width="3"/>
              <circle cx="18" cy="18" r="15" fill="none" stroke="#ffffff" stroke-width="3" stroke-linecap="round" stroke-dasharray="{avance} {resto}" transform="rotate(-90 18 18)"/>
              <text x="18" y="20" text-anchor="middle" font-size="7" fill="#ffffff">{progreso:0}%</text>
            </svg>
            """;
    }
}
