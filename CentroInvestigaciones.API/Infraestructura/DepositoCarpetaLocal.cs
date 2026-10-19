namespace CentroInvestigaciones.API.Infraestructura;

public sealed class DepositoCarpetaLocal : Application.HojasVida.IDepositoArchivos
{
    private readonly string _raiz;

    public DepositoCarpetaLocal(IWebHostEnvironment entorno)
    {
        _raiz = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "datos-locales", "hojas-de-vida-archivos"));
        Directory.CreateDirectory(_raiz);
    }

    public string Guardar(Guid hojaId, string carpeta, string nombreArchivo, Stream contenido)
    {
        var nombre = Path.GetFileName(nombreArchivo);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "archivo";
        }

        var relativo = Path.Combine(hojaId.ToString("N"), carpeta, nombre);
        var destino = RutaFisica(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        using var archivo = File.Create(destino);
        contenido.CopyTo(archivo);
        return relativo.Replace('\\', '/');
    }

    public Stream Abrir(string ruta)
    {
        return File.OpenRead(RutaFisica(ruta));
    }

    public void Eliminar(string ruta)
    {
        var fisica = RutaFisica(ruta);
        if (File.Exists(fisica))
        {
            File.Delete(fisica);
        }
    }

    public void EliminarHoja(Guid hojaId)
    {
        var carpeta = Path.Combine(_raiz, hojaId.ToString("N"));
        if (Directory.Exists(carpeta))
        {
            Directory.Delete(carpeta, true);
        }
    }

    private string RutaFisica(string ruta) => RutaDeposito.Resolver(_raiz, ruta);
}
