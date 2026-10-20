using CentroInvestigaciones.Application.Plantillas;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class FirmasInvestigadorLocal : IFirmasInvestigador
{
    private static readonly string[] Extensiones = [".png", ".jpg", ".jpeg", ".webp"];

    private readonly string _carpeta;

    public FirmasInvestigadorLocal(IWebHostEnvironment entorno)
    {
        _carpeta = Path.Combine(entorno.ContentRootPath, "Recursos", "firmas");
        Directory.CreateDirectory(_carpeta);
    }

    public byte[]? Buscar(string nombre, string titulo)
    {
        var carpeta = Ruta(nombre, titulo);
        if (carpeta is not null && Directory.Exists(carpeta))
        {
            var imagen = Directory.EnumerateFiles(carpeta)
                .FirstOrDefault(archivo => Extensiones.Contains(Path.GetExtension(archivo), StringComparer.OrdinalIgnoreCase));
            if (imagen is not null)
            {
                return File.ReadAllBytes(imagen);
            }
        }

        if (!Directory.Exists(_carpeta))
        {
            return null;
        }

        var clave = Normalizar(Carpeta(nombre, titulo));
        foreach (var archivo in Directory.EnumerateFiles(_carpeta))
        {
            if (!Extensiones.Contains(Path.GetExtension(archivo), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(Normalizar(Path.GetFileNameWithoutExtension(archivo)), clave, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Normalizar(Path.GetFileNameWithoutExtension(archivo)), Normalizar(nombre), StringComparison.OrdinalIgnoreCase))
            {
                return File.ReadAllBytes(archivo);
            }
        }

        return null;
    }

    public void Asegurar(string nombre, string titulo)
    {
        var carpeta = RutaSegura(nombre, titulo);
        if (carpeta is null)
        {
            return;
        }

        Directory.CreateDirectory(carpeta);
    }

    public void Renombrar(string nombreAnterior, string tituloAnterior, string nombre, string titulo)
    {
        var origen = Ruta(nombreAnterior, tituloAnterior);
        var destino = RutaSegura(nombre, titulo);
        if (destino is null)
        {
            return;
        }

        if (origen is not null && Directory.Exists(origen) && !string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
            Directory.Move(origen, destino);
            return;
        }

        Asegurar(nombre, titulo);
    }

    public void Eliminar(string nombre, string titulo)
    {
        var carpeta = Ruta(nombre, titulo);
        if (carpeta is not null && Directory.Exists(carpeta))
        {
            Directory.Delete(carpeta, true);
        }
    }

    private string? Ruta(string nombre, string titulo)
    {
        if (!Directory.Exists(_carpeta))
        {
            return null;
        }

        var clave = Normalizar(Carpeta(nombre, titulo));
        return Directory.EnumerateDirectories(_carpeta)
            .FirstOrDefault(carpeta => string.Equals(Normalizar(Path.GetFileName(carpeta)), clave, StringComparison.OrdinalIgnoreCase));
    }

    private static string Carpeta(string nombre, string titulo) =>
        string.IsNullOrWhiteSpace(titulo) ? nombre.Trim() : $"{nombre.Trim()} - {titulo.Trim()}";

    private string? RutaSegura(string nombre, string titulo)
    {
        var limpio = NombreCarpeta(nombre, titulo);
        if (limpio.Length == 0 || limpio is "." or ".." || limpio.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var raiz = Path.GetFullPath(_carpeta);
        var ruta = Path.GetFullPath(Path.Combine(raiz, limpio));
        var prefijo = raiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return ruta.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) ? ruta : null;
    }

    private static string NombreCarpeta(string nombre, string titulo)
    {
        var texto = Carpeta(nombre, titulo);
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(texto.Select(caracter => invalidos.Contains(caracter) ? ' ' : caracter).ToArray());
        return string.Join(' ', limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string Normalizar(string nombre)
    {
        var partes = nombre.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', partes);
    }
}
