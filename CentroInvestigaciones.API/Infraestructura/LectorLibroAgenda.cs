using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public static class LectorLibroAgenda
{
    public static (List<CitaAgenda> Citas, List<MonitoriaAgenda> Monitorias, List<DateOnly> Festivos) Leer(string ruta)
    {
        using var zip = ZipFile.OpenRead(ruta);
        var hojas = MapaHojas(zip);
        var textos = Textos(zip);
        var citas = new List<CitaAgenda>();
        AgregarCitas(citas, Filas(zip, hojas, textos, "Datos_Medicos"), ReglasAgenda.Medicos);
        AgregarCitas(citas, Filas(zip, hojas, textos, "Datos_Coordinacion"), ReglasAgenda.Coordinacion);
        AgregarCitas(citas, Filas(zip, hojas, textos, "Datos_DisenoDesarrollo"), ReglasAgenda.Diseno);
        var monitorias = Filas(zip, hojas, textos, "Monitorias")
            .Select(FilaMonitoria)
            .Where(item => item is not null)
            .Cast<MonitoriaAgenda>()
            .ToList();
        var festivos = Filas(zip, hojas, textos, "Festivos")
            .Select(fila => Fecha(fila, "A"))
            .Where(fecha => fecha is not null)
            .Select(fecha => fecha!.Value)
            .Distinct()
            .Order()
            .ToList();
        return (citas, monitorias, festivos);
    }

    private static void AgregarCitas(List<CitaAgenda> destino, IEnumerable<Dictionary<string, string>> filas, string agenda)
    {
        foreach (var fila in filas)
        {
            var cita = FilaCita(fila, agenda);
            if (cita is not null)
            {
                destino.Add(cita);
            }
        }
    }

    private static CitaAgenda? FilaCita(Dictionary<string, string> fila, string agenda)
    {
        if (!Guid.TryParse(Valor(fila, "A"), out var id) || EsVerdadero(Valor(fila, "G")))
        {
            return null;
        }

        var fecha = Fecha(fila, "B");
        if (fecha is null)
        {
            return null;
        }

        return new CitaAgenda
        {
            Id = id,
            Agenda = agenda,
            Fecha = fecha.Value,
            Persona = Valor(fila, "C"),
            HoraInicio = Hora(Valor(fila, "D")),
            HoraFin = Hora(Valor(fila, "E")),
            Campos = Campos(Valor(fila, "F")),
            Importante = EsVerdadero(Valor(fila, "L")),
            CreadoPorCorreo = Valor(fila, "I"),
            CreadoPorPersona = Valor(fila, "M"),
            Tipo = string.IsNullOrWhiteSpace(Valor(fila, "N")) ? ReglasAgenda.Citas : Valor(fila, "N").Trim().ToLowerInvariant()
        };
    }

    private static MonitoriaAgenda? FilaMonitoria(Dictionary<string, string> fila)
    {
        if (!Guid.TryParse(Valor(fila, "A"), out var id))
        {
            return null;
        }

        var fecha = Fecha(fila, "C");
        if (fecha is null || string.IsNullOrWhiteSpace(Valor(fila, "D")))
        {
            return null;
        }

        return new MonitoriaAgenda
        {
            Id = id,
            Fecha = fecha.Value,
            Descripcion = Valor(fila, "D"),
            CreadoPorPersona = Valor(fila, "F")
        };
    }

    private static Dictionary<string, string> Campos(string json)
    {
        var campos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return campos;
        }

        try
        {
            var mapa = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (mapa is null)
            {
                return campos;
            }

            foreach (var par in mapa)
            {
                campos[par.Key] = par.Value.ValueKind == JsonValueKind.String
                    ? par.Value.GetString() ?? ""
                    : par.Value.ToString();
            }
        }
        catch (JsonException)
        {
            return campos;
        }

        return campos;
    }

    private static string Hora(string valor)
    {
        if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero) && numero is > 0 and < 1)
        {
            var minutos = (int)Math.Round(numero * 24 * 60);
            return ReglasAgenda.Texto(minutos);
        }

        return valor.Length >= 5 ? valor[..5] : valor;
    }

    private static DateOnly? Fecha(Dictionary<string, string> fila, string columna)
    {
        var valor = Valor(fila, columna);
        if (string.IsNullOrWhiteSpace(valor) || valor.Equals("Fecha", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 1000)
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(Math.Floor(serial)));
        }

        return DateOnly.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha) ? fecha : null;
    }

    private static bool EsVerdadero(string valor) =>
        valor is "1" or "true" or "True";

    private static string Valor(Dictionary<string, string> fila, string columna) =>
        fila.TryGetValue(columna, out var valor) ? valor.Trim() : "";

    private static List<Dictionary<string, string>> Filas(ZipArchive zip, Dictionary<string, string> hojas, List<string> textos, string nombre)
    {
        if (!hojas.TryGetValue(nombre, out var entrada))
        {
            return [];
        }

        var xml = Leer(zip, entrada);
        var filas = new List<Dictionary<string, string>>();
        foreach (Match fila in Regex.Matches(xml, "<row[^>]*r=\"(\\d+)\"[^>]*>(.*?)</row>", RegexOptions.Singleline))
        {
            if (fila.Groups[1].Value == "1")
            {
                continue;
            }

            var celdas = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match celda in Regex.Matches(fila.Groups[2].Value, "<c[^>]*r=\"([A-Z]+)\\d+\"([^>]*)>(.*?)</c>", RegexOptions.Singleline))
            {
                celdas[celda.Groups[1].Value] = TextoCelda(celda.Groups[2].Value, celda.Groups[3].Value, textos);
            }

            if (celdas.Count > 0)
            {
                filas.Add(celdas);
            }
        }

        return filas;
    }

    private static string TextoCelda(string atributos, string cuerpo, List<string> textos)
    {
        var valor = Regex.Match(cuerpo, "<v>(.*?)</v>", RegexOptions.Singleline).Groups[1].Value;
        if (atributos.Contains("t=\"s\"", StringComparison.Ordinal) && int.TryParse(valor, out var indice) && indice >= 0 && indice < textos.Count)
        {
            return textos[indice];
        }

        return System.Net.WebUtility.HtmlDecode(valor);
    }

    private static Dictionary<string, string> MapaHojas(ZipArchive zip)
    {
        XNamespace principal = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relacion = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace paquete = "http://schemas.openxmlformats.org/package/2006/relationships";
        var libro = XDocument.Parse(Leer(zip, "xl/workbook.xml"));
        var rels = XDocument.Parse(Leer(zip, "xl/_rels/workbook.xml.rels"));
        var destinos = rels.Root!.Elements(paquete + "Relationship")
            .ToDictionary(item => (string)item.Attribute("Id")!, item => "xl/" + (string)item.Attribute("Target")!);
        return libro.Root!.Element(principal + "sheets")!.Elements(principal + "sheet")
            .ToDictionary(
                item => (string)item.Attribute("name")!,
                item => destinos[(string)item.Attribute(relacion + "id")!]);
    }

    private static List<string> Textos(ZipArchive zip)
    {
        var xml = Leer(zip, "xl/sharedStrings.xml");
        return Regex.Matches(xml, "<si>(.*?)</si>", RegexOptions.Singleline)
            .Select(item => string.Concat(Regex.Matches(item.Groups[1].Value, "<t[^>]*>(.*?)</t>", RegexOptions.Singleline)
                .Select(texto => System.Net.WebUtility.HtmlDecode(texto.Groups[1].Value))))
            .ToList();
    }

    private static string Leer(ZipArchive zip, string nombre)
    {
        var entrada = zip.GetEntry(nombre.Replace('\\', '/')) ?? throw new InvalidDataException(nombre);
        using var lector = new StreamReader(entrada.Open());
        return lector.ReadToEnd();
    }
}
