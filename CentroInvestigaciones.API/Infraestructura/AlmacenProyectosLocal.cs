using System.Text.Json;
using CentroInvestigaciones.Application.Proyectos;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenProyectosLocal : IAlmacenProyectos
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();
    private readonly List<ProyectoSeguimiento> _proyectos = [];
    private readonly List<FaseProyecto> _fases = [];
    private readonly List<ActividadProyecto> _actividades = [];
    private readonly List<InvestigadorProyecto> _investigadores = [];
    private readonly List<AsignacionActividad> _asignaciones = [];
    private readonly List<ProrrogaActividad> _prorrogas = [];

    public AlmacenProyectosLocal(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "proyectos.json");
        Cargar();
    }

    public IReadOnlyList<ProyectoSeguimiento> Listar(Guid responsableId)
    {
        lock (_bloqueo)
        {
            return _proyectos
                .Where(item => item.ResponsableId == responsableId)
                .OrderByDescending(item => item.CreadoEl)
                .ThenBy(item => item.Nombre)
                .ToList();
        }
    }

    public ProyectoSeguimiento Crear(ProyectoSeguimiento proyecto, IReadOnlyList<FaseProyecto> fases)
    {
        lock (_bloqueo)
        {
            ValidarFases(fases);
            proyecto.Id = Guid.NewGuid();
            proyecto.Nombre = proyecto.Nombre.Trim();
            proyecto.Semillero = proyecto.Semillero.Trim();
            proyecto.Estado = "Activo";
            proyecto.CreadoEl = DateTimeOffset.Now;
            _proyectos.Add(proyecto);
            AgregarFases(proyecto.Id, fases);
            Guardar();
            return proyecto;
        }
    }

    public ProyectoSeguimiento Actualizar(Guid id, string nombre, string semillero)
    {
        lock (_bloqueo)
        {
            var proyecto = Buscar(id);
            var limpio = nombre.Trim();
            if (limpio.Length == 0)
            {
                throw new InvalidOperationException("Ingresa el nombre del proyecto.");
            }

            proyecto.Nombre = limpio;
            proyecto.Semillero = semillero.Trim();
            Guardar();
            return proyecto;
        }
    }

    public void ReemplazarFases(Guid proyectoId, IReadOnlyList<FaseProyecto> fases)
    {
        lock (_bloqueo)
        {
            Buscar(proyectoId);
            ValidarFases(fases);
            var propias = _fases.Where(item => item.ProyectoId == proyectoId).Select(item => item.Id).ToHashSet();
            foreach (var fase in fases)
            {
                if (fase.Id != Guid.Empty && !propias.Contains(fase.Id))
                {
                    throw new InvalidOperationException("La fase no pertenece al proyecto.");
                }
            }

            var conservadas = fases.Where(item => item.Id != Guid.Empty).Select(item => item.Id).ToHashSet();
            foreach (var id in propias.Where(item => !conservadas.Contains(item)))
            {
                QuitarFase(id);
            }

            var orden = 1;
            foreach (var fase in fases)
            {
                fase.Nombre = fase.Nombre.Trim();
                fase.Orden = orden++;
                fase.ProyectoId = proyectoId;
                if (fase.Id == Guid.Empty)
                {
                    fase.Id = Guid.NewGuid();
                    _fases.Add(fase);
                    continue;
                }

                var actual = _fases.First(item => item.Id == fase.Id);
                actual.Nombre = fase.Nombre;
                actual.Peso = fase.Peso;
                actual.Orden = fase.Orden;
            }

            Guardar();
        }
    }

    public void GuardarActividades(Guid proyectoId, IReadOnlyList<ActividadProyecto> actividades)
    {
        lock (_bloqueo)
        {
            Buscar(proyectoId);
            var fases = _fases.Where(item => item.ProyectoId == proyectoId).Select(item => item.Id).ToHashSet();
            foreach (var actividad in actividades)
            {
                ValidarActividad(actividad);
                if (!fases.Contains(actividad.FaseId))
                {
                    throw new InvalidOperationException("La fase no pertenece al proyecto.");
                }
            }

            var propias = _actividades.Where(item => fases.Contains(item.FaseId)).Select(item => item.Id).ToHashSet();
            var conservadas = actividades.Where(item => item.Id != Guid.Empty).Select(item => item.Id).ToHashSet();
            foreach (var id in conservadas)
            {
                if (!propias.Contains(id))
                {
                    throw new InvalidOperationException("La actividad no pertenece al proyecto.");
                }
            }

            foreach (var id in propias.Where(item => !conservadas.Contains(item)))
            {
                QuitarActividad(id);
            }

            foreach (var actividad in actividades)
            {
                actividad.Descripcion = actividad.Descripcion.Trim();
                if (actividad.Id == Guid.Empty)
                {
                    actividad.Id = Guid.NewGuid();
                    actividad.Estado = EstadosActividad.EnProgreso;
                    _actividades.Add(actividad);
                    continue;
                }

                var actual = _actividades.First(item => item.Id == actividad.Id);
                actual.FaseId = actividad.FaseId;
                actual.Descripcion = actividad.Descripcion;
                actual.FechaInicio = actividad.FechaInicio;
                actual.FechaLimite = actividad.FechaLimite;
                actual.Peso = actividad.Peso;
            }

            Guardar();
        }
    }

    public void CambiarEstado(Guid actividadId, string estado)
    {
        lock (_bloqueo)
        {
            if (!EstadosActividad.Valido(estado))
            {
                throw new InvalidOperationException("El estado no es válido.");
            }

            var actividad = _actividades.FirstOrDefault(item => item.Id == actividadId)
                ?? throw new InvalidOperationException("No se encontró la actividad.");
            actividad.Estado = estado;
            foreach (var asignacion in _asignaciones.Where(item => item.ActividadId == actividadId))
            {
                asignacion.Estado = estado;
            }

            Guardar();
        }
    }

    public InvestigadorProyecto CrearInvestigador(string nombre)
    {
        lock (_bloqueo)
        {
            var limpio = nombre.Trim();
            if (limpio.Length == 0)
            {
                throw new InvalidOperationException("Ingresa el nombre del investigador.");
            }

            var investigador = new InvestigadorProyecto { Id = Guid.NewGuid(), Nombre = limpio };
            _investigadores.Add(investigador);
            Guardar();
            return investigador;
        }
    }

    public void Asignar(Guid actividadId, Guid investigadorId)
    {
        lock (_bloqueo)
        {
            var actividad = _actividades.FirstOrDefault(item => item.Id == actividadId)
                ?? throw new InvalidOperationException("No se encontró la actividad.");
            if (_investigadores.All(item => item.Id != investigadorId))
            {
                throw new InvalidOperationException("No se encontró el investigador.");
            }

            var actuales = _asignaciones.Where(item => item.ActividadId == actividadId).ToList();
            if (actuales.Any(item => item.InvestigadorId == investigadorId))
            {
                throw new InvalidOperationException("Este investigador ya está asignado a esta actividad.");
            }

            actuales.Add(new AsignacionActividad
            {
                Id = Guid.NewGuid(),
                ActividadId = actividadId,
                InvestigadorId = investigadorId,
                Estado = actividad.Estado
            });
            _asignaciones.Add(actuales[^1]);
            Repartir(actuales);
            Guardar();
        }
    }

    public void QuitarAsignacion(Guid actividadId, Guid investigadorId)
    {
        lock (_bloqueo)
        {
            var quitada = _asignaciones.FirstOrDefault(item => item.ActividadId == actividadId && item.InvestigadorId == investigadorId)
                ?? throw new InvalidOperationException("No se encontró la asignación.");
            _asignaciones.Remove(quitada);
            Repartir(_asignaciones.Where(item => item.ActividadId == actividadId).ToList());
            Guardar();
        }
    }

    public void Prorrogar(Guid actividadId, string nuevaFecha, IReadOnlyList<Guid> investigadores)
    {
        lock (_bloqueo)
        {
            var actividad = _actividades.FirstOrDefault(item => item.Id == actividadId)
                ?? throw new InvalidOperationException("No se encontró la actividad.");
            if (!FechaValida(nuevaFecha))
            {
                throw new InvalidOperationException("Selecciona una nueva fecha.");
            }

            var elegidos = investigadores.Where(item => item != Guid.Empty).Distinct().ToList();
            var asignados = _asignaciones.Where(item => item.ActividadId == actividadId).Select(item => item.InvestigadorId).ToHashSet();
            if (elegidos.Any(item => !asignados.Contains(item)))
            {
                throw new InvalidOperationException("El responsable de la prórroga no está asignado a la actividad.");
            }

            var anterior = actividad.FechaLimite;
            if (elegidos.Count == 0)
            {
                _prorrogas.Add(new ProrrogaActividad
                {
                    Id = Guid.NewGuid(),
                    ActividadId = actividadId,
                    FechaAnterior = anterior
                });
            }
            else
            {
                foreach (var investigador in elegidos)
                {
                    _prorrogas.Add(new ProrrogaActividad
                    {
                        Id = Guid.NewGuid(),
                        ActividadId = actividadId,
                        InvestigadorId = investigador,
                        FechaAnterior = anterior
                    });
                }
            }

            actividad.FechaLimite = nuevaFecha;
            Guardar();
        }
    }

    public TableroProyecto? Tablero(Guid proyectoId)
    {
        lock (_bloqueo)
        {
            var proyecto = _proyectos.FirstOrDefault(item => item.Id == proyectoId);
            if (proyecto is null)
            {
                return null;
            }

            var fases = _fases.Where(item => item.ProyectoId == proyectoId).OrderBy(item => item.Orden).ToList();
            var tablero = new TableroProyecto
            {
                Id = proyecto.Id,
                Nombre = proyecto.Nombre,
                Semillero = proyecto.Semillero,
                ResponsableId = proyecto.ResponsableId,
                Investigadores = _investigadores.OrderBy(item => item.Nombre).ToList()
            };

            foreach (var fase in fases)
            {
                var actividades = _actividades.Where(item => item.FaseId == fase.Id).ToList();
                var progreso = ProgresoFase(actividades);
                tablero.Fases.Add(new TableroFase
                {
                    Id = fase.Id,
                    Nombre = fase.Nombre,
                    Orden = fase.Orden,
                    Peso = fase.Peso,
                    Progreso = progreso,
                    Investigadores = Avances(actividades),
                    Actividades = actividades.Select(Mapa).ToList()
                });
            }

            var total = tablero.Fases.Sum(item => item.Peso);
            tablero.Progreso = total == 0
                ? 0
                : tablero.Fases.Sum(item => item.Progreso / 100m * item.Peso) * 100m / total;
            return tablero;
        }
    }

    private ProyectoSeguimiento Buscar(Guid id) =>
        _proyectos.FirstOrDefault(item => item.Id == id)
        ?? throw new InvalidOperationException("No se encontró el proyecto.");

    private void AgregarFases(Guid proyectoId, IReadOnlyList<FaseProyecto> fases)
    {
        var orden = 1;
        foreach (var fase in fases)
        {
            fase.Id = Guid.NewGuid();
            fase.ProyectoId = proyectoId;
            fase.Nombre = fase.Nombre.Trim();
            fase.Orden = orden++;
            _fases.Add(fase);
        }
    }

    private void QuitarFase(Guid faseId)
    {
        foreach (var actividad in _actividades.Where(item => item.FaseId == faseId).Select(item => item.Id).ToList())
        {
            QuitarActividad(actividad);
        }

        _fases.RemoveAll(item => item.Id == faseId);
    }

    private void QuitarActividad(Guid actividadId)
    {
        _asignaciones.RemoveAll(item => item.ActividadId == actividadId);
        _prorrogas.RemoveAll(item => item.ActividadId == actividadId);
        _actividades.RemoveAll(item => item.Id == actividadId);
    }

    private TableroActividad Mapa(ActividadProyecto actividad)
    {
        var prorrogas = _prorrogas.Where(item => item.ActividadId == actividad.Id).ToList();
        return new TableroActividad
        {
            Id = actividad.Id,
            Descripcion = actividad.Descripcion,
            FechaInicio = actividad.FechaInicio,
            FechaLimite = actividad.FechaLimite,
            FechasAnteriores = prorrogas
                .Select(item => item.FechaAnterior)
                .Where(item => item.Length > 0)
                .Distinct()
                .ToList(),
            Peso = actividad.Peso,
            Estado = actividad.Estado,
            Asignados = _asignaciones
                .Where(item => item.ActividadId == actividad.Id)
                .Select(item => new AsignadoActividad
                {
                    Id = item.InvestigadorId,
                    Nombre = _investigadores.FirstOrDefault(investigador => investigador.Id == item.InvestigadorId)?.Nombre ?? "",
                    Peso = item.Peso,
                    Prorrogas = prorrogas.Count(prorroga => prorroga.InvestigadorId == item.InvestigadorId)
                })
                .ToList()
        };
    }

    private List<AvanceInvestigador> Avances(IReadOnlyList<ActividadProyecto> actividades)
    {
        var mapa = new Dictionary<string, (int Total, int Hecho)>(StringComparer.OrdinalIgnoreCase);
        foreach (var actividad in actividades)
        {
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asignacion in _asignaciones.Where(item => item.ActividadId == actividad.Id))
            {
                var nombre = _investigadores.FirstOrDefault(item => item.Id == asignacion.InvestigadorId)?.Nombre ?? "";
                if (nombre.Length == 0 || !vistos.Add(nombre))
                {
                    continue;
                }

                var actual = mapa.GetValueOrDefault(nombre);
                actual.Total += actividad.Peso;
                if (actividad.Estado == EstadosActividad.Completado)
                {
                    actual.Hecho += actividad.Peso;
                }

                mapa[nombre] = actual;
            }
        }

        return mapa
            .OrderBy(item => item.Key)
            .Select(item => new AvanceInvestigador
            {
                Nombre = item.Key,
                Porcentaje = item.Value.Total == 0 ? 0 : item.Value.Hecho * 100m / item.Value.Total
            })
            .ToList();
    }

    private static decimal ProgresoFase(IReadOnlyList<ActividadProyecto> actividades)
    {
        var total = actividades.Sum(item => item.Peso);
        if (total == 0)
        {
            return 0;
        }

        var hecho = actividades.Where(item => item.Estado == EstadosActividad.Completado).Sum(item => item.Peso);
        return hecho * 100m / total;
    }

    private static void Repartir(IReadOnlyList<AsignacionActividad> asignaciones)
    {
        if (asignaciones.Count == 0)
        {
            return;
        }

        var basePeso = 100 / asignaciones.Count;
        var ultimo = 100 - basePeso * (asignaciones.Count - 1);
        for (var i = 0; i < asignaciones.Count; i++)
        {
            asignaciones[i].Peso = i == asignaciones.Count - 1 ? ultimo : basePeso;
        }
    }

    private static void ValidarFases(IReadOnlyList<FaseProyecto> fases)
    {
        if (fases.Count == 0)
        {
            throw new InvalidOperationException("El proyecto necesita al menos una fase.");
        }

        if (fases.Any(item => string.IsNullOrWhiteSpace(item.Nombre)))
        {
            throw new InvalidOperationException("Ingresa el nombre de cada fase.");
        }

        if (fases.Any(item => item.Peso < 1))
        {
            throw new InvalidOperationException("Cada fase necesita un peso válido.");
        }

        if (fases.Sum(item => item.Peso) != 100)
        {
            throw new InvalidOperationException($"Los pesos deben sumar 100%. Actualmente: {fases.Sum(item => item.Peso)}%.");
        }
    }

    private static void ValidarActividad(ActividadProyecto actividad)
    {
        var descripcion = actividad.Descripcion.Trim();
        if (descripcion.Length == 0)
        {
            throw new InvalidOperationException("Hay actividades sin descripción.");
        }

        if (!FechaValida(actividad.FechaInicio) || !FechaValida(actividad.FechaLimite))
        {
            throw new InvalidOperationException($"Faltan fechas en \"{descripcion}\".");
        }

        if (string.CompareOrdinal(actividad.FechaInicio, actividad.FechaLimite) > 0)
        {
            throw new InvalidOperationException($"Fecha inicio posterior a límite en \"{descripcion}\".");
        }

        if (actividad.Peso < 1)
        {
            throw new InvalidOperationException($"Peso inválido en \"{descripcion}\".");
        }
    }

    private static bool FechaValida(string valor) => DateOnly.TryParse(valor, out _);

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            return;
        }

        var foto = JsonSerializer.Deserialize<Foto>(File.ReadAllText(_ruta), Json) ?? new Foto();
        _proyectos.AddRange(foto.Proyectos);
        _fases.AddRange(foto.Fases);
        _actividades.AddRange(foto.Actividades);
        _investigadores.AddRange(foto.Investigadores);
        _asignaciones.AddRange(foto.Asignaciones);
        _prorrogas.AddRange(foto.Prorrogas);
    }

    private void Guardar()
    {
        var foto = new Foto
        {
            Proyectos = _proyectos,
            Fases = _fases,
            Actividades = _actividades,
            Investigadores = _investigadores,
            Asignaciones = _asignaciones,
            Prorrogas = _prorrogas
        };
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(foto, Json));
    }

    private sealed class Foto
    {
        public List<ProyectoSeguimiento> Proyectos { get; set; } = [];

        public List<FaseProyecto> Fases { get; set; } = [];

        public List<ActividadProyecto> Actividades { get; set; } = [];

        public List<InvestigadorProyecto> Investigadores { get; set; } = [];

        public List<AsignacionActividad> Asignaciones { get; set; } = [];

        public List<ProrrogaActividad> Prorrogas { get; set; } = [];
    }
}
