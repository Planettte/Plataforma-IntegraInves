using System.Globalization;
using System.Text.Json;
using CentroInvestigaciones.Application.HojasVida;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

internal static class ImportadorHojasVida
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static int ImportarSiCorresponde(string carpetaDatos, List<HojaVida> hojas, IDepositoArchivos deposito)
    {
        var importar = Path.Combine(carpetaDatos, "importar");
        Directory.CreateDirectory(importar);
        var instrucciones = Path.Combine(importar, "instrucciones.txt");
        if (!File.Exists(instrucciones))
        {
            File.WriteAllText(instrucciones, """
                Para traer el Sheet anterior: expórtelo como CSV y guárdelo aquí con el nombre personas.csv.
                Copie los soportes en archivos/<nombre completo de la persona>/.
                Reinicie la API. La importación corre una sola vez.
                """);
        }

        var csv = Path.Combine(importar, "personas.csv");
        var marca = Path.Combine(importar, ".hecho");
        if (!File.Exists(csv) || File.Exists(marca))
        {
            return 0;
        }

        var filas = LeerCsv(File.ReadAllText(csv));
        if (filas.Count < 2)
        {
            File.WriteAllText(marca, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            return 0;
        }

        var encabezados = filas[0].Select(item => item.Trim()).ToList();
        var agregadas = 0;
        foreach (var fila in filas.Skip(1))
        {
            if (fila.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < encabezados.Count && i < fila.Count; i++)
            {
                mapa[encabezados[i]] = fila[i];
            }

            var nombre = Valor(mapa, "nombreCompleto");
            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            if (hojas.Any(item => string.Equals(item.NombreCompleto, nombre, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var hoja = new HojaVida
            {
                Id = Guid.NewGuid(),
                NombreCompleto = nombre.Trim(),
                TituloAcademico = Valor(mapa, "tituloAcademico"),
                DirInstitucion = Valor(mapa, "dirInstitucion"),
                DirCompleta = Valor(mapa, "dirCompleta"),
                DirCiudad = Valor(mapa, "dirCiudad"),
                DirEstado = Valor(mapa, "dirEstado"),
                DirProvincia = Valor(mapa, "dirProvincia"),
                DirPais = Valor(mapa, "dirPais"),
                DirCodigo = Valor(mapa, "dirCodigo"),
                Telefono = Valor(mapa, "telefono"),
                Extension = Valor(mapa, "extension"),
                VinculacionModalidad = Valor(mapa, "vinculacionModalidad"),
                VinculacionFechaInicio = Valor(mapa, "vinculacionFechaInicio"),
                Formacion = Lista<FormacionHoja>(Valor(mapa, "formacionAcademica")),
                ExperienciaLaboral = Lista<LaboralHoja>(Valor(mapa, "experienciaLaboral")),
                ExperienciaInvestigacion = Lista<InvestigacionHoja>(Valor(mapa, "experienciaInvestigacion")),
                GcpSiNo = string.IsNullOrWhiteSpace(Valor(mapa, "gcpSiNo")) ? "No" : Valor(mapa, "gcpSiNo"),
                GcpDetalle = Valor(mapa, "gcpDetalle"),
                OtrosSiNo = string.IsNullOrWhiteSpace(Valor(mapa, "otrosSiNo")) ? "No" : Valor(mapa, "otrosSiNo"),
                OtrosDetalle = Valor(mapa, "otrosDetalle"),
                FirmaFecha = Valor(mapa, "firmaFecha"),
                Version = int.TryParse(Valor(mapa, "version"), out var version) && version > 0 ? version : 1,
                FechaEdicionUtc = DateTime.UtcNow
            };
            hoja.Historial.Add(new EventoHojaVida
            {
                Version = hoja.Version,
                FechaUtc = hoja.FechaEdicionUtc,
                Usuario = "importación",
                Nota = "Importada desde el CSV del Sheet"
            });
            CopiarArchivos(Path.Combine(importar, "archivos", nombre.Trim()), hoja, deposito);
            hojas.Add(hoja);
            agregadas++;
        }

        File.WriteAllText(marca, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        return agregadas;
    }

    private static void CopiarArchivos(string carpeta, HojaVida hoja, IDepositoArchivos deposito)
    {
        if (!Directory.Exists(carpeta))
        {
            return;
        }

        var orden = 0;
        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
        {
            var nombre = Path.GetFileName(archivo);
            if (nombre.StartsWith("HV_", StringComparison.OrdinalIgnoreCase)
                || nombre.StartsWith("CV_", StringComparison.OrdinalIgnoreCase)
                || nombre.Equals(".hecho", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tipo = Tipo(nombre);
            if (tipo is null)
            {
                continue;
            }

            using var flujo = File.OpenRead(archivo);
            var enFirma = archivo.Contains($"{Path.DirectorySeparatorChar}_Firma{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || nombre.StartsWith("firma", StringComparison.OrdinalIgnoreCase);
            if (enFirma && tipo.StartsWith("image/", StringComparison.Ordinal))
            {
                hoja.FirmaNombre = nombre;
                hoja.FirmaTipo = tipo;
                hoja.FirmaRuta = deposito.Guardar(hoja.Id, "firma", nombre, flujo);
                continue;
            }

            hoja.Soportes.Add(new SoporteHoja
            {
                Id = Guid.NewGuid(),
                Nombre = nombre,
                Tipo = tipo,
                Orden = orden++,
                Ruta = deposito.Guardar(hoja.Id, "soportes", $"{Guid.NewGuid():N}{Path.GetExtension(nombre)}", flujo)
            });
        }
    }

    private static string? Tipo(string nombre)
    {
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => null
        };
    }

    private static string Valor(Dictionary<string, string> mapa, string clave) =>
        mapa.TryGetValue(clave, out var valor) ? valor.Trim() : "";

    private static List<T> Lista<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<List<string>> LeerCsv(string texto)
    {
        var filas = new List<List<string>>();
        var fila = new List<string>();
        var campo = new System.Text.StringBuilder();
        var comillas = false;
        for (var i = 0; i < texto.Length; i++)
        {
            var caracter = texto[i];
            if (comillas)
            {
                if (caracter == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        comillas = false;
                    }
                }
                else
                {
                    campo.Append(caracter);
                }

                continue;
            }

            switch (caracter)
            {
                case '"':
                    comillas = true;
                    break;
                case ',':
                    fila.Add(campo.ToString());
                    campo.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    fila.Add(campo.ToString());
                    campo.Clear();
                    filas.Add(fila);
                    fila = [];
                    break;
                default:
                    campo.Append(caracter);
                    break;
            }
        }

        if (campo.Length > 0 || fila.Count > 0)
        {
            fila.Add(campo.ToString());
            filas.Add(fila);
        }

        return filas;
    }
}
