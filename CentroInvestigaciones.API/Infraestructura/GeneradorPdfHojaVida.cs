using CentroInvestigaciones.Application.HojasVida;
using CentroInvestigaciones.Domain;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class GeneradorPdfHojaVida
{
    private const string Azul = "#3d6fa8";
    private const string AzulMedio = "#4f87c0";
    private const string AzulClaro = "#e7f0f8";
    private const string Borde = "#d5e1ee";
    private const string Texto = "#1a1a2e";
    private const string PieFondo = "#f4f7fd";

    private readonly IDepositoArchivos _deposito;
    private readonly byte[]? _logo;

    static GeneradorPdfHojaVida()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public GeneradorPdfHojaVida(IDepositoArchivos deposito, IWebHostEnvironment entorno)
    {
        _deposito = deposito;
        var ruta = Path.Combine(entorno.ContentRootPath, "Recursos", "logofoscal.png");
        if (File.Exists(ruta))
        {
            _logo = File.ReadAllBytes(ruta);
        }
    }

    public byte[] Generar(HojaVida hoja, string idioma, IReadOnlyList<Guid> soportesIds, string? nota)
    {
        var textos = idioma == "en" ? Textos.Ingles : Textos.Espanol;
        _ = nota;
        var firma = LeerFirma(hoja);
        var cuerpo = Documento(hoja, textos, firma);
        using var salida = new PdfDocument();
        Anexar(salida, cuerpo);

        foreach (var id in soportesIds)
        {
            var soporte = hoja.Soportes.FirstOrDefault(item => item.Id == id);
            if (soporte is null)
            {
                continue;
            }

            using var origen = _deposito.Abrir(soporte.Ruta);
            using var memoria = new MemoryStream();
            origen.CopyTo(memoria);
            memoria.Position = 0;
            if (soporte.Tipo.StartsWith("image/", StringComparison.Ordinal))
            {
                Anexar(salida, PaginaImagen(memoria.ToArray()));
            }
            else if (soporte.Tipo == "application/pdf")
            {
                Anexar(salida, memoria.ToArray());
            }
        }

        using var resultado = new MemoryStream();
        salida.Save(resultado, false);
        return resultado.ToArray();
    }

    private byte[]? LeerFirma(HojaVida hoja)
    {
        if (string.IsNullOrWhiteSpace(hoja.FirmaRuta))
        {
            return null;
        }

        using var flujo = _deposito.Abrir(hoja.FirmaRuta);
        using var memoria = new MemoryStream();
        flujo.CopyTo(memoria);
        return memoria.ToArray();
    }

    private byte[] Documento(HojaVida hoja, Textos textos, byte[]? firma)
    {
        var fecha = DateTime.Now.ToString("yyyy-MM-dd");
        var direccion = Juntar(", ", hoja.DirCompleta, hoja.DirCiudad);
        var ciudad = Juntar(" · ", hoja.DirCiudad, hoja.DirEstado, hoja.DirProvincia);
        var pais = Juntar(" ", hoja.DirPais, hoja.DirCodigo);
        var telefono = string.IsNullOrWhiteSpace(hoja.Telefono)
            ? "—"
            : hoja.Telefono.Trim() + (string.IsNullOrWhiteSpace(hoja.Extension) ? "" : " ext. " + hoja.Extension.Trim());

        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(36);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Texto));
                pagina.Content().Border(1).BorderColor(AzulMedio).Column(columna =>
                {
                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def =>
                        {
                            def.ConstantColumn(100);
                            def.RelativeColumn();
                            def.ConstantColumn(158);
                        });
                        if (_logo is { Length: > 0 })
                        {
                            tabla.Cell().MinHeight(108).Border(0.6f).BorderColor(Borde).AlignCenter().AlignMiddle().Padding(10)
                                .MaxHeight(72).Image(_logo).FitArea();
                        }
                        else
                        {
                            tabla.Cell().MinHeight(108).Border(0.6f).BorderColor(Borde).AlignCenter().AlignMiddle().Padding(10)
                                .Text("FOSCAL").SemiBold().FontColor(Azul);
                        }
                        tabla.Cell().MinHeight(108).Border(0.6f).BorderColor(Borde).AlignCenter().AlignMiddle().Padding(8).Column(centro =>
                        {
                            centro.Item().AlignCenter().Text(textos.Titulo).FontSize(13).SemiBold().FontColor(Azul);
                            centro.Item().PaddingTop(3).AlignCenter().Text(textos.Subtitulo).FontSize(8).FontColor(AzulMedio);
                        });
                        tabla.Cell().MinHeight(108).Border(0.6f).BorderColor(Borde).AlignCenter().AlignMiddle().Padding(8).Column(control =>
                        {
                            control.Spacing(4);
                            control.Item().Row(fila => FilaControl(fila, textos.Version, hoja.Version.ToString()));
                            control.Item().Row(fila => FilaControl(fila, textos.Fecha, fecha));
                        });
                    });

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn(3);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                        });
                        Titulo(tabla, 4, textos.Personal);
                        Campo(tabla, 2, textos.Nombre, hoja.NombreCompleto);
                        Campo(tabla, 2, textos.TituloAcademico, hoja.TituloAcademico);
                        Campo(tabla, 4, textos.Institucion, string.IsNullOrWhiteSpace(hoja.DirInstitucion) ? "—" : hoja.DirInstitucion);
                        Campo(tabla, 1, textos.Direccion, direccion);
                        Campo(tabla, 1, textos.Ciudad, ciudad);
                        Campo(tabla, 1, textos.Pais, pais);
                        Campo(tabla, 1, textos.Telefono, telefono);
                    });

                    TablaDatos(columna, textos.Academica,
                        [textos.ColInstitucion, textos.ColTitulo, textos.ColAnio, textos.ColArea],
                        hoja.Formacion.Select(item => new[] { item.Institucion, item.Titulo, item.Anio, item.Area }).ToList(),
                        centrar: [2]);

                    TablaDatos(columna, textos.Laboral,
                        [textos.ColCargo, textos.ColInstitucion, textos.ColUbicacion, textos.ColPeriodo],
                        hoja.ExperienciaLaboral.Select(item => new[]
                        {
                            item.Posicion, item.Institucion, item.Locacion,
                            $"{item.FechaInicio} – {(string.IsNullOrWhiteSpace(item.FechaFin) ? textos.Actual : item.FechaFin)}"
                        }).ToList(),
                        centrar: [3]);

                    TablaDatos(columna, textos.Investigacion,
                        [textos.ColCodigo, textos.ColIndicacion, textos.ColFase, textos.ColPapel, textos.ColAnio],
                        hoja.ExperienciaInvestigacion.Select(item => new[]
                        {
                            item.Codigo, item.Indicacion, item.Fase, item.Papel, item.Anio
                        }).ToList(),
                        centrar: [2, 4]);

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn();
                            def.RelativeColumn();
                        });
                        Titulo(tabla, 2, textos.Vinculo);
                        Campo(tabla, 1, textos.Modalidad, string.IsNullOrWhiteSpace(hoja.VinculacionModalidad) ? "—" : hoja.VinculacionModalidad);
                        Campo(tabla, 1, textos.FechaVinculo, Fecha(hoja.VinculacionFechaInicio));
                    });

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn();
                            def.RelativeColumn();
                        });
                        Titulo(tabla, 2, textos.Certificaciones);
                        Subtitulo(tabla, textos.Gcp);
                        Subtitulo(tabla, textos.Otros);
                        tabla.Cell().Border(0.6f).BorderColor(Borde).Padding(6).Text(texto =>
                        {
                            texto.Span(hoja.GcpSiNo == "Sí" ? textos.Si : textos.NoAplica).SemiBold();
                            if (hoja.GcpSiNo == "Sí")
                            {
                                texto.Span(" — " + (hoja.GcpDetalle ?? ""));
                            }
                        });
                        tabla.Cell().Border(0.6f).BorderColor(Borde).Padding(6).Text(texto =>
                        {
                            texto.Span(hoja.OtrosSiNo == "Sí" ? textos.Si : textos.NoAplica).SemiBold();
                            if (hoja.OtrosSiNo == "Sí")
                            {
                                texto.Span(" — " + (hoja.OtrosDetalle ?? ""));
                            }
                        });
                    });

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def => def.RelativeColumn());
                        Titulo(tabla, 1, textos.Firma);
                        tabla.Cell().Border(0.6f).BorderColor(Borde).Padding(10).PaddingHorizontal(12).Column(firmaCol =>
                        {
                            if (firma is { Length: > 0 } && ImagenValida.Es(firma))
                            {
                                firmaCol.Item().Height(55).Width(160).Image(firma).FitArea();
                            }
                            else
                            {
                                firmaCol.Item().Height(64);
                            }

                            firmaCol.Item().PaddingTop(6).BorderTop(1).BorderColor(Azul).PaddingTop(6).Column(linea =>
                            {
                                linea.Item().Text(hoja.NombreCompleto).SemiBold().FontSize(9).FontColor(Azul);
                                linea.Item().PaddingTop(2).Text(hoja.TituloAcademico).FontSize(8).FontColor("#444444");
                                linea.Item().PaddingTop(3).Text($"{textos.FechaFirma}: {Fecha(hoja.FirmaFecha)}").FontSize(7).FontColor("#888888");
                            });
                        });
                    });

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn();
                            def.ConstantColumn(150);
                        });
                        tabla.Cell().Background(PieFondo).Border(0.6f).BorderColor(Borde).Padding(4).PaddingHorizontal(8)
                            .Text(textos.Pie).FontSize(7).FontColor("#555555");
                        tabla.Cell().Background(PieFondo).Border(0.6f).BorderColor(Borde).Padding(4).AlignRight()
                            .Text($"v{hoja.Version}  ·  {fecha}").FontSize(7).FontColor("#555555");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void FilaControl(RowDescriptor fila, string etiqueta, string valor)
    {
        fila.ConstantItem(46).Background(AzulClaro).Border(0.6f).BorderColor(Borde).Padding(3).PaddingHorizontal(4)
            .AlignMiddle().Text(etiqueta).FontSize(7.5f).SemiBold().FontColor(Azul);
        fila.RelativeItem().Border(0.6f).BorderColor(Borde).Padding(3).PaddingHorizontal(4)
            .AlignMiddle().Text(valor).FontSize(8).FontColor(Texto);
    }

    private static void Titulo(TableDescriptor tabla, uint columnas, string texto)
    {
        tabla.Cell().ColumnSpan(columnas).Background(Azul).Border(0.6f).BorderColor(Azul).Padding(4).PaddingHorizontal(8)
            .Text(texto.ToUpperInvariant()).FontSize(8.5f).SemiBold().FontColor(Colors.White);
    }

    private static void Subtitulo(TableDescriptor tabla, string texto)
    {
        tabla.Cell().Background(AzulClaro).Border(0.6f).BorderColor(Borde).Padding(3).PaddingHorizontal(8)
            .Text(texto).FontSize(8).SemiBold().FontColor(Azul);
    }

    private static void Campo(TableDescriptor tabla, uint columnas, string etiqueta, string? valor)
    {
        tabla.Cell().ColumnSpan(columnas).Border(0.6f).BorderColor(Borde).Padding(4).PaddingHorizontal(7).Column(columna =>
        {
            columna.Item().Text(etiqueta).FontSize(7.5f).SemiBold().FontColor(AzulMedio);
            columna.Item().MinHeight(14).Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(9).FontColor(Texto);
        });
    }

    private static void TablaDatos(ColumnDescriptor columna, string titulo, string[] encabezados, List<string[]> filas, int[] centrar)
    {
        var total = Math.Max(filas.Count, 3);
        columna.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(def =>
            {
                for (var i = 0; i < encabezados.Length; i++)
                {
                    def.RelativeColumn(centrar.Contains(i) ? 1 : 2);
                }
            });
            Titulo(tabla, (uint)encabezados.Length, titulo);
            foreach (var encabezado in encabezados)
            {
                tabla.Cell().Background(AzulMedio).Border(0.6f).BorderColor("#3d6fa8").Padding(3).AlignCenter()
                    .Text(encabezado).FontSize(8).SemiBold().FontColor(Colors.White);
            }

            for (var fila = 0; fila < total; fila++)
            {
                var datos = fila < filas.Count ? filas[fila] : null;
                for (var col = 0; col < encabezados.Length; col++)
                {
                    var valor = datos is null || col >= datos.Length ? "" : datos[col];
                    var celda = tabla.Cell().Border(0.6f).BorderColor(Borde).MinHeight(22).Padding(3).PaddingHorizontal(6);
                    var texto = celda.Text(valor).FontSize(8).FontColor(Texto);
                    if (centrar.Contains(col))
                    {
                        texto.AlignCenter();
                    }
                }
            }
        });
    }

    private static byte[] PaginaImagen(byte[] imagen)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(24);
                pagina.Content().AlignCenter().AlignMiddle().Image(imagen).FitArea();
            });
        }).GeneratePdf();
    }

    private static void Anexar(PdfDocument destino, byte[] origen)
    {
        using var memoria = new MemoryStream(origen);
        using var documento = PdfReader.Open(memoria, PdfDocumentOpenMode.Import);
        foreach (var pagina in documento.Pages)
        {
            destino.AddPage(pagina);
        }
    }

    private static string Juntar(string separador, params string?[] partes)
    {
        var texto = string.Join(separador, partes.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!.Trim()));
        return string.IsNullOrWhiteSpace(texto) ? "—" : texto;
    }

    private static string Fecha(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "—";
        }

        return DateTime.TryParse(valor, out var fecha) ? fecha.ToString("dd-MM-yyyy") : valor;
    }

    private sealed record Textos(
        string Titulo, string Subtitulo, string Version, string Fecha,
        string Personal, string Nombre, string TituloAcademico, string Institucion, string Direccion, string Ciudad, string Pais, string Telefono,
        string Academica, string ColInstitucion, string ColTitulo, string ColAnio, string ColArea,
        string Laboral, string ColCargo, string ColUbicacion, string ColPeriodo, string Actual,
        string Investigacion, string ColCodigo, string ColIndicacion, string ColFase, string ColPapel,
        string Vinculo, string Modalidad, string FechaVinculo,
        string Certificaciones, string Gcp, string Otros, string Si, string NoAplica,
        string Firma, string FechaFirma, string Pie)
    {
        public static Textos Espanol { get; } = new(
            "Hoja de Vida",
            "Fundación Oftalmológica de Santander — Foscal",
            "Versión", "Fecha",
            "1. Información Personal",
            "Nombres y apellidos(como aparecen en el documento de identidad)",
            "Título académico(por ejemplo,MD,BSc,Msc)",
            "Nombre de la institución", "Dirección", "Ciudad / Departamento", "País", "Teléfono",
            "2. Información académica",
            "Nombre y ubicación de la institucion(Ciudad, departamento y país)",
            "Título obtenido", "Año", "Área de estudios",
            "3. Experiencia laboral",
            "Posición(comience por la más actual)", "Ubicación(Ciudad, departamento y país)", "Período", "Actual",
            "4. Experiencia en investigación",
            "Código del estudio", "Especialidad", "Fase/Etapa Clínica (I-IV)", "Papel dentro de la investigación",
            "3.1 Vinculación con la Fundación Oftalmológica de Santander – FOSCAL",
            "Modalidad", "Fecha de inicio",
            "5. Certificaciones",
            "5.1 Entrenamiento ICH / GCP(Buenas practicas clínicas)",
            "5.2 Otros(Licencias, colegiaturas, publicaciones académicas)",
            "Sí", "No aplica",
            "6. Firma", "Firma y fecha",
            "Foscal — Fundación Oftalmológica de Santander · Documento confidencial de uso interno");

        public static Textos Ingles { get; } = new(
            "Curriculum Vitae",
            "Fundación Oftalmológica de Santander — Foscal",
            "Version", "Date",
            "1. Personal Information",
            "Full name", "Academic Qualifications",
            "Name of institution", "Address", "City / State", "Country", "Phone",
            "2. Academic Information",
            "Name and location of institution(City, State or Province, Country)",
            "Degree obtained", "Year", "Area of study",
            "3. Professional Experience",
            "Position / Title", "Location(City,state, country)", "Period", "Present",
            "4. Research Experience",
            "Study code", "Speciality", "Phase/Stage of trial (I-IV)", "Role in trial",
            "3.1 Affiliation with Fundación Oftalmológica de Santander – FOSCAL",
            "Modality", "Start date",
            "5. Certifications",
            "5.1 ICH / GCP Training",
            "5.2 Other(License, tuition, fees, academic publications)",
            "Yes", "Not applicable",
            "6. Signature", "Signature and date",
            "Foscal — Fundación Oftalmológica de Santander · Confidential document — internal use only");
    }
}
