using System.Globalization;
using System.Text;

namespace CentroInvestigaciones.Domain;

public sealed class SemaforoCalculado
{
    public string Estado { get; init; } = "gris";

    public string Etiqueta { get; init; } = "Sin dato";

    public DateOnly? Proxima { get; init; }

    public IReadOnlyList<VentanaSemaforo> Ventanas { get; init; } = [];
}

public sealed class VentanaSemaforo
{
    public int Indice { get; init; }

    public string Estado { get; init; } = "verde";

    public string Etiqueta { get; init; } = "Al día";

    public DateOnly Deadline { get; init; }
}

public sealed class EpisodioRojo
{
    public string Clave { get; init; } = "";

    public string Dm { get; init; } = "";

    public string CodigoActivo { get; init; } = "";

    public string Ubicacion { get; init; } = "";

    public string Area { get; init; } = "";

    public string Tipo { get; init; } = "";

    public string Etiqueta { get; init; } = "";
}

public static class SemaforoDispositivos
{
    public const int UmbralRojo = 1;
    public const int UmbralAmarillo = 3;

    private static readonly Dictionary<string, int> Meses = new(StringComparer.Ordinal)
    {
        ["ENE"] = 1, ["ENERO"] = 1,
        ["FEB"] = 2, ["FEBRERO"] = 2,
        ["MAR"] = 3, ["MARZO"] = 3,
        ["ABR"] = 4, ["ABRIL"] = 4,
        ["MAY"] = 5, ["MAYO"] = 5,
        ["JUN"] = 6, ["JUNIO"] = 6,
        ["JUL"] = 7, ["JULIO"] = 7,
        ["AGO"] = 8, ["AGOSTO"] = 8,
        ["SEP"] = 9, ["SEPT"] = 9, ["SEPTIEMBRE"] = 9,
        ["OCT"] = 10, ["OCTUBRE"] = 10,
        ["NOV"] = 11, ["NOVIEMBRE"] = 11,
        ["DIC"] = 12, ["DICIEMBRE"] = 12
    };

    private static readonly Dictionary<string, int> Periodicidades = new(StringComparer.Ordinal)
    {
        ["MENSUAL"] = 1,
        ["BIMESTRAL"] = 2,
        ["TRIMESTRAL"] = 3,
        ["CUATRIMESTRAL"] = 4,
        ["SEMESTRAL"] = 6,
        ["ANUAL"] = 12,
        ["BIANUAL"] = 24,
        ["BIENAL"] = 24
    };

    public static DateOnly Hoy()
    {
        foreach (var zona in new[] { "SA Pacific Standard Time", "America/Bogota" })
        {
            try
            {
                var info = TimeZoneInfo.FindSystemTimeZoneById(zona);
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, info));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return DateOnly.FromDateTime(DateTime.Now);
    }

    public static SemaforoCalculado Mantenimiento(DispositivoMedico dispositivo, DateOnly hoy) =>
        Periodico(dispositivo.UltimoMantenimiento, dispositivo.PeriodicidadMantenimiento, hoy);

    public static SemaforoCalculado Calibracion(DispositivoMedico dispositivo, DateOnly hoy) =>
        Periodico(dispositivo.UltimaCalibracion, dispositivo.PeriodicidadCalibracion, hoy);

    public static SemaforoCalculado Calificacion(DispositivoMedico dispositivo, DateOnly hoy) =>
        Calificacion(dispositivo.MesCalificacion, dispositivo.CalificacionVentana1, dispositivo.CalificacionVentana2, hoy);

    public static IReadOnlyList<EpisodioRojo> EpisodiosRojos(DispositivoMedico dispositivo, DateOnly hoy)
    {
        var episodios = new List<EpisodioRojo>();
        Agregar(episodios, dispositivo, "mantenimiento", Mantenimiento(dispositivo, hoy));
        Agregar(episodios, dispositivo, "calibracion", Calibracion(dispositivo, hoy));

        var calificacion = Calificacion(dispositivo, hoy);
        var rojas = calificacion.Ventanas.Where(ventana => ventana.Estado == "rojo").ToList();
        if (rojas.Count == 0)
        {
            Agregar(episodios, dispositivo, "calificacion", calificacion);
        }
        else
        {
            foreach (var ventana in rojas)
            {
                episodios.Add(Crear(dispositivo, "calificacion", ventana.Etiqueta,
                    $"{dispositivo.Id:N}|calificacion|{ventana.Deadline.Year}|{ventana.Indice}|{ventana.Deadline:yyyy-MM-dd}"));
            }
        }

        return episodios;
    }

    public static string NombreTipo(string tipo) => tipo switch
    {
        "mantenimiento" => "Mantenimiento",
        "calibracion" => "Calibración",
        "calificacion" => "Calificación",
        _ => tipo
    };

    private static void Agregar(List<EpisodioRojo> episodios, DispositivoMedico dispositivo, string tipo, SemaforoCalculado semaforo)
    {
        if (semaforo.Estado != "rojo")
        {
            return;
        }

        var marca = semaforo.Proxima?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? semaforo.Etiqueta;
        episodios.Add(Crear(dispositivo, tipo, semaforo.Etiqueta, $"{dispositivo.Id:N}|{tipo}|{marca}"));
    }

    private static EpisodioRojo Crear(DispositivoMedico dispositivo, string tipo, string etiqueta, string clave) => new()
    {
        Clave = clave,
        Dm = dispositivo.Dm,
        CodigoActivo = dispositivo.CodigoActivo,
        Ubicacion = dispositivo.Ubicacion,
        Area = dispositivo.Area,
        Tipo = tipo,
        Etiqueta = etiqueta
    };

    private static SemaforoCalculado Periodico(string? ultimaTexto, string? periodicidadTexto, DateOnly hoy)
    {
        if (EsNoAplica(periodicidadTexto) || EsNoAplica(ultimaTexto))
        {
            return NoAplica();
        }

        var meses = Periodicidad(periodicidadTexto);
        var ultima = ParseFecha(ultimaTexto);
        if (meses is null || ultima is null)
        {
            return SinDato();
        }

        var proxima = new DateOnly(ultima.Value.Year, ultima.Value.Month, 1).AddMonths(meses.Value);
        return PorProxima(proxima, hoy, "Vencido", "Vence pronto", "Próximo", "Al día");
    }

    private static SemaforoCalculado Calificacion(string? mesTexto, string? fecha1, string? fecha2, DateOnly hoy)
    {
        var ventanas = Ventanas(mesTexto);
        if (ventanas.Count == 0)
        {
            return NoAplica();
        }

        var checks = new[] { ParseFecha(fecha1), ParseFecha(fecha2) };
        var resultados = new List<VentanaSemaforo>();
        for (var i = 0; i < ventanas.Count; i++)
        {
            var ventana = ventanas[i];
            var deadline = FinDeMes(hoy.Year, ventana.Fin);
            var check = i < checks.Length ? checks[i] : null;
            if (check is not null && check.Value.Year == hoy.Year)
            {
                resultados.Add(new VentanaSemaforo { Indice = i, Estado = "verde", Etiqueta = "Cumplida", Deadline = deadline });
                continue;
            }

            var meses = MesesEntre(hoy, deadline);
            var (estado, etiqueta) = meses switch
            {
                < 0 => ("rojo", "Vencida"),
                <= UmbralRojo => ("rojo", "Vence pronto"),
                <= UmbralAmarillo => ("amarillo", "Próxima"),
                _ => ("verde", "Al día")
            };
            resultados.Add(new VentanaSemaforo { Indice = i, Estado = estado, Etiqueta = etiqueta, Deadline = deadline });
        }

        var orden = new Dictionary<string, int> { ["rojo"] = 0, ["amarillo"] = 1, ["verde"] = 2 };
        var peor = resultados[0];
        foreach (var resultado in resultados)
        {
            if (orden[resultado.Estado] < orden[peor.Estado])
            {
                peor = resultado;
            }
        }

        return new SemaforoCalculado
        {
            Estado = peor.Estado,
            Etiqueta = peor.Etiqueta,
            Proxima = peor.Deadline,
            Ventanas = resultados
        };
    }

    private static SemaforoCalculado PorProxima(DateOnly proxima, DateOnly hoy, string vencido, string pronto, string cercano, string alDia)
    {
        var meses = MesesEntre(hoy, proxima);
        if (meses < 0)
        {
            return new SemaforoCalculado { Estado = "rojo", Etiqueta = vencido, Proxima = proxima };
        }

        if (meses <= UmbralRojo)
        {
            return new SemaforoCalculado { Estado = "rojo", Etiqueta = pronto, Proxima = proxima };
        }

        if (meses <= UmbralAmarillo)
        {
            return new SemaforoCalculado { Estado = "amarillo", Etiqueta = cercano, Proxima = proxima };
        }

        return new SemaforoCalculado { Estado = "verde", Etiqueta = alDia, Proxima = proxima };
    }

    private static List<(int Inicio, int Fin)> Ventanas(string? texto)
    {
        var ventanas = new List<(int Inicio, int Fin)>();
        if (EsNoAplica(texto) || EsVacio(texto))
        {
            return ventanas;
        }

        foreach (var grupo in Normalizar(texto).Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var meses = grupo.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(Mes)
                .Where(mes => mes is not null)
                .Select(mes => mes!.Value)
                .ToList();
            if (meses.Count == 1)
            {
                ventanas.Add((meses[0], meses[0]));
            }
            else if (meses.Count >= 2)
            {
                ventanas.Add((meses[0], meses[^1]));
            }
        }

        return ventanas;
    }

    private static int? Periodicidad(string? texto)
    {
        if (EsNoAplica(texto) || EsVacio(texto))
        {
            return null;
        }

        return Periodicidades.TryGetValue(Normalizar(texto), out var meses) ? meses : null;
    }

    private static DateOnly? ParseFecha(string? texto)
    {
        if (EsVacio(texto) || EsNoAplica(texto))
        {
            return null;
        }

        var limpio = texto!.Trim();
        var normal = Normalizar(limpio);
        var partes = normal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length >= 2 && int.TryParse(partes[^1], out var anio) && anio is >= 1900 and <= 2500)
        {
            var nombre = partes.Length >= 3 && partes[^2] == "DE" ? partes[0] : partes[0];
            if (partes.Length == 2 || (partes.Length == 3 && partes[1] == "DE"))
            {
                var mes = Mes(nombre);
                if (mes is not null)
                {
                    return new DateOnly(anio, mes.Value, 1);
                }
            }
        }

        if (DateOnly.TryParseExact(limpio, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return iso;
        }

        if (DateOnly.TryParseExact(limpio, ["d/M/yyyy", "dd/MM/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var latina))
        {
            return latina;
        }

        if (DateTime.TryParse(limpio, CultureInfo.GetCultureInfo("es-CO"), DateTimeStyles.None, out var fecha))
        {
            return DateOnly.FromDateTime(fecha);
        }

        return null;
    }

    private static int? Mes(string? texto)
    {
        var normal = Normalizar(texto);
        return Meses.TryGetValue(normal, out var mes) ? mes : null;
    }

    private static int MesesEntre(DateOnly desde, DateOnly hasta) =>
        (hasta.Year - desde.Year) * 12 + (hasta.Month - desde.Month);

    private static DateOnly FinDeMes(int anio, int mes) =>
        new DateOnly(anio, mes, 1).AddMonths(1).AddDays(-1);

    private static bool EsNoAplica(string? texto) => Normalizar(texto) == "NO APLICA";

    private static bool EsVacio(string? texto)
    {
        var normal = Normalizar(texto);
        return normal.Length == 0 || normal is "-" or "--";
    }

    private static SemaforoCalculado NoAplica() => new() { Estado = "no_aplica", Etiqueta = "No aplica" };

    private static SemaforoCalculado SinDato() => new() { Estado = "gris", Etiqueta = "Sin dato" };

    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var limpio = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                limpio.Append(caracter);
            }
        }

        return limpio.ToString().Normalize(NormalizationForm.FormC);
    }
}
