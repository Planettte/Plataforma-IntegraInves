namespace CentroInvestigaciones.Domain;

public static class ReglasAgenda
{
    public const string Medicos = "medicos";
    public const string Coordinacion = "coordinacion";
    public const string Diseno = "diseno";
    public const string Citas = "citas";
    public const string Otros = "otros";
    public const int MinutoInicio = 6 * 60;
    public const int MinutoFin = 20 * 60;

    public static bool AgendaEstudios(string agenda) =>
        agenda is Medicos or Coordinacion;

    public static bool HoraValida(string? inicio, string? fin)
    {
        if (!Minutos(inicio, out var desde) || !Minutos(fin, out var hasta))
        {
            return false;
        }

        return desde >= MinutoInicio
            && hasta <= MinutoFin
            && hasta > desde
            && desde % 30 == 0
            && hasta % 30 == 0;
    }

    public static bool Solapa(string inicio, string fin, string otroInicio, string otroFin)
    {
        if (!Minutos(inicio, out var desde) || !Minutos(fin, out var hasta)
            || !Minutos(otroInicio, out var otroDesde) || !Minutos(otroFin, out var otroHasta))
        {
            return false;
        }

        return desde < otroHasta && otroDesde < hasta;
    }

    public static IEnumerable<string> HorasInicio()
    {
        for (var minuto = MinutoInicio; minuto < MinutoFin; minuto += 30)
        {
            yield return Texto(minuto);
        }
    }

    public static IEnumerable<string> HorasFin()
    {
        for (var minuto = MinutoInicio + 30; minuto <= MinutoFin; minuto += 30)
        {
            yield return Texto(minuto);
        }
    }

    public static bool Minutos(string? texto, out int minutos)
    {
        minutos = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var partes = texto.Trim().Split(':');
        if (partes.Length < 2 || !int.TryParse(partes[0], out var hora) || !int.TryParse(partes[1], out var minuto))
        {
            return false;
        }

        minutos = hora * 60 + minuto;
        return true;
    }

    public static string Texto(int minutos) => $"{minutos / 60:00}:{minutos % 60:00}";
}
