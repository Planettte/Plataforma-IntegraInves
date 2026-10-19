using System.Net;
using System.Text;
using CentroInvestigaciones.Application.Dispositivos;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AvisadorDispositivos
{
    public const string CorreoPredeterminado = "medico.investigador3@foscal.com.co";
    public const string Ruta = "/estudios-clinicos/inventario-dispositivos";
    private static readonly TimeSpan IntervaloCorreo = TimeSpan.FromDays(7);

    private readonly IAlmacenDispositivos _almacen;
    private readonly IEnviadorCorreo _correo;
    private readonly AlmacenNotificaciones _notificaciones;
    private readonly IServiceScopeFactory _alcances;
    private readonly ILogger<AvisadorDispositivos> _registro;
    private readonly SemaphoreSlim _serie = new(1, 1);
    private string _origen = "https://localhost:7029";

    public AvisadorDispositivos(
        IAlmacenDispositivos almacen,
        IEnviadorCorreo correo,
        AlmacenNotificaciones notificaciones,
        IServiceScopeFactory alcances,
        ILogger<AvisadorDispositivos> registro)
    {
        _almacen = almacen;
        _correo = correo;
        _notificaciones = notificaciones;
        _alcances = alcances;
        _registro = registro;
    }

    public void RecordarOrigen(string origen)
    {
        if (!string.IsNullOrWhiteSpace(origen))
        {
            _origen = origen.TrimEnd('/');
        }
    }

    public async Task RevisarAsync(CancellationToken ct)
    {
        await _serie.WaitAsync(ct);
        try
        {
            await RevisarSinSerieAsync(ct);
        }
        finally
        {
            _serie.Release();
        }
    }

    private async Task RevisarSinSerieAsync(CancellationToken ct)
    {
        var hoy = SemaforoDispositivos.Hoy();
        var ultimo = _almacen.UltimoCorreoUtc();
        if (ultimo is not null && DateTime.UtcNow - ultimo.Value < IntervaloCorreo)
        {
            return;
        }

        var episodios = _almacen.Listar().SelectMany(item => SemaforoDispositivos.EpisodiosRojos(item, hoy)).ToList();
        if (episodios.Count == 0)
        {
            _almacen.MarcarCorreoEnviado(DateTime.UtcNow);
            return;
        }

        var destino = _almacen.CorreoEncargado();
        var envioOk = true;
        if (_correo.EstaConfigurado && !string.IsNullOrWhiteSpace(destino))
        {
            envioOk = await _correo.EnviarAsync(
                destino,
                "Dispositivos en rojo",
                PlantillaCorreo.Armar(
                    "Dispositivos en rojo",
                    "",
                    Cuerpo(episodios),
                    $"{_origen}{Ruta}",
                    "Abrir el inventario",
                    "Inventario de dispositivos"),
                null,
                ct);
        }

        if (!envioOk)
        {
            _registro.LogWarning("No se envió el aviso semanal de dispositivos en rojo. Se reintentará en la próxima revisión.");
            return;
        }

        var usuario = await BuscarDestinoAsync(destino, ct);
        if (usuario is not null)
        {
            _notificaciones.Agregar(usuario.Id, "Dispositivos en rojo", Mensaje(episodios), Ruta);
        }

        _almacen.MarcarCorreoEnviado(DateTime.UtcNow);
    }

    private async Task<Usuario?> BuscarDestinoAsync(string destino, CancellationToken ct)
    {
        await using var alcance = _alcances.CreateAsyncScope();
        var nucleo = alcance.ServiceProvider.GetRequiredService<IAlmacenNucleo>();
        var (usuarios, _) = await nucleo.ListarUsuariosAsync(0, 20, destino, nameof(EstadoCuenta.Activo), ct);
        return usuarios.FirstOrDefault(item =>
            string.Equals(item.CorreoElectronico, destino, StringComparison.OrdinalIgnoreCase));
    }

    private static string Cuerpo(IReadOnlyList<EpisodioRojo> episodios)
    {
        var lista = new StringBuilder();
        lista.Append("<p style=\"margin:0 0 12px;\">Aviso semanal. Estos dispositivos están en rojo:</p><ul style=\"margin:0;padding-left:18px;\">");
        foreach (var episodio in episodios)
        {
            var dm = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(episodio.Dm) ? "Sin DM" : episodio.Dm);
            var codigo = WebUtility.HtmlEncode(episodio.CodigoActivo);
            var lugar = WebUtility.HtmlEncode(string.Join(" / ", new[] { episodio.Ubicacion, episodio.Area }.Where(item => !string.IsNullOrWhiteSpace(item))));
            var tipo = WebUtility.HtmlEncode(SemaforoDispositivos.NombreTipo(episodio.Tipo));
            var etiqueta = WebUtility.HtmlEncode(episodio.Etiqueta);
            lista.Append($"<li style=\"margin:0 0 6px;\"><strong>{dm}</strong>");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                lista.Append($" — {codigo}");
            }

            if (!string.IsNullOrWhiteSpace(lugar))
            {
                lista.Append($" · {lugar}");
            }

            lista.Append($" · {tipo}: {etiqueta}</li>");
        }

        lista.Append("</ul>");
        return lista.ToString();
    }

    private static string Mensaje(IReadOnlyList<EpisodioRojo> episodios)
    {
        var nombres = episodios
            .Select(item => $"{(string.IsNullOrWhiteSpace(item.Dm) ? "Sin DM" : item.Dm)} ({SemaforoDispositivos.NombreTipo(item.Tipo)})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var texto = $"{episodios.Count} en rojo: {string.Join(", ", nombres)}";
        return texto.Length <= 280 ? texto : texto[..277] + "...";
    }
}
