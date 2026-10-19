using System.Security.Claims;
using CentroInvestigaciones.API.Autenticacion;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Route("api/sesion")]
public sealed class SesionController : ControllerBase
{
    private readonly OpcionesGoogle _google;
    private readonly IAlmacenNucleo _almacen;
    private readonly ServicioIngreso _ingreso;
    private readonly ServicioUsuarios _usuarios;
    private readonly IWebHostEnvironment _entorno;

    public SesionController(OpcionesGoogle google, IAlmacenNucleo almacen, ServicioIngreso ingreso, ServicioUsuarios usuarios, IWebHostEnvironment entorno)
    {
        _google = google;
        _almacen = almacen;
        _ingreso = ingreso;
        _usuarios = usuarios;
        _entorno = entorno;
    }

    [HttpGet("estado")]
    public ActionResult<EstadoAutenticacionDto> Estado()
    {
        return new EstadoAutenticacionDto
        {
            GoogleConfigurado = _google.EstaConfigurado,
            IngresoDesarrollo = _entorno.IsDevelopment()
        };
    }

    [HttpGet("yo")]
    public async Task<ActionResult<SesionActualDto>> Yo()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new SesionActualDto { Autenticado = false };
        }

        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var dto = new SesionActualDto
        {
            Autenticado = true,
            Nombre = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name"),
            CorreoElectronico = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
            IdentificadorProveedor = identificador
        };

        if (string.IsNullOrWhiteSpace(identificador) || !_almacen.Disponible)
        {
            return dto;
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
        if (usuario is null)
        {
            return dto;
        }

        dto.Nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? dto.Nombre : usuario.Nombre;
        dto.Nombres = string.IsNullOrWhiteSpace(usuario.Nombres) ? null : usuario.Nombres;
        dto.Apellidos = string.IsNullOrWhiteSpace(usuario.Apellidos) ? null : usuario.Apellidos;
        dto.NumeroContacto = string.IsNullOrWhiteSpace(usuario.NumeroContacto) ? null : usuario.NumeroContacto;
        dto.Extension = string.IsNullOrWhiteSpace(usuario.Extension) ? null : usuario.Extension;
        dto.AreaInteresId = usuario.AreaInteresId;
        dto.CorreoElectronico = string.IsNullOrWhiteSpace(usuario.CorreoElectronico) ? dto.CorreoElectronico : usuario.CorreoElectronico;
        dto.EstadoCuenta = usuario.Estado.ToString();
        dto.AreaInteres = usuario.AreaInteres?.Nombre;
        dto.AreaCodigo = usuario.AreaInteres?.Codigo;
        dto.Cargo = string.IsNullOrWhiteSpace(usuario.Cargo) ? null : usuario.Cargo;
        dto.TemaPreferido = string.IsNullOrWhiteSpace(usuario.TemaPreferido) ? "claro" : usuario.TemaPreferido;
        dto.RequiereCompletarIngreso = !usuario.PerfilCompleto;
        dto.RequiereElegirCargo = AccesoAgenda.RequiereElegirCargo(usuario.AreaInteres?.Codigo, usuario.Cargo);
        if (usuario.Estado == EstadoCuenta.Activo)
        {
            dto.Permisos = await _almacen.PermisosDeUsuarioAsync(usuario.Id, HttpContext.RequestAborted);
        }

        return dto;
    }

    [HttpPost("tema")]
    public async Task<IActionResult> GuardarTema([FromBody] TemaPreferidoDto solicitud)
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(identificador) || !_almacen.Disponible)
        {
            return Unauthorized();
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
        if (usuario is null)
        {
            return BadRequest();
        }

        var tema = string.IsNullOrWhiteSpace(solicitud.Tema) ? "claro" : solicitud.Tema.Trim().ToLowerInvariant();
        var permitidos = new HashSet<string>(StringComparer.Ordinal)
        {
            "claro", "oscuro", "slate", "teal", "oceano",
            "forest", "atardecer", "rosa", "indigo", "lavanda", "cyberpunk"
        };
        if (!permitidos.Contains(tema))
        {
            return BadRequest();
        }

        usuario.TemaPreferido = tema;
        await _almacen.GuardarAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("perfil-inicial")]
    public async Task<IActionResult> CompletarPerfil([FromBody] CompletarIngresoDto solicitud)
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(identificador))
        {
            return Unauthorized();
        }

        var hecho = await _ingreso.CompletarPerfilAsync(
            identificador,
            solicitud.Nombres,
            solicitud.Apellidos,
            solicitud.NumeroContacto,
            solicitud.Extension,
            solicitud.Cargo,
            solicitud.AreaInteresId,
            HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromBody] CompletarIngresoDto solicitud)
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(identificador))
        {
            return Unauthorized();
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
        if (usuario is null)
        {
            return Unauthorized();
        }

        var hecho = await _usuarios.ActualizarDatosAsync(
            usuario.Id,
            solicitud.Nombres,
            solicitud.Apellidos,
            solicitud.NumeroContacto,
            solicitud.Extension,
            solicitud.Cargo,
            usuario.AreaInteresId ?? Guid.Empty,
            false,
            usuario.Id,
            Direccion(),
            HttpContext.RequestAborted);
        return hecho ? NoContent() : BadRequest();
    }

    [HttpPost("area-interes")]
    public async Task<IActionResult> CompletarArea([FromBody] CompletarIngresoDto solicitud)
    {
        return await CompletarPerfil(solicitud);
    }

    [HttpGet("cuentas-desarrollo")]
    public async Task<ActionResult<List<CuentaDesarrolloDto>>> CuentasDesarrollo(CancellationToken ct)
    {
        if (!_entorno.IsDevelopment())
        {
            return NotFound();
        }

        var (usuarios, _) = await _almacen.ListarUsuariosAsync(0, 200, null, null, ct);
        return usuarios
            .Where(usuario => !string.IsNullOrWhiteSpace(usuario.IdentificadorProveedor))
            .OrderBy(usuario => usuario.Nombre)
            .Select(usuario => new CuentaDesarrolloDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Cargo = usuario.Cargo,
                Estado = usuario.Estado.ToString()
            })
            .ToList();
    }

    [HttpGet("ingreso-desarrollo")]
    public async Task<IActionResult> IngresoDesarrollo([FromQuery] Guid usuario, CancellationToken ct)
    {
        if (!_entorno.IsDevelopment())
        {
            return NotFound();
        }

        var cuenta = await _almacen.BuscarPorIdAsync(usuario, ct);
        if (cuenta is null || string.IsNullOrWhiteSpace(cuenta.IdentificadorProveedor))
        {
            return Redirect("/login");
        }

        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, cuenta.IdentificadorProveedor),
                new Claim(ClaimTypes.Name, cuenta.Nombre),
                new Claim(ClaimTypes.Email, cuenta.CorreoElectronico)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
        return Redirect("/");
    }

    [HttpGet("ingresar")]
    public IActionResult Ingresar([FromQuery] string? retorno)
    {
        if (!_google.EstaConfigurado)
        {
            return Redirect("/login?error=configuracion");
        }

        var destino = EsRetornoLocal(retorno) ? retorno! : "/";
        var propiedades = new AuthenticationProperties { RedirectUri = destino };
        return Challenge(propiedades, GoogleDefaults.AuthenticationScheme);
    }

    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private static bool EsRetornoLocal(string? retorno)
    {
        if (string.IsNullOrWhiteSpace(retorno))
        {
            return false;
        }

        if (!Uri.TryCreate(retorno, UriKind.Relative, out _))
        {
            return false;
        }

        return retorno.StartsWith('/')
            && !retorno.StartsWith("//", StringComparison.Ordinal)
            && !retorno.StartsWith("/\\", StringComparison.Ordinal);
    }

    private string? Direccion() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
