using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Application.Proyectos;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.udd.proyectos")]
[Route("api/proyectos")]
public sealed class ProyectosController : ControllerBase
{
    private const string Permiso = "herramientas.udd.proyectos";

    private readonly IAlmacenProyectos _almacen;
    private readonly IAlmacenNucleo _nucleo;
    private readonly GeneradorPdfProyectos _pdf;

    public ProyectosController(IAlmacenProyectos almacen, IAlmacenNucleo nucleo, GeneradorPdfProyectos pdf)
    {
        _almacen = almacen;
        _nucleo = nucleo;
        _pdf = pdf;
    }

    [HttpGet("responsables")]
    public async Task<ActionResult<List<ResponsableProyectoDto>>> Responsables(CancellationToken ct)
    {
        var (usuarios, _) = await _nucleo.ListarUsuariosAsync(0, 500, null, nameof(EstadoCuenta.Activo), ct);
        var lista = new List<ResponsableProyectoDto>();
        foreach (var usuario in usuarios.OrderBy(item => item.Nombre))
        {
            var permisos = await _nucleo.PermisosDeUsuarioAsync(usuario.Id, ct);
            if (permisos.Contains(Permiso, StringComparer.Ordinal))
            {
                lista.Add(new ResponsableProyectoDto { Id = usuario.Id, Nombre = usuario.Nombre });
            }
        }

        return lista;
    }

    [HttpGet]
    public ActionResult<List<ProyectoListaDto>> Listar([FromQuery] Guid responsable)
    {
        if (responsable == Guid.Empty)
        {
            return BadRequest("Selecciona un responsable.");
        }

        return _almacen.Listar(responsable).Select(item => new ProyectoListaDto
        {
            Id = item.Id,
            Nombre = item.Nombre,
            Semillero = item.Semillero,
            ResponsableId = item.ResponsableId,
            CreadoEl = item.CreadoEl
        }).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TableroProyectoDto>> Obtener(Guid id, CancellationToken ct)
    {
        var tablero = _almacen.Tablero(id);
        return tablero is null ? NotFound() : await Mapa(tablero, ct);
    }

    [HttpPost]
    public async Task<ActionResult<ProyectoListaDto>> Crear([FromBody] CrearProyectoDto solicitud, CancellationToken ct)
    {
        if (!await EsResponsable(solicitud.ResponsableId, ct))
        {
            return BadRequest("El responsable no tiene esta herramienta.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            return BadRequest("Ingresa el nombre del proyecto.");
        }

        try
        {
            var proyecto = _almacen.Crear(new ProyectoSeguimiento
            {
                Nombre = solicitud.Nombre,
                Semillero = solicitud.Semillero ?? "",
                ResponsableId = solicitud.ResponsableId
            }, (solicitud.Fases ?? []).Select(item => new FaseProyecto { Nombre = item.Nombre ?? "", Peso = item.Peso }).ToList());
            return new ProyectoListaDto
            {
                Id = proyecto.Id,
                Nombre = proyecto.Nombre,
                Semillero = proyecto.Semillero,
                ResponsableId = proyecto.ResponsableId,
                CreadoEl = proyecto.CreadoEl
            };
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPut("{id:guid}")]
    public ActionResult<ProyectoListaDto> Actualizar(Guid id, [FromBody] ActualizarProyectoDto solicitud)
    {
        try
        {
            var proyecto = _almacen.Actualizar(id, solicitud.Nombre ?? "", solicitud.Semillero ?? "");
            return new ProyectoListaDto
            {
                Id = proyecto.Id,
                Nombre = proyecto.Nombre,
                Semillero = proyecto.Semillero,
                ResponsableId = proyecto.ResponsableId,
                CreadoEl = proyecto.CreadoEl
            };
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPut("{id:guid}/fases")]
    public IActionResult Fases(Guid id, [FromBody] List<FaseEdicionDto> fases)
    {
        try
        {
            _almacen.ReemplazarFases(id, (fases ?? []).Select(item => new FaseProyecto
            {
                Id = item.Id,
                Nombre = item.Nombre ?? "",
                Peso = item.Peso
            }).ToList());
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPut("{id:guid}/actividades")]
    public IActionResult Actividades(Guid id, [FromBody] List<ActividadEdicionDto> actividades)
    {
        try
        {
            _almacen.GuardarActividades(id, (actividades ?? []).Select(item => new ActividadProyecto
            {
                Id = item.Id,
                FaseId = item.FaseId,
                Descripcion = item.Descripcion ?? "",
                FechaInicio = item.FechaInicio ?? "",
                FechaLimite = item.FechaLimite ?? "",
                Peso = item.Peso
            }).ToList());
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPut("actividades/{id:guid}/estado")]
    public IActionResult Estado(Guid id, [FromBody] EstadoActividadDto solicitud)
    {
        try
        {
            _almacen.CambiarEstado(id, solicitud.Estado ?? "");
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPost("investigadores")]
    public ActionResult<InvestigadorProyectoDto> Investigador([FromBody] InvestigadorProyectoDto solicitud)
    {
        try
        {
            var investigador = _almacen.CrearInvestigador(solicitud.Nombre ?? "");
            return new InvestigadorProyectoDto { Id = investigador.Id, Nombre = investigador.Nombre };
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPost("asignaciones")]
    public IActionResult Asignar([FromBody] AsignacionDto solicitud)
    {
        try
        {
            _almacen.Asignar(solicitud.ActividadId, solicitud.InvestigadorId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpDelete("asignaciones/{actividadId:guid}/{investigadorId:guid}")]
    public IActionResult QuitarAsignacion(Guid actividadId, Guid investigadorId)
    {
        try
        {
            _almacen.QuitarAsignacion(actividadId, investigadorId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPost("actividades/{id:guid}/prorroga")]
    public IActionResult Prorroga(Guid id, [FromBody] ProrrogaDto solicitud)
    {
        try
        {
            _almacen.Prorrogar(id, solicitud.Fecha ?? "", solicitud.Investigadores ?? []);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(ProyectosController));
        }
    }

    [HttpPost("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var tablero = _almacen.Tablero(id);
        if (tablero is null)
        {
            return NotFound();
        }

        var dto = await Mapa(tablero, ct);
        var nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? "proyecto" : dto.Nombre.Trim();
        foreach (var caracter in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(caracter, '-');
        }

        return File(_pdf.Generar(dto), "application/pdf", $"Seguimiento_{nombre}.pdf");
    }

    private async Task<bool> EsResponsable(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var usuario = await _nucleo.BuscarPorIdAsync(id, ct);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return false;
        }

        var permisos = await _nucleo.PermisosDeUsuarioAsync(id, ct);
        return permisos.Contains(Permiso, StringComparer.Ordinal);
    }

    private async Task<TableroProyectoDto> Mapa(TableroProyecto tablero, CancellationToken ct)
    {
        var nombres = await _nucleo.NombresUsuariosAsync([tablero.ResponsableId], ct);
        return new TableroProyectoDto
        {
            Id = tablero.Id,
            Nombre = tablero.Nombre,
            Semillero = tablero.Semillero,
            ResponsableId = tablero.ResponsableId,
            ResponsableNombre = nombres.GetValueOrDefault(tablero.ResponsableId) ?? "",
            Progreso = tablero.Progreso,
            Investigadores = tablero.Investigadores.Select(item => new InvestigadorProyectoDto { Id = item.Id, Nombre = item.Nombre }).ToList(),
            Fases = tablero.Fases.Select(fase => new TableroFaseDto
            {
                Id = fase.Id,
                Nombre = fase.Nombre,
                Orden = fase.Orden,
                Peso = fase.Peso,
                Progreso = fase.Progreso,
                Investigadores = fase.Investigadores.Select(item => new AvanceInvestigadorDto { Nombre = item.Nombre, Porcentaje = item.Porcentaje }).ToList(),
                Actividades = fase.Actividades.Select(actividad => new TableroActividadDto
                {
                    Id = actividad.Id,
                    Descripcion = actividad.Descripcion,
                    FechaInicio = actividad.FechaInicio,
                    FechaLimite = actividad.FechaLimite,
                    FechasAnteriores = actividad.FechasAnteriores,
                    Peso = actividad.Peso,
                    Estado = actividad.Estado,
                    Asignados = actividad.Asignados.Select(item => new AsignadoTableroDto
                    {
                        Id = item.Id,
                        Nombre = item.Nombre,
                        Peso = item.Peso,
                        Prorrogas = item.Prorrogas
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
