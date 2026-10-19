using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Navigation;

public static class PresentacionAgenda
{
    private static readonly string[] Paleta = ["#9BCB9B", "#E8A9C0", "#E8D077", "#E5A0A0", "#7FC8C8", "#C9A66B", "#B5CC6B", "#8FB8A8"];

    public static string Color(string? nombre)
    {
        var clave = (nombre ?? "").Trim().ToLowerInvariant();
        var fijo = clave switch
        {
            "jose manuel contreras sandoval" => "#F5A25D",
            "daniela castillo meza" => "#6C9BE0",
            "nathalia katherine amado bautista" => "#A78BDA",
            _ => ""
        };
        if (fijo.Length > 0)
        {
            return fijo;
        }

        var hash = 0u;
        foreach (var caracter in clave)
        {
            hash = unchecked(hash * 31 + caracter);
        }

        return Paleta[hash % (uint)Paleta.Length];
    }

    public static string Titulo(CitaAgendaDto cita)
    {
        var claves = cita.Agenda == "diseno"
            ? new[] { "actividad", "participante" }
            : cita.Tipo == "otros"
                ? ["actividad", "nombrePaciente", "codigoPaciente", "paciente"]
                : ["estudio", "nombrePaciente", "codigoPaciente", "paciente", "visita"];
        var texto = string.Join(" · ", claves
            .Select(clave => cita.Campos.TryGetValue(clave, out var valor) ? valor.Trim() : "")
            .Where(valor => valor.Length > 0));
        return texto.Length == 0 ? "Registro" : texto;
    }

    public static string FechaLarga(string iso, System.Globalization.CultureInfo cultura)
    {
        return DateOnly.TryParse(iso, out var fecha)
            ? fecha.ToString("dddd d 'de' MMMM yyyy", cultura)
            : iso;
    }
}
