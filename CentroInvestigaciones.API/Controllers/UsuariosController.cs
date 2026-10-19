using System.Security.Claims;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;
    private readonly ServicioUsuarios _usuarios;

    public UsuariosController(IAlmacenNucleo almacen, ServicioUsuarios usuarios)
    {
        _almacen = almacen;
        _usuarios = usuarios;
    }

    [HttpGet]
    [RequierePermiso(PermisosNucleo.UsuariosConsultar)]
    public async Task<ActionResult<PaginaDto<UsuarioResumenDto>>> Listar([FromQuery] int pagina = 0, [FromQuery] int tamano = 10, [FromQuery] string? busqueda = null, [FromQuery] string? estado = null)
    {
        tamano = Math.Clamp(tamano, 1, 50);
        pagina = Math.Max(0, pagina);
        var (elementos, total) = await _almacen.ListarUsuariosAsync(pagina, tamano, busqueda, estado, HttpContext.RequestAborted);
        return new PaginaDto<UsuarioResumenDto>
        {
            Total = total,
            Elementos = elementos.Select(Mapa).ToList()
        };
    }

    [HttpPost("{id:guid}/estado")]
    [RequierePermiso(PermisosNucleo.UsuariosAprobar)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoDto solicitud)
    {
        if (!Enum.TryParse<EstadoCuenta>(solicitud.Estado, out var estado))
        {
            return BadRequest();
        }

        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.CambiarEstadoAsync(id, estado, actor.Value, Direccion(), HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPut("{id:guid}/area-cargo")]
    [RequierePermiso(PermisosNucleo.UsuariosAprobar)]
    public async Task<IActionResult> AsignarAreaCargo(Guid id, [FromBody] AsignarAreaCargoDto solicitud)
    {
        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.AsignarAreaCargoAsync(id, solicitud.AreaId, solicitud.Cargo, actor.Value, Direccion(), HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPost("{id:guid}/roles")]
    [RequierePermiso(PermisosNucleo.UsuariosModificar)]
    public async Task<IActionResult> AsignarRol(Guid id, [FromBody] AsignarRolDto solicitud)
    {
        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.AsignarRolAsync(id, solicitud.RolId, solicitud.AreaId, actor.Value, Direccion(), HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpGet("{id:guid}/herramientas")]
    [RequierePermiso(PermisosNucleo.UsuariosConsultar)]
    public async Task<ActionResult<IReadOnlyList<string>>> Herramientas(Guid id)
    {
        var permisos = await _almacen.PermisosDirectosDeUsuarioAsync(id, HttpContext.RequestAborted);
        var herramientas = CatalogoHerramientas.Todas
            .Select(item => item.CodigoPermiso)
            .Where(codigo => permisos.Contains(codigo, StringComparer.Ordinal))
            .ToList();
        return herramientas;
    }

    [HttpPut("{id:guid}/herramientas")]
    [RequierePermiso(PermisosNucleo.UsuariosModificar)]
    public async Task<IActionResult> AsignarHerramientas(Guid id, [FromBody] AsignarHerramientasDto solicitud)
    {
        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.AsignarHerramientasAsync(id, solicitud.CodigosPermiso, actor.Value, Direccion(), HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPut("{id:guid}/datos")]
    [RequierePermiso(PermisosNucleo.UsuariosModificar)]
    public async Task<IActionResult> ActualizarDatos(Guid id, [FromBody] CompletarIngresoDto solicitud)
    {
        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.ActualizarDatosAsync(
            id,
            solicitud.Nombres,
            solicitud.Apellidos,
            solicitud.NumeroContacto,
            solicitud.Extension,
            solicitud.Cargo,
            solicitud.AreaInteresId,
            true,
            actor.Value,
            Direccion(),
            HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPut("{id:guid}/acceso")]
    [RequierePermiso(PermisosNucleo.UsuariosModificar)]
    public async Task<IActionResult> GuardarAcceso(Guid id, [FromBody] GuardarAccesoUsuarioDto solicitud)
    {
        var actor = await ActorIdAsync();
        if (actor is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.EstablecerAccesoAsync(id, solicitud.Rol, solicitud.Herramientas, actor.Value, Direccion(), HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    private async Task<Guid?> ActorIdAsync()
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador))
        {
            return null;
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
        return usuario?.Id;
    }

    private string? Direccion() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static UsuarioResumenDto Mapa(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        CorreoElectronico = usuario.CorreoElectronico,
        EstadoCuenta = usuario.Estado.ToString(),
        AreaInteres = usuario.AreaInteres?.Nombre,
        Cargo = string.IsNullOrWhiteSpace(usuario.Cargo) ? null : usuario.Cargo,
        NumeroContacto = string.IsNullOrWhiteSpace(usuario.NumeroContacto) ? null : usuario.NumeroContacto,
        Nombres = usuario.Nombres,
        Apellidos = usuario.Apellidos,
        Extension = usuario.Extension,
        AreaInteresId = usuario.AreaInteresId,
        Rol = RolDe(usuario),
        FechaCreacionUtc = usuario.FechaCreacionUtc,
        UltimoAccesoUtc = usuario.UltimoAccesoUtc
    };

    private static string RolDe(Usuario usuario)
    {
        foreach (var codigo in new[] { PermisosNucleo.RolAdministrador, PermisosNucleo.RolJefeArea, PermisosNucleo.RolUsuario })
        {
            if (usuario.Asignaciones.Any(asignacion => asignacion.Rol?.Codigo == codigo))
            {
                return codigo;
            }
        }

        return PermisosNucleo.RolUsuario;
    }
}
