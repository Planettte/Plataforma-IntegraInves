using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/auditoria")]
public sealed class AuditoriaController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;

    public AuditoriaController(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    [HttpGet]
    [RequierePermiso(PermisosNucleo.AuditoriaConsultar)]
    public async Task<ActionResult<PaginaDto<EventoAuditoriaDto>>> Listar([FromQuery] int pagina = 0, [FromQuery] int tamano = 20)
    {
        tamano = Math.Clamp(tamano, 1, 50);
        pagina = Math.Max(0, pagina);
        var (elementos, total) = await _almacen.ListarEventosAsync(pagina, tamano, HttpContext.RequestAborted);
        var nombres = await _almacen.NombresUsuariosAsync(
            elementos.Where(evento => evento.UsuarioId is not null).Select(evento => evento.UsuarioId!.Value),
            HttpContext.RequestAborted);

        return new PaginaDto<EventoAuditoriaDto>
        {
            Total = total,
            Elementos = elementos.Select(evento => new EventoAuditoriaDto
            {
                FechaUtc = evento.FechaUtc,
                Actor = evento.UsuarioId is Guid id && nombres.TryGetValue(id, out var nombre) ? nombre : null,
                Accion = evento.Accion,
                Entidad = evento.Entidad,
                Resultado = evento.Resultado
            }).ToList()
        };
    }
}
