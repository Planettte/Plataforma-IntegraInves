using System.Text.Json;
using CentroInvestigaciones.Application.Farmacia;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenFarmaciaLocal : IAlmacenFarmacia
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ruta;
    private readonly ILogger<AlmacenFarmaciaLocal> _registro;
    private readonly Lock _bloqueo = new();
    private readonly List<FormatoFarmacia> _formatos = [];
    private readonly List<FilaFarmacia> _inventario = [];

    public AlmacenFarmaciaLocal(IWebHostEnvironment entorno, ILogger<AlmacenFarmaciaLocal> registro)
    {
        _registro = registro;
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "farmacia.json");
        Cargar();
    }

    public IReadOnlyList<FormatoFarmacia> ListarFormatos()
    {
        lock (_bloqueo)
        {
            return _formatos
                .Where(item => item.Activo)
                .OrderByDescending(item => item.GeneradoEl ?? DateTimeOffset.MinValue)
                .ThenBy(item => item.NumeroEstudio)
                .ToList();
        }
    }

    public FormatoFarmacia? BuscarFormato(Guid id)
    {
        lock (_bloqueo)
        {
            return _formatos.FirstOrDefault(item => item.Id == id);
        }
    }

    public FormatoFarmacia GuardarFormato(FormatoFarmacia formato)
    {
        lock (_bloqueo)
        {
            Limpiar(formato);
            if (_formatos.Any(item => item.Id == formato.Id))
            {
                throw new InvalidOperationException("El formato está cerrado.");
            }

            if (formato.Id == Guid.Empty)
            {
                formato.Id = Guid.NewGuid();
            }

            formato.GeneradoEl = DateTimeOffset.Now;
            formato.Activo = true;
            formato.EliminadoEl = null;
            formato.EliminadoPorId = null;
            formato.EliminadoPorNombre = "";
            _formatos.Add(formato);

            GuardarSinBloqueo();
            return formato;
        }
    }

    public bool DesactivarFormato(Guid id, Guid? usuarioId, string nombre)
    {
        lock (_bloqueo)
        {
            var actual = _formatos.FirstOrDefault(item => item.Id == id && item.Activo);
            if (actual is null)
            {
                return false;
            }

            actual.Activo = false;
            actual.EliminadoEl = DateTimeOffset.Now;
            actual.EliminadoPorId = usuarioId;
            actual.EliminadoPorNombre = nombre.Trim();
            GuardarSinBloqueo();
            return true;
        }
    }

    public IReadOnlyList<FilaFarmacia> ListarInventario()
    {
        lock (_bloqueo)
        {
            return _inventario
                .OrderBy(item => item.CodigoMedicamento)
                .ThenBy(item => item.Lote)
                .ToList();
        }
    }

    public FilaFarmacia GuardarInventario(FilaFarmacia fila)
    {
        lock (_bloqueo)
        {
            Limpiar(fila);
            var actual = _inventario.FirstOrDefault(item => item.Id == fila.Id);
            if (actual is null)
            {
                if (fila.Id == Guid.Empty)
                {
                    fila.Id = Guid.NewGuid();
                }

                _inventario.Add(fila);
            }
            else
            {
                Copiar(fila, actual);
                fila = actual;
            }

            GuardarSinBloqueo();
            return fila;
        }
    }

    public bool EliminarInventario(Guid id)
    {
        lock (_bloqueo)
        {
            var quitados = _inventario.RemoveAll(item => item.Id == id);
            if (quitados == 0)
            {
                return false;
            }

            GuardarSinBloqueo();
            return true;
        }
    }

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            GuardarSinBloqueo();
            return;
        }

        try
        {
            var archivo = JsonSerializer.Deserialize<ArchivoFarmacia>(File.ReadAllText(_ruta), Json);
            if (archivo is null)
            {
                return;
            }

            _formatos.AddRange(archivo.Formatos);
            _inventario.AddRange(archivo.Inventario);
        }
        catch (Exception excepcion)
        {
            _registro.LogWarning(excepcion, "No se pudo leer datos-locales/farmacia.json. No se reemplaza el archivo.");
        }
    }

    private void GuardarSinBloqueo()
    {
        var archivo = new ArchivoFarmacia
        {
            Formatos = _formatos,
            Inventario = _inventario
        };
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(archivo, Json));
    }

    private static void Limpiar(FormatoFarmacia formato)
    {
        formato.NumeroEstudio = formato.NumeroEstudio.Trim();
        formato.Patrocinador = formato.Patrocinador.Trim();
        formato.Coordinador = formato.Coordinador.Trim();
        formato.NumeroCentro = formato.NumeroCentro.Trim();
        formato.Titulo = formato.Titulo.Trim();
        formato.InvestigadorPrincipal = formato.InvestigadorPrincipal.Trim();
        formato.CondicionesAlmacenamiento = formato.CondicionesAlmacenamiento.Trim();
        formato.DescripcionMedicamento = formato.DescripcionMedicamento.Trim();
        formato.Dosis = formato.Dosis.Trim();
        formato.Observaciones = formato.Observaciones.Trim();
        formato.ResponsableFarmacia = formato.ResponsableFarmacia.Trim();
        formato.FirmaCierre = formato.FirmaCierre.Trim();
        formato.CantidadFinal = formato.CantidadFinal.Trim();
        formato.Filas ??= [];
        foreach (var fila in formato.Filas)
        {
            if (fila.Id == Guid.Empty)
            {
                fila.Id = Guid.NewGuid();
            }

            Limpiar(fila);
        }
    }

    private static void Limpiar(FilaFarmacia fila)
    {
        fila.NumeroEnvio = fila.NumeroEnvio.Trim();
        fila.FechaRecibido = fila.FechaRecibido.Trim();
        fila.Lote = fila.Lote.Trim();
        fila.FechaVencimiento = fila.FechaVencimiento.Trim();
        fila.CodigoMedicamento = fila.CodigoMedicamento.Trim();
        fila.Conforme = fila.Conforme.Trim();
        fila.SujetoCodigo = fila.SujetoCodigo.Trim();
        fila.FechaEntrega = fila.FechaEntrega.Trim();
        fila.Entrega = fila.Entrega.Trim();
        fila.Recibe = fila.Recibe.Trim();
        fila.FechaRetorno = fila.FechaRetorno.Trim();
        fila.Empaque = fila.Empaque.Trim();
        fila.Unidades = fila.Unidades.Trim();
        fila.EntregaRetorno = fila.EntregaRetorno.Trim();
        fila.RecibeRetorno = fila.RecibeRetorno.Trim();
        fila.ControlMonitoria = fila.ControlMonitoria.Trim();
        fila.FechaDisposicion = fila.FechaDisposicion.Trim();
        fila.RetornoPatrocinador = fila.RetornoPatrocinador.Trim();
        fila.DestruccionActa = fila.DestruccionActa.Trim();
    }

    private static void Copiar(FilaFarmacia origen, FilaFarmacia destino)
    {
        destino.NumeroEnvio = origen.NumeroEnvio;
        destino.FechaRecibido = origen.FechaRecibido;
        destino.Lote = origen.Lote;
        destino.FechaVencimiento = origen.FechaVencimiento;
        destino.CodigoMedicamento = origen.CodigoMedicamento;
        destino.Conforme = origen.Conforme;
        destino.SujetoCodigo = origen.SujetoCodigo;
        destino.FechaEntrega = origen.FechaEntrega;
        destino.Entrega = origen.Entrega;
        destino.Recibe = origen.Recibe;
        destino.FechaRetorno = origen.FechaRetorno;
        destino.Empaque = origen.Empaque;
        destino.Unidades = origen.Unidades;
        destino.EntregaRetorno = origen.EntregaRetorno;
        destino.RecibeRetorno = origen.RecibeRetorno;
        destino.ControlMonitoria = origen.ControlMonitoria;
        destino.FechaDisposicion = origen.FechaDisposicion;
        destino.RetornoPatrocinador = origen.RetornoPatrocinador;
        destino.DestruccionActa = origen.DestruccionActa;
    }

    private sealed class ArchivoFarmacia
    {
        public List<FormatoFarmacia> Formatos { get; set; } = [];

        public List<FilaFarmacia> Inventario { get; set; } = [];
    }
}
