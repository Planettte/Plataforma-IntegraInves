using System.Security.Claims;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notificaciones")]
public sealed class NotificacionesController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;
    private readonly AlmacenNotificaciones _notificaciones;

    public NotificacionesController(IAlmacenNucleo almacen, AlmacenNotificaciones notificaciones)
    {
        _almacen = almacen;
        _notificaciones = notificaciones;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Listar()
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        var lista = new List<NotificacionDto>();
        foreach (var aviso in _notificaciones.ListarVigentes(usuario.Id))
        {
            lista.Add(new NotificacionDto
            {
                Id = aviso.Id,
                Origen = "Estudios",
                Titulo = aviso.Titulo,
                Mensaje = aviso.Mensaje,
                Enlace = aviso.Enlace,
                FechaUtc = aviso.FechaUtc,
                Vista = aviso.VistaUtc is not null
            });
        }

        var permisos = await _almacen.PermisosDeUsuarioAsync(usuario.Id, HttpContext.RequestAborted);
        var herramientas = CatalogoHerramientas.Todas
            .Where(item => permisos.Contains(item.CodigoPermiso, StringComparer.Ordinal))
            .ToList();

        if (herramientas.Count == 0)
        {
            lista.Add(new NotificacionDto
            {
                Origen = "Sistema",
                Titulo = "Sin herramientas asignadas",
                Mensaje = "Un administrador puede darte acceso a las herramientas desde Administración → Usuarios.",
                Enlace = "/estado-cuenta",
                FechaUtc = DateTime.UtcNow
            });
        }

        return lista.OrderByDescending(item => item.FechaUtc).ToList();
    }

    [HttpPost("{id:guid}/vista")]
    public async Task<IActionResult> MarcarVista(Guid id)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        if (!_notificaciones.Pertenece(usuario.Id, id))
        {
            return NotFound();
        }

        _notificaciones.MarcarVista(usuario.Id, id);
        return NoContent();
    }

    private async Task<Usuario?> UsuarioActualAsync()
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador) || !_almacen.Disponible)
        {
            return null;
        }

        return await _almacen.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
    }
}
