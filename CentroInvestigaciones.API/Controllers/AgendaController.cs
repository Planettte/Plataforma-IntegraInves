using System.Security.Claims;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Agenda;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/agenda")]
public sealed class AgendaController : ControllerBase
{
    private readonly IAlmacenAgenda _agenda;
    private readonly IAlmacenNucleo _nucleo;
    private readonly AlmacenNotificaciones _notificaciones;

    public AgendaController(IAlmacenAgenda agenda, IAlmacenNucleo nucleo, AlmacenNotificaciones notificaciones)
    {
        _agenda = agenda;
        _nucleo = nucleo;
        _notificaciones = notificaciones;
    }

    [HttpGet("{ambito}")]
    public async Task<ActionResult<DiaAgendaDto>> Dia(string ambito, [FromQuery] string fecha, [FromQuery] bool compartida, [FromQuery] bool asignadas, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor is null || !Permiso(actor, ambito))
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(fecha, out var dia))
        {
            return BadRequest("Indica la fecha.");
        }

        var asigna = PuedeAsignar(actor, ambito);
        var verAsignadas = asignadas && asigna && ambito == "estudios";
        var verCompartida = compartida && asigna && ambito == "estudios" && !verAsignadas;
        var personas = await PersonasAsync(ambito, asigna, actor.Usuario, ct);
        var propia = Propia(personas, actor, ambito);
        var fuente = verAsignadas
            ? _agenda.Citas().Where(cita => cita.Agenda is ReglasAgenda.Medicos or ReglasAgenda.Coordinacion)
            : verCompartida
                ? ConsultaAgenda.Medicos(_agenda.Citas())
                : CitasDe(propia);
        var festivos = _agenda.Festivos();
        return new DiaAgendaDto
        {
            Fecha = dia.ToString("yyyy-MM-dd"),
            Festivo = festivos.Contains(dia),
            PuedeAsignar = asigna,
            PuedeMonitorias = ambito == "estudios",
            AgendaPropia = propia.Agenda,
            Persona = propia.Nombre,
            UsuarioId = propia.UsuarioId,
            Personas = asigna ? personas : [propia],
            Citas = fuente.Where(item => item.Fecha == dia).OrderBy(item => item.HoraInicio).Select(cita => Mapa(cita, actor)).ToList(),
            Monitorias = ambito == "estudios"
                ? _agenda.Monitorias().Where(item => item.Fecha == dia).Select(item => Mapa(item, actor)).ToList()
                : [],
            FestivosMes = festivos.Where(item => item.Year == dia.Year && item.Month == dia.Month).Select(item => item.ToString("yyyy-MM-dd")).ToList(),
            DiasOcupados = fuente
                .Where(item => item.Fecha.Year == dia.Year && item.Fecha.Month == dia.Month)
                .Select(item => item.Fecha.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList(),
            DiasMonitorias = ambito == "estudios"
                ? _agenda.Monitorias()
                    .Where(item => item.Fecha.Year == dia.Year && item.Fecha.Month == dia.Month)
                    .Select(item => item.Fecha.ToString("yyyy-MM-dd"))
                    .Distinct()
                    .ToList()
                : [],
            Importantes = CitasDe(propia)
                .Where(item => item.Importante)
                .OrderBy(item => item.Fecha)
                .ThenBy(item => item.HoraInicio)
                .Select(cita => Mapa(cita, actor))
                .ToList()
        };
    }

    [HttpGet("{ambito:regex(^(estudios|udd)$)}/buscar")]
    public async Task<ActionResult<List<CitaAgendaDto>>> Buscar(string ambito, [FromQuery] string? texto, [FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] bool compartida, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor is null || !Permiso(actor, ambito))
        {
            return Forbid();
        }

        if (!Rango(desde, hasta, out var inicio, out var fin, out var error))
        {
            return BadRequest(error);
        }

        var asigna = PuedeAsignar(actor, ambito);
        var personas = await PersonasAsync(ambito, asigna, actor.Usuario, ct);
        var propia = Propia(personas, actor, ambito);
        var fuente = compartida && asigna && ambito == "estudios"
            ? ConsultaAgenda.Medicos(_agenda.Citas())
            : CitasDe(propia);
        return ConsultaAgenda.Buscar(fuente, texto, inicio, fin).Select(cita => Mapa(cita, actor)).ToList();
    }

    [HttpGet("publico/dia")]
    public async Task<ActionResult<DiaAgendaDto>> Publico([FromQuery] string fecha, CancellationToken ct)
    {
        if (await ActorAsync(ct) is not Actor actor)
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(fecha, out var dia))
        {
            return BadRequest("Indica la fecha.");
        }

        var medicos = _agenda.Citas().Where(item => item.Agenda == ReglasAgenda.Medicos);
        var festivos = _agenda.Festivos();
        return new DiaAgendaDto
        {
            Fecha = dia.ToString("yyyy-MM-dd"),
            Festivo = festivos.Contains(dia),
            PuedeMonitorias = true,
            AgendaPropia = ReglasAgenda.Medicos,
            Persona = "Médicos",
            Citas = medicos.Where(item => item.Fecha == dia).OrderBy(item => item.HoraInicio).Select(MapaPublico).ToList(),
            Monitorias = _agenda.Monitorias().Where(item => item.Fecha == dia).Select(item => Mapa(item, actor)).ToList(),
            FestivosMes = festivos.Where(item => item.Year == dia.Year && item.Month == dia.Month).Select(item => item.ToString("yyyy-MM-dd")).ToList(),
            DiasOcupados = medicos
                .Where(item => item.Fecha.Year == dia.Year && item.Fecha.Month == dia.Month)
                .Select(item => item.Fecha.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList(),
            DiasMonitorias = _agenda.Monitorias()
                .Where(item => item.Fecha.Year == dia.Year && item.Fecha.Month == dia.Month)
                .Select(item => item.Fecha.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList()
        };
    }

    [HttpGet("publico/buscar")]
    public async Task<ActionResult<List<CitaAgendaDto>>> BuscarPublico([FromQuery] string? texto, [FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken ct)
    {
        if (await ActorAsync(ct) is null)
        {
            return Forbid();
        }

        if (!Rango(desde, hasta, out var inicio, out var fin, out var error))
        {
            return BadRequest(error);
        }

        var medicos = _agenda.Citas().Where(item => item.Agenda == ReglasAgenda.Medicos);
        return ConsultaAgenda.Buscar(medicos, texto, inicio, fin).Select(MapaPublico).ToList();
    }

    [HttpPost("{ambito}/citas")]
    public Task<ActionResult<CitaAgendaDto>> Crear(string ambito, [FromBody] GuardarCitaAgendaDto solicitud, CancellationToken ct) =>
        Guardar(ambito, null, solicitud, ct);

    [HttpPut("{ambito}/citas/{id:guid}")]
    public Task<ActionResult<CitaAgendaDto>> Actualizar(string ambito, Guid id, [FromBody] GuardarCitaAgendaDto solicitud, CancellationToken ct) =>
        Guardar(ambito, id, solicitud, ct);

    [HttpDelete("{ambito}/citas/{id:guid}")]
    public async Task<IActionResult> Eliminar(string ambito, Guid id, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var cita = _agenda.Citas().FirstOrDefault(item => item.Id == id);
        if (actor is null || cita is null || !Permiso(actor, ambito) || !AgendaDelAmbito(ambito, cita.Agenda) || !EsCreador(actor, cita))
        {
            return Forbid();
        }

        _agenda.EliminarCita(id);
        return NoContent();
    }

    [HttpPost("{ambito}/monitorias")]
    public async Task<ActionResult<MonitoriaAgendaDto>> GuardarMonitoria(string ambito, [FromBody] GuardarMonitoriaAgendaDto solicitud, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor is null || ambito != "estudios" || !Permiso(actor, ambito))
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(solicitud.Fecha, out var fecha) || string.IsNullOrWhiteSpace(solicitud.Descripcion))
        {
            return BadRequest("Indica la fecha y la descripción.");
        }

        if (_agenda.Festivos().Contains(fecha))
        {
            return BadRequest("Ese día es festivo.");
        }

        var id = solicitud.Id ?? Guid.NewGuid();
        var existente = solicitud.Id is null ? null : _agenda.Monitorias().FirstOrDefault(item => item.Id == id);
        if (solicitud.Id is not null && (existente is null || !EsCreador(actor, existente)))
        {
            return Forbid();
        }

        var delDia = _agenda.Monitorias().Where(item => item.Fecha == fecha && item.Id != id).ToList();
        if (solicitud.Id is null && delDia.Count >= 2)
        {
            return BadRequest("Ya hay 2 monitorías registradas ese día.");
        }

        var monitoria = new MonitoriaAgenda
        {
            Id = id,
            Fecha = fecha,
            Descripcion = solicitud.Descripcion.Trim(),
            CreadoPorPersona = existente?.CreadoPorPersona is { Length: > 0 } nombre ? nombre : actor.Usuario.Nombre,
            CreadoPorCorreo = existente?.CreadoPorCorreo is { Length: > 0 } correo ? correo : actor.Usuario.CorreoElectronico
        };
        _agenda.GuardarMonitoria(monitoria);
        return Mapa(monitoria, actor);
    }

    [HttpDelete("{ambito}/monitorias/{id:guid}")]
    public async Task<IActionResult> EliminarMonitoria(string ambito, Guid id, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var monitoria = _agenda.Monitorias().FirstOrDefault(item => item.Id == id);
        if (actor is null || monitoria is null || ambito != "estudios" || !Permiso(actor, ambito) || !EsCreador(actor, monitoria))
        {
            return Forbid();
        }

        return _agenda.EliminarMonitoria(id) ? NoContent() : NotFound();
    }

    [HttpPut("{ambito}/citas/{id:guid}/importante")]
    public async Task<IActionResult> Importante(string ambito, Guid id, [FromBody] MarcarImportanteDto solicitud, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var cita = _agenda.Citas().FirstOrDefault(item => item.Id == id);
        if (actor is null || cita is null || !Permiso(actor, ambito) || !AgendaDelAmbito(ambito, cita.Agenda) || !EsCreador(actor, cita))
        {
            return Forbid();
        }

        cita.Importante = solicitud.Importante;
        _agenda.GuardarCita(cita);
        return NoContent();
    }

    private async Task<ActionResult<CitaAgendaDto>> Guardar(string ambito, Guid? id, GuardarCitaAgendaDto solicitud, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor is null || !Permiso(actor, ambito))
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(solicitud.Fecha, out var fecha))
        {
            return BadRequest("Indica la fecha.");
        }

        if (_agenda.Festivos().Contains(fecha))
        {
            return BadRequest("Ese día es festivo.");
        }

        if (!ReglasAgenda.HoraValida(solicitud.HoraInicio, solicitud.HoraFin))
        {
            return BadRequest("El horario va de 6:00 a 20:00, en bloques de 30 minutos.");
        }

        var existente = id is null ? null : _agenda.Citas().FirstOrDefault(item => item.Id == id);
        if (id is not null && (existente is null || !AgendaDelAmbito(ambito, existente.Agenda) || !EsCreador(actor, existente)))
        {
            return Forbid();
        }

        var asigna = PuedeAsignar(actor, ambito);
        var agenda = asigna ? (solicitud.Agenda ?? "").Trim().ToLowerInvariant() : AgendaPropia(actor, ambito);
        if (!AgendaDelAmbito(ambito, agenda))
        {
            return BadRequest("Esa agenda no corresponde a esta pantalla.");
        }

        var tipo = ambito == "udd" ? ReglasAgenda.Citas : (solicitud.Tipo ?? "").Trim().ToLowerInvariant();
        if (tipo is not (ReglasAgenda.Citas or ReglasAgenda.Otros))
        {
            return BadRequest("El tipo de registro no es válido.");
        }

        var personas = await PersonasAsync(ambito, true, actor.Usuario, ct);
        PersonaAgendaDto destino;
        if (!asigna)
        {
            destino = new PersonaAgendaDto { UsuarioId = actor.Usuario.Id, Nombre = actor.Usuario.Nombre, Agenda = agenda };
        }
        else if (solicitud.UsuarioId is Guid usuarioId)
        {
            destino = personas.FirstOrDefault(item => item.UsuarioId == usuarioId && item.Agenda == agenda)
                ?? new PersonaAgendaDto();
            if (destino.UsuarioId is null)
            {
                return BadRequest("Esa persona no está en esta agenda.");
            }
        }
        else
        {
            var nombre = solicitud.Persona?.Trim() ?? "";
            if (nombre.Length == 0)
            {
                return BadRequest("Indica la persona.");
            }

            destino = personas.FirstOrDefault(item => item.Agenda == agenda && string.Equals(item.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
                ?? new PersonaAgendaDto();
            if (string.IsNullOrWhiteSpace(destino.Nombre))
            {
                return BadRequest("Esa persona no está en esta agenda.");
            }
        }

        var cita = new CitaAgenda
        {
            Id = id ?? Guid.NewGuid(),
            Agenda = agenda,
            Fecha = fecha,
            Persona = destino.Nombre,
            UsuarioId = destino.UsuarioId,
            HoraInicio = solicitud.HoraInicio.Trim(),
            HoraFin = solicitud.HoraFin.Trim(),
            Tipo = tipo,
            Importante = solicitud.Importante,
            Campos = (solicitud.Campos ?? []).ToDictionary(item => item.Key, item => item.Value?.Trim() ?? "", StringComparer.OrdinalIgnoreCase),
            CreadoPorPersona = existente?.CreadoPorPersona is { Length: > 0 } nombreCreador ? nombreCreador : actor.Usuario.Nombre,
            CreadoPorCorreo = existente?.CreadoPorCorreo is { Length: > 0 } correo ? correo : actor.Usuario.CorreoElectronico
        };
        _agenda.GuardarCita(cita);
        if (id is null && destino.UsuarioId is Guid avisado && avisado != actor.Usuario.Id)
        {
            var titulo = PrimerTexto(cita.Campos);
            _notificaciones.Agregar(
                avisado,
                "Cita en la agenda",
                $"Te añadieron un registro el {fecha:dd/MM/yyyy} a las {cita.HoraInicio}: {titulo}. Lo agendó {actor.Usuario.Nombre}.",
                ambito == "udd" ? "/udd/agenda" : "/estudios-clinicos/agenda");
        }

        return Mapa(cita);
    }

    private IEnumerable<CitaAgenda> CitasDe(PersonaAgendaDto persona) =>
        _agenda.Citas().Where(item => ConsultaAgenda.EsDe(item, persona.Agenda, persona.UsuarioId, persona.Nombre));

    private static PersonaAgendaDto Propia(List<PersonaAgendaDto> personas, Actor actor, string ambito) =>
        personas.FirstOrDefault(item => item.UsuarioId == actor.Usuario.Id)
        ?? new PersonaAgendaDto { UsuarioId = actor.Usuario.Id, Nombre = actor.Usuario.Nombre, Agenda = AgendaPropia(actor, ambito) };

    private static bool Rango(string? desde, string? hasta, out DateOnly? inicio, out DateOnly? fin, out string error)
    {
        inicio = null;
        fin = null;
        error = "";
        if (!string.IsNullOrWhiteSpace(desde))
        {
            if (!DateOnly.TryParse(desde, out var fechaDesde))
            {
                error = "La fecha inicial no es válida.";
                return false;
            }

            inicio = fechaDesde;
        }

        if (!string.IsNullOrWhiteSpace(hasta))
        {
            if (!DateOnly.TryParse(hasta, out var fechaHasta))
            {
                error = "La fecha final no es válida.";
                return false;
            }

            fin = fechaHasta;
        }

        if (inicio is DateOnly desdeOk && fin is DateOnly hastaOk && hastaOk < desdeOk)
        {
            error = "La fecha final es anterior a la inicial.";
            return false;
        }

        return true;
    }

    private async Task<List<PersonaAgendaDto>> PersonasAsync(string ambito, bool asigna, Usuario actor, CancellationToken ct)
    {
        var propias = new List<PersonaAgendaDto>();
        if (!asigna)
        {
            propias.Add(new PersonaAgendaDto
            {
                UsuarioId = actor.Id,
                Nombre = actor.Nombre,
                Agenda = AgendaPropia(new Actor(actor, []), ambito)
            });
            return propias;
        }

        var (usuarios, _) = await _nucleo.ListarUsuariosAsync(0, 500, null, nameof(EstadoCuenta.Activo), ct);
        foreach (var usuario in usuarios)
        {
            var area = AccesoAgenda.Normalizar(usuario.AreaInteres?.Codigo);
            var cargo = AccesoAgenda.Normalizar(usuario.Cargo);
            if (ambito == "estudios" && area == AccesoAgenda.AreaEstudios && cargo is AccesoAgenda.Medico or AccesoAgenda.Coordinacion)
            {
                propias.Add(new PersonaAgendaDto
                {
                    UsuarioId = usuario.Id,
                    Nombre = usuario.Nombre,
                    Agenda = cargo == AccesoAgenda.Medico ? ReglasAgenda.Medicos : ReglasAgenda.Coordinacion
                });
            }
            else if (ambito == "udd" && area == AccesoAgenda.AreaUdd)
            {
                propias.Add(new PersonaAgendaDto { UsuarioId = usuario.Id, Nombre = usuario.Nombre, Agenda = ReglasAgenda.Diseno });
            }
        }

        var agendaActor = AgendaPropia(new Actor(actor, []), ambito);
        if (!propias.Any(item => item.UsuarioId == actor.Id && item.Agenda == agendaActor))
        {
            propias.Add(new PersonaAgendaDto
            {
                UsuarioId = actor.Id,
                Nombre = actor.Nombre,
                Agenda = agendaActor
            });
        }

        foreach (var cita in _agenda.Citas().Where(item => AgendaDelAmbito(ambito, item.Agenda)))
        {
            if (propias.Any(item => item.Agenda == cita.Agenda && string.Equals(item.Nombre, cita.Persona, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            propias.Add(new PersonaAgendaDto { Nombre = cita.Persona, Agenda = cita.Agenda });
        }

        return propias
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .OrderBy(item => item.Nombre)
            .ToList();
    }

    private static bool EsCreador(Actor actor, MonitoriaAgenda monitoria)
    {
        var correo = actor.Usuario.CorreoElectronico.Trim();
        var correoCreador = monitoria.CreadoPorCorreo.Trim();
        return correoCreador.Length > 0
            && correo.Length > 0
            && string.Equals(correoCreador, correo, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsCreador(Actor actor, CitaAgenda cita)
    {
        var correo = actor.Usuario.CorreoElectronico.Trim();
        var correoCreador = cita.CreadoPorCorreo.Trim();
        return correoCreador.Length > 0
            && correo.Length > 0
            && string.Equals(correoCreador, correo, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PuedeAsignar(Actor actor, string ambito)
    {
        if (actor.Permisos.Contains(AccesoAgenda.PermisoEstudios, StringComparer.Ordinal)
            && actor.Permisos.Contains(AccesoAgenda.PermisoUdd, StringComparer.Ordinal))
        {
            return true;
        }

        return ambito == "estudios"
            && AccesoAgenda.Normalizar(actor.Usuario.Cargo) == AccesoAgenda.Coordinacion;
    }

    private static string AgendaPropia(Actor actor, string ambito)
    {
        if (ambito == "udd")
        {
            return ReglasAgenda.Diseno;
        }

        return AccesoAgenda.Normalizar(actor.Usuario.Cargo) == AccesoAgenda.Coordinacion
            ? ReglasAgenda.Coordinacion
            : ReglasAgenda.Medicos;
    }

    private static bool Permiso(Actor actor, string ambito)
    {
        var codigo = ambito == "udd" ? AccesoAgenda.PermisoUdd : ambito == "estudios" ? AccesoAgenda.PermisoEstudios : "";
        return codigo.Length > 0 && actor.Permisos.Contains(codigo, StringComparer.Ordinal);
    }

    private static bool AgendaDelAmbito(string ambito, string agenda) =>
        ambito == "estudios" ? ReglasAgenda.AgendaEstudios(agenda) : ambito == "udd" && agenda == ReglasAgenda.Diseno;

    private static string PrimerTexto(Dictionary<string, string> campos)
    {
        foreach (var clave in new[] { "nombrePaciente", "codigoPaciente", "estudio", "actividad", "paciente", "participante" })
        {
            if (campos.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor))
            {
                return valor.Trim();
            }
        }

        return "Nuevo registro";
    }

    private static readonly string[] CamposPublicos =
    [
        "estudio", "nombrePaciente", "codigoPaciente", "paciente", "visita", "actividad", "participante"
    ];

    private static CitaAgendaDto MapaPublico(CitaAgenda cita) => new()
    {
        Agenda = cita.Agenda,
        Fecha = cita.Fecha.ToString("yyyy-MM-dd"),
        Persona = cita.Persona,
        HoraInicio = cita.HoraInicio,
        HoraFin = cita.HoraFin,
        Tipo = cita.Tipo,
        Campos = cita.Campos
            .Where(par => CamposPublicos.Contains(par.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(par => par.Key, par => par.Value, StringComparer.OrdinalIgnoreCase)
    };

    private static CitaAgendaDto Mapa(CitaAgenda cita, Actor? actor = null) => new()
    {
        Id = cita.Id,
        Agenda = cita.Agenda,
        Fecha = cita.Fecha.ToString("yyyy-MM-dd"),
        Persona = cita.Persona,
        UsuarioId = cita.UsuarioId,
        HoraInicio = cita.HoraInicio,
        HoraFin = cita.HoraFin,
        Tipo = cita.Tipo,
        Importante = cita.Importante,
        Campos = cita.Campos,
        CreadoPorPersona = cita.CreadoPorPersona,
        PuedeEditar = actor is not null && EsCreador(actor, cita)
    };

    private static MonitoriaAgendaDto Mapa(MonitoriaAgenda monitoria, Actor? actor = null) => new()
    {
        Id = monitoria.Id,
        Fecha = monitoria.Fecha.ToString("yyyy-MM-dd"),
        Descripcion = monitoria.Descripcion,
        CreadoPorPersona = monitoria.CreadoPorPersona,
        PuedeQuitar = actor is not null && EsCreador(actor, monitoria)
    };

    private async Task<Actor?> ActorAsync(CancellationToken ct)
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador))
        {
            return null;
        }

        var usuario = await _nucleo.BuscarPorProveedorAsync(identificador, ct);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return null;
        }

        var permisos = await _nucleo.PermisosDeUsuarioAsync(usuario.Id, ct);
        return new Actor(usuario, permisos);
    }

    private sealed record Actor(Usuario Usuario, IReadOnlyList<string> Permisos);
}
