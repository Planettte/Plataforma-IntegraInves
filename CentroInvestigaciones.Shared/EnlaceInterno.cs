namespace CentroInvestigaciones.Shared;

public static class EnlaceInterno
{
    public static bool EsRuta(string? enlace)
    {
        if (string.IsNullOrWhiteSpace(enlace))
        {
            return false;
        }

        var texto = enlace.Trim();
        return texto.StartsWith('/')
            && !texto.StartsWith("//", StringComparison.Ordinal)
            && !texto.Contains(':')
            && !texto.Contains('\\');
    }
}
