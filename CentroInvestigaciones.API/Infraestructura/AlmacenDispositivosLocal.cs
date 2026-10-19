using System.Text.Json;
using CentroInvestigaciones.Application.Dispositivos;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenDispositivosLocal : IAlmacenDispositivos
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ruta;
    private readonly string _semilla;
    private readonly ILogger<AlmacenDispositivosLocal> _registro;
    private readonly Lock _bloqueo = new();
    private readonly List<DispositivoMedico> _dispositivos = [];
    private readonly List<string> _avisos = [];
    private string _correoEncargado = AvisadorDispositivos.CorreoPredeterminado;
    private DateTime? _ultimoCorreoUtc;

    public AlmacenDispositivosLocal(IWebHostEnvironment entorno, ILogger<AlmacenDispositivosLocal> registro)
    {
        _registro = registro;
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "dispositivos.json");
        _semilla = Path.Combine(entorno.ContentRootPath, "Recursos", "dispositivos-semilla.json");
        Cargar();
    }

    public IReadOnlyList<DispositivoMedico> Listar()
    {
        lock (_bloqueo)
        {
            return _dispositivos.OrderBy(item => item.Dm).ThenBy(item => item.CodigoActivo).ToList();
        }
    }

    public DispositivoMedico? Buscar(Guid id)
    {
        lock (_bloqueo)
        {
            return _dispositivos.FirstOrDefault(item => item.Id == id);
        }
    }

    public DispositivoMedico Crear(DispositivoMedico datos)
    {
        lock (_bloqueo)
        {
            datos.Id = Guid.NewGuid();
            Limpiar(datos);
            _dispositivos.Add(datos);
            GuardarSinBloqueo();
            return datos;
        }
    }

    public DispositivoMedico? Actualizar(Guid id, DispositivoMedico datos)
    {
        lock (_bloqueo)
        {
            var actual = _dispositivos.FirstOrDefault(item => item.Id == id);
            if (actual is null)
            {
                return null;
            }

            Limpiar(datos);
            actual.Dm = datos.Dm;
            actual.Marca = datos.Marca;
            actual.Modelo = datos.Modelo;
            actual.Riesgo = datos.Riesgo;
            actual.Serie = datos.Serie;
            actual.CodigoActivo = datos.CodigoActivo;
            actual.InventarioBiomedica = datos.InventarioBiomedica;
            actual.RegistroInvima = datos.RegistroInvima;
            actual.PeriodicidadMantenimiento = datos.PeriodicidadMantenimiento;
            actual.UltimoMantenimiento = datos.UltimoMantenimiento;
            actual.MesCalificacion = datos.MesCalificacion;
            actual.MesVerificacionMetrologica = datos.MesVerificacionMetrologica;
            actual.PeriodicidadCalibracion = datos.PeriodicidadCalibracion;
            actual.UltimaCalibracion = datos.UltimaCalibracion;
            actual.Ubicacion = datos.Ubicacion;
            actual.Area = datos.Area;
            actual.Observaciones = datos.Observaciones;
            GuardarSinBloqueo();
            return actual;
        }
    }

    public IReadOnlyList<string> Avisos()
    {
        lock (_bloqueo)
        {
            return _avisos.ToList();
        }
    }

    public void ReemplazarAvisos(IReadOnlyList<string> claves)
    {
        lock (_bloqueo)
        {
            _avisos.Clear();
            _avisos.AddRange(claves.Where(clave => !string.IsNullOrWhiteSpace(clave)).Distinct(StringComparer.Ordinal));
            GuardarSinBloqueo();
        }
    }

    public string CorreoEncargado()
    {
        lock (_bloqueo)
        {
            return _correoEncargado;
        }
    }

    public void GuardarCorreo(string correo)
    {
        lock (_bloqueo)
        {
            _correoEncargado = correo.Trim();
            GuardarSinBloqueo();
        }
    }

    public DateTime? UltimoCorreoUtc()
    {
        lock (_bloqueo)
        {
            return _ultimoCorreoUtc;
        }
    }

    public void MarcarCorreoEnviado(DateTime instante)
    {
        lock (_bloqueo)
        {
            _ultimoCorreoUtc = instante;
            GuardarSinBloqueo();
        }
    }

    private void Cargar()
    {
        if (File.Exists(_ruta))
        {
            try
            {
                var archivo = JsonSerializer.Deserialize<ArchivoDispositivos>(File.ReadAllText(_ruta), Json);
                if (archivo is not null)
                {
                    _dispositivos.AddRange(archivo.Dispositivos);
                    _avisos.AddRange(archivo.Avisos);
                    if (!string.IsNullOrWhiteSpace(archivo.CorreoEncargado))
                    {
                        _correoEncargado = archivo.CorreoEncargado.Trim();
                    }

                    _ultimoCorreoUtc = archivo.UltimoCorreoUtc;
                    if (_ultimoCorreoUtc is null)
                    {
                        _ultimoCorreoUtc = DateTime.UtcNow;
                        GuardarSinBloqueo();
                    }
                }

                return;
            }
            catch (Exception excepcion)
            {
                _registro.LogWarning(excepcion, "No se pudo leer datos-locales/dispositivos.json. No se reemplaza el archivo.");
                return;
            }
        }

        if (!File.Exists(_semilla))
        {
            _registro.LogWarning("No está la semilla de dispositivos en {Ruta}.", _semilla);
            return;
        }

        var semilla = JsonSerializer.Deserialize<List<DispositivoMedico>>(File.ReadAllText(_semilla), Json) ?? [];
        _dispositivos.AddRange(semilla.Where(item => item.Id != Guid.Empty));
        GuardarSinBloqueo();
    }

    private void GuardarSinBloqueo()
    {
        var archivo = new ArchivoDispositivos
        {
            Dispositivos = _dispositivos,
            Avisos = _avisos,
            CorreoEncargado = _correoEncargado,
            UltimoCorreoUtc = _ultimoCorreoUtc
        };
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(archivo, Json));
    }

    private static void Limpiar(DispositivoMedico datos)
    {
        datos.Dm = datos.Dm.Trim();
        datos.Marca = datos.Marca.Trim();
        datos.Modelo = datos.Modelo.Trim();
        datos.Riesgo = datos.Riesgo.Trim();
        datos.Serie = datos.Serie.Trim();
        datos.CodigoActivo = datos.CodigoActivo.Trim();
        datos.InventarioBiomedica = datos.InventarioBiomedica.Trim();
        datos.RegistroInvima = datos.RegistroInvima.Trim();
        datos.PeriodicidadMantenimiento = datos.PeriodicidadMantenimiento.Trim();
        datos.UltimoMantenimiento = datos.UltimoMantenimiento.Trim();
        datos.MesCalificacion = datos.MesCalificacion.Trim();
        datos.MesVerificacionMetrologica = datos.MesVerificacionMetrologica.Trim();
        datos.PeriodicidadCalibracion = datos.PeriodicidadCalibracion.Trim();
        datos.UltimaCalibracion = datos.UltimaCalibracion.Trim();
        datos.Ubicacion = datos.Ubicacion.Trim();
        datos.Area = datos.Area.Trim();
        datos.Observaciones = datos.Observaciones.Trim();
    }

    private sealed class ArchivoDispositivos
    {
        public List<DispositivoMedico> Dispositivos { get; set; } = [];

        public List<string> Avisos { get; set; } = [];

        public string CorreoEncargado { get; set; } = "";

        public DateTime? UltimoCorreoUtc { get; set; }
    }
}
