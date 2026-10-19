using System.Text.Json;
using CentroInvestigaciones.Application.Plantillas;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenPlantillasLocal : IAlmacenPlantillas
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();
    private readonly List<InvestigadorPlantilla> _investigadores = [];
    private readonly List<CoordinadorPlantilla> _coordinadores = [];
    private readonly List<EstudioDePlantillas> _estudios = [];

    public AlmacenPlantillasLocal(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "plantillas-estudios.json");
        Cargar();
    }

    public IReadOnlyList<InvestigadorPlantilla> Investigadores()
    {
        lock (_bloqueo)
        {
            return _investigadores.OrderBy(item => item.Nombre).ToList();
        }
    }

    public InvestigadorPlantilla AgregarInvestigador(string nombre, string titulo)
    {
        lock (_bloqueo)
        {
            var persona = new InvestigadorPlantilla
            {
                Id = Guid.NewGuid(),
                Nombre = nombre.Trim(),
                Titulo = titulo.Trim()
            };
            _investigadores.Add(persona);
            GuardarSinBloqueo();
            return persona;
        }
    }

    public InvestigadorPlantilla? ActualizarInvestigador(Guid id, string nombre, string titulo)
    {
        lock (_bloqueo)
        {
            var persona = _investigadores.FirstOrDefault(item => item.Id == id);
            if (persona is null)
            {
                return null;
            }

            persona.Nombre = nombre.Trim();
            persona.Titulo = titulo.Trim();
            GuardarSinBloqueo();
            return persona;
        }
    }

    public bool EliminarInvestigador(Guid id)
    {
        lock (_bloqueo)
        {
            if (_estudios.Any(item => item.InvestigadorPrincipalId == id || item.SubinvestigadorId == id))
            {
                return false;
            }

            var quitado = _investigadores.RemoveAll(item => item.Id == id) > 0;
            if (quitado)
            {
                GuardarSinBloqueo();
            }

            return quitado;
        }
    }

    public IReadOnlyList<CoordinadorPlantilla> Coordinadores()
    {
        lock (_bloqueo)
        {
            return _coordinadores.OrderBy(item => item.Nombre).ToList();
        }
    }

    public CoordinadorPlantilla AgregarCoordinador(string nombre, string correo)
    {
        lock (_bloqueo)
        {
            var persona = new CoordinadorPlantilla
            {
                Id = Guid.NewGuid(),
                Nombre = nombre.Trim(),
                Correo = correo.Trim()
            };
            _coordinadores.Add(persona);
            GuardarSinBloqueo();
            return persona;
        }
    }

    public CoordinadorPlantilla? ActualizarCoordinador(Guid id, string nombre, string correo)
    {
        lock (_bloqueo)
        {
            var persona = _coordinadores.FirstOrDefault(item => item.Id == id);
            if (persona is null)
            {
                return null;
            }

            if (!EsBackup(persona))
            {
                persona.Nombre = nombre.Trim();
            }

            persona.Correo = correo.Trim();
            GuardarSinBloqueo();
            return persona;
        }
    }

    public bool EliminarCoordinador(Guid id)
    {
        lock (_bloqueo)
        {
            if (EsBackup(_coordinadores.FirstOrDefault(item => item.Id == id))
                || _estudios.Any(item => item.CoordinadorPrincipalId == id || item.CoordinadorApoyoId == id))
            {
                return false;
            }

            var quitado = _coordinadores.RemoveAll(item => item.Id == id) > 0;
            if (quitado)
            {
                GuardarSinBloqueo();
            }

            return quitado;
        }
    }

    public IReadOnlyList<EstudioDePlantillas> Estudios()
    {
        lock (_bloqueo)
        {
            return _estudios.OrderByDescending(item => item.FechaCreacionUtc).ToList();
        }
    }

    public EstudioDePlantillas? BuscarEstudio(Guid id)
    {
        lock (_bloqueo)
        {
            return _estudios.FirstOrDefault(item => item.Id == id);
        }
    }

    public EstudioDePlantillas CrearEstudio(EstudioDePlantillas datos)
    {
        lock (_bloqueo)
        {
            var ahora = DateTime.UtcNow;
            var estudio = Copiar(datos);
            estudio.Id = Guid.NewGuid();
            estudio.FechaCreacionUtc = ahora;
            estudio.FechaEdicionUtc = ahora;
            estudio.PlantillasRevisadas = [];
            _estudios.Add(estudio);
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public EstudioDePlantillas? ActualizarEstudio(Guid id, EstudioDePlantillas datos)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null)
            {
                return null;
            }

            estudio.Nombre = datos.Nombre.Trim();
            estudio.Codigo = datos.Codigo.Trim();
            estudio.Patrocinador = datos.Patrocinador.Trim();
            estudio.InvestigadorPrincipalId = datos.InvestigadorPrincipalId;
            estudio.SubinvestigadorId = datos.SubinvestigadorId;
            estudio.CoordinadorPrincipalId = datos.CoordinadorPrincipalId;
            estudio.CoordinadorApoyoId = IdBackup();
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public bool EliminarEstudio(Guid id)
    {
        lock (_bloqueo)
        {
            var quitado = _estudios.RemoveAll(item => item.Id == id) > 0;
            if (quitado)
            {
                GuardarSinBloqueo();
            }

            return quitado;
        }
    }

    public EstudioDePlantillas? GuardarRevisadas(Guid id, IReadOnlyList<string> codigos)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null)
            {
                return null;
            }

            estudio.PlantillasRevisadas = codigos
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    private EstudioDePlantillas Copiar(EstudioDePlantillas datos) => new()
    {
        Nombre = datos.Nombre.Trim(),
        Codigo = datos.Codigo.Trim(),
        Patrocinador = datos.Patrocinador.Trim(),
        InvestigadorPrincipalId = datos.InvestigadorPrincipalId,
        SubinvestigadorId = datos.SubinvestigadorId,
        CoordinadorPrincipalId = datos.CoordinadorPrincipalId,
        CoordinadorApoyoId = IdBackup()
    };

    private Guid? IdBackup() =>
        _coordinadores.FirstOrDefault(item => EsBackup(item))?.Id;

    private static bool EsBackup(CoordinadorPlantilla? persona) =>
        persona is not null && persona.Nombre.Equals(PlantillasFijas.CoordinadorBackup, StringComparison.OrdinalIgnoreCase);

    private void Cargar()
    {
        if (!File.Exists(_ruta) || !Leer(File.ReadAllText(_ruta)))
        {
            Sembrar();
            GuardarSinBloqueo();
        }
    }

    private bool Leer(string texto)
    {
        try
        {
            using var documento = JsonDocument.Parse(texto);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var datos = JsonSerializer.Deserialize<DocumentoLocal>(texto, Json);
            if (datos is null)
            {
                return false;
            }

            _investigadores.AddRange(datos.Investigadores);
            _coordinadores.AddRange(datos.Coordinadores);
            _estudios.AddRange(datos.Estudios);
            foreach (var estudio in _estudios)
            {
                estudio.PlantillasRevisadas ??= [];
            }

            if (AsegurarBackup())
            {
                GuardarSinBloqueo();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void Sembrar()
    {
        _investigadores.Clear();
        _coordinadores.Clear();
        _estudios.Clear();
        _investigadores.Add(new InvestigadorPlantilla { Id = Guid.NewGuid(), Nombre = "Investigador temporal", Titulo = "Médico general" });
        _investigadores.Add(new InvestigadorPlantilla { Id = Guid.NewGuid(), Nombre = "Subinvestigador temporal", Titulo = "Médico general" });
        AsegurarBackup();
    }

    private bool AsegurarBackup()
    {
        if (_coordinadores.Any(EsBackup))
        {
            return false;
        }

        _coordinadores.Add(new CoordinadorPlantilla
        {
            Id = Guid.NewGuid(),
            Nombre = PlantillasFijas.CoordinadorBackup,
            Correo = ""
        });
        return true;
    }

    private void GuardarSinBloqueo()
    {
        var datos = new DocumentoLocal
        {
            Investigadores = _investigadores,
            Coordinadores = _coordinadores,
            Estudios = _estudios
        };
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(datos, Json));
    }

    private sealed class DocumentoLocal
    {
        public List<InvestigadorPlantilla> Investigadores { get; set; } = [];

        public List<CoordinadorPlantilla> Coordinadores { get; set; } = [];

        public List<EstudioDePlantillas> Estudios { get; set; } = [];
    }
}
