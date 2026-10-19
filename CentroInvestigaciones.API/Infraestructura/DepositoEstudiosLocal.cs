using CentroInvestigaciones.Application.Estudios;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class DepositoEstudiosLocal : IDepositoEstudios
{
    private readonly string _raiz;

    public DepositoEstudiosLocal(IWebHostEnvironment entorno)
    {
        _raiz = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "datos-locales", "estudios-archivos"));
        Directory.CreateDirectory(_raiz);
    }

    public string Guardar(Guid estudioId, string nombreArchivo, Stream contenido)
    {
        var nombre = Path.GetFileName(nombreArchivo);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "archivo";
        }

        var unico = $"{Guid.NewGuid():N}-{nombre}";
        var relativo = Path.Combine(estudioId.ToString("N"), unico);
        var destino = RutaFisica(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        using var archivo = File.Create(destino);
        contenido.CopyTo(archivo);
        return relativo.Replace('\\', '/');
    }

    public Stream Abrir(string ruta) => File.OpenRead(RutaFisica(ruta));

    public void Eliminar(string ruta)
    {
        var fisica = RutaFisica(ruta);
        if (File.Exists(fisica))
        {
            File.Delete(fisica);
        }
    }

    private string RutaFisica(string ruta) => RutaDeposito.Resolver(_raiz, ruta);
}
