using System.Text.Json;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenNotificaciones
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly TimeSpan Vigencia = TimeSpan.FromDays(2);

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();
    private readonly List<NotificacionUsuario> _items = [];

    public AlmacenNotificaciones(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "notificaciones.json");
        if (File.Exists(_ruta))
        {
            var lista = JsonSerializer.Deserialize<List<NotificacionUsuario>>(File.ReadAllText(_ruta), Json);
            if (lista is not null)
            {
                _items.AddRange(lista);
            }
        }
    }

    public void Agregar(Guid usuarioId, string titulo, string mensaje, string enlace)
    {
        lock (_bloqueo)
        {
            _items.Add(new NotificacionUsuario
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Titulo = titulo,
                Mensaje = mensaje,
                Enlace = EnlaceInterno.EsRuta(enlace) ? enlace.Trim() : "",
                FechaUtc = DateTime.UtcNow
            });
            GuardarSinBloqueo();
        }
    }

    public IReadOnlyList<NotificacionUsuario> ListarVigentes(Guid usuarioId)
    {
        lock (_bloqueo)
        {
            var limite = DateTime.UtcNow.Subtract(Vigencia);
            var quitadas = _items.RemoveAll(item => item.VistaUtc is DateTime vista && vista < limite);
            if (quitadas > 0)
            {
                GuardarSinBloqueo();
            }

            return _items
                .Where(item => item.UsuarioId == usuarioId)
                .OrderByDescending(item => item.FechaUtc)
                .ToList();
        }
    }

    public bool Pertenece(Guid usuarioId, Guid id)
    {
        lock (_bloqueo)
        {
            return _items.Any(item => item.Id == id && item.UsuarioId == usuarioId);
        }
    }

    public void MarcarVista(Guid usuarioId, Guid id)
    {
        lock (_bloqueo)
        {
            var aviso = _items.FirstOrDefault(item => item.Id == id && item.UsuarioId == usuarioId);
            if (aviso is null || aviso.VistaUtc is not null)
            {
                return;
            }

            aviso.VistaUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
        }
    }

    private void GuardarSinBloqueo()
    {
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(_items, Json));
    }
}
