using System.Text.Json;
using System.Text.RegularExpressions;
using CentroInvestigaciones.Application.Agenda;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenAgendaLocal : IAlmacenAgenda
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ruta;
    private readonly string _libro;
    private readonly ILogger<AlmacenAgendaLocal> _registro;
    private readonly Lock _bloqueo = new();
    private readonly List<CitaAgenda> _citas = [];
    private readonly List<MonitoriaAgenda> _monitorias = [];
    private readonly List<DateOnly> _festivos = [];

    public AlmacenAgendaLocal(IWebHostEnvironment entorno, ILogger<AlmacenAgendaLocal> registro)
    {
        _registro = registro;
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "agenda.json");
        _libro = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "..", "AgendaApp", "BD Agendas Investigacion.xlsx"));
        Cargar();
    }

    public IReadOnlyList<CitaAgenda> Citas()
    {
        lock (_bloqueo)
        {
            return _citas.ToList();
        }
    }

    public IReadOnlyList<MonitoriaAgenda> Monitorias()
    {
        lock (_bloqueo)
        {
            return _monitorias.ToList();
        }
    }

    public IReadOnlyList<DateOnly> Festivos()
    {
        lock (_bloqueo)
        {
            return _festivos.ToList();
        }
    }

    public void GuardarCita(CitaAgenda cita)
    {
        lock (_bloqueo)
        {
            var indice = _citas.FindIndex(item => item.Id == cita.Id);
            if (indice >= 0)
            {
                _citas[indice] = cita;
            }
            else
            {
                _citas.Add(cita);
            }

            GuardarSinBloqueo();
        }
    }

    public bool EliminarCita(Guid id)
    {
        lock (_bloqueo)
        {
            var quitadas = _citas.RemoveAll(item => item.Id == id);
            if (quitadas > 0)
            {
                GuardarSinBloqueo();
            }

            return quitadas > 0;
        }
    }

    public void GuardarMonitoria(MonitoriaAgenda monitoria)
    {
        lock (_bloqueo)
        {
            var indice = _monitorias.FindIndex(item => item.Id == monitoria.Id);
            if (indice >= 0)
            {
                _monitorias[indice] = monitoria;
            }
            else
            {
                _monitorias.Add(monitoria);
            }

            GuardarSinBloqueo();
        }
    }

    public bool EliminarMonitoria(Guid id)
    {
        lock (_bloqueo)
        {
            var quitadas = _monitorias.RemoveAll(item => item.Id == id);
            if (quitadas > 0)
            {
                GuardarSinBloqueo();
            }

            return quitadas > 0;
        }
    }

    private void Cargar()
    {
        if (File.Exists(_ruta))
        {
            try
            {
                var archivo = JsonSerializer.Deserialize<ArchivoAgenda>(File.ReadAllText(_ruta), Json);
                if (archivo is not null)
                {
                    _citas.AddRange(archivo.Citas);
                    _monitorias.AddRange(archivo.Monitorias);
                    _festivos.AddRange(archivo.Festivos);
                    var separadas = 0;
                    foreach (var cita in _citas)
                    {
                        if (SepararPaciente(cita))
                        {
                            separadas++;
                        }
                    }

                    if (separadas > 0)
                    {
                        GuardarSinBloqueo();
                        _registro.LogInformation("Se separó el campo paciente en {Cantidad} registros.", separadas);
                    }
                }

                return;
            }
            catch (Exception excepcion)
            {
                _registro.LogWarning(excepcion, "No se pudo leer datos-locales/agenda.json. No se reemplaza el archivo.");
                return;
            }
        }

        if (!File.Exists(_libro))
        {
            _registro.LogWarning("No está el libro de agendas en {Ruta}.", _libro);
            return;
        }

        var (citas, monitorias, festivos) = LectorLibroAgenda.Leer(_libro);
        foreach (var cita in citas)
        {
            SepararPaciente(cita);
        }

        _citas.AddRange(citas);
        _monitorias.AddRange(monitorias);
        _festivos.AddRange(festivos);
        GuardarSinBloqueo();
        _registro.LogInformation(
            "Agenda sembrada: {Citas} citas, {Monitorias} monitorías, {Festivos} festivos.",
            _citas.Count,
            _monitorias.Count,
            _festivos.Count);
    }

    private static bool SepararPaciente(CitaAgenda cita)
    {
        if (cita.Campos is null || !cita.Campos.TryGetValue("paciente", out var crudo))
        {
            return false;
        }

        cita.Campos.Remove("paciente");
        var texto = (crudo ?? "").Trim();
        if (texto.Length == 0)
        {
            return true;
        }

        var nombre = texto;
        var codigo = "";
        var conGuion = Regex.Match(texto, @"^(?<codigo>[A-Za-z0-9][A-Za-z0-9./-]{0,24})\s*[-–|]\s*(?<nombre>\p{L}.+)$");
        if (conGuion.Success && conGuion.Groups["codigo"].Value.Any(char.IsDigit))
        {
            codigo = conGuion.Groups["codigo"].Value.Trim();
            nombre = conGuion.Groups["nombre"].Value.Trim();
        }
        else
        {
            var alFinal = Regex.Match(texto, @"^(?<nombre>\p{L}[\p{L} .'&]{2,}?)\s+(?<codigo>(?:\d{2,}(?:-\d+)?)|(?:[A-Za-z]{1,8}-[\w.-]+))$");
            if (alFinal.Success)
            {
                nombre = alFinal.Groups["nombre"].Value.Trim();
                codigo = alFinal.Groups["codigo"].Value.Trim();
            }
            else if (Regex.IsMatch(texto, @"^(?=.*\d)[A-Za-z0-9][A-Za-z0-9./-]{0,20}$"))
            {
                codigo = texto;
                nombre = "";
            }
        }

        if (!cita.Campos.ContainsKey("nombrePaciente"))
        {
            cita.Campos["nombrePaciente"] = nombre;
        }

        if (codigo.Length > 0 && !cita.Campos.ContainsKey("codigoPaciente"))
        {
            cita.Campos["codigoPaciente"] = codigo;
        }

        return true;
    }

    private void GuardarSinBloqueo()
    {
        var archivo = new ArchivoAgenda
        {
            Citas = _citas,
            Monitorias = _monitorias,
            Festivos = _festivos
        };
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(archivo, Json));
    }

    private sealed class ArchivoAgenda
    {
        public List<CitaAgenda> Citas { get; set; } = [];

        public List<MonitoriaAgenda> Monitorias { get; set; } = [];

        public List<DateOnly> Festivos { get; set; } = [];
    }
}
