namespace CentroInvestigaciones.API.Infraestructura;

internal static class RutaDeposito
{
    public static string Resolver(string raiz, string relativa)
    {
        var baseDir = Path.GetFullPath(raiz);
        var limpia = (relativa ?? "").Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var completa = Path.GetFullPath(Path.Combine(baseDir, limpia));
        var prefijo = baseDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!completa.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ruta de archivo no permitida.");
        }

        return completa;
    }
}
