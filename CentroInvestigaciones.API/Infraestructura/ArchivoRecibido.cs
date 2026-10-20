namespace CentroInvestigaciones.API.Infraestructura;

internal static class ArchivoRecibido
{
    public const int MaximoAdicionales = 20;
    public const long TamanoMaximo = 20 * 1024 * 1024;

    public static async Task<MemoryStream?> LeerAsync(IFormFile archivo, CancellationToken ct = default)
    {
        if (archivo.Length == 0 || archivo.Length > TamanoMaximo || !ExtensionPermitida(archivo.FileName))
        {
            return null;
        }

        var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        if (!ContenidoValido(archivo.FileName, memoria.ToArray()))
        {
            await memoria.DisposeAsync();
            return null;
        }

        memoria.Position = 0;
        return memoria;
    }

    private static bool ExtensionPermitida(string nombre)
    {
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        return extension is ".pdf" or ".png" or ".jpg" or ".jpeg" or ".webp";
    }

    private static bool ContenidoValido(string nombre, byte[] bytes)
    {
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        if (extension == ".pdf")
        {
            return bytes.Length >= 5
                && bytes[0] == (byte)'%'
                && bytes[1] == (byte)'P'
                && bytes[2] == (byte)'D'
                && bytes[3] == (byte)'F'
                && bytes[4] == (byte)'-';
        }

        return extension is ".png" or ".jpg" or ".jpeg" or ".webp" && ImagenValida.Es(bytes);
    }
}
