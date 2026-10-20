using System.Net.Mail;
using System.Security.Claims;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Dispositivos;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.estudios.dispositivos")]
[Route("api/dispositivos")]
public sealed class DispositivosController : ControllerBase
{
    private readonly IAlmacenDispositivos _almacen;
    private readonly IAlmacenNucleo _nucleo;

    public DispositivosController(IAlmacenDispositivos almacen, IAlmacenNucleo nucleo)
    {
        _almacen = almacen;
        _nucleo = nucleo;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<DispositivoDto>> Listar()
    {
        var hoy = SemaforoDispositivos.Hoy();
        return _almacen.Listar().Select(item => Mapa(item, hoy)).ToList();
    }

    [HttpGet("encargado")]
    public async Task<ActionResult<EncargadoDispositivosDto>> LeerEncargado(CancellationToken ct)
    {
        if (!await PuedeAdministrarAsync(ct))
        {
            return Forbid();
        }

        return new EncargadoDispositivosDto { Correo = _almacen.CorreoEncargado() };
    }

    [HttpPut("encargado")]
    public async Task<IActionResult> GuardarEncargado([FromBody] EncargadoDispositivosDto solicitud, CancellationToken ct)
    {
        if (!await PuedeAdministrarAsync(ct))
        {
            return Forbid();
        }

        var correo = solicitud.Correo?.Trim() ?? "";
        if (!MailAddress.TryCreate(correo, out _))
        {
            return BadRequest();
        }

        _almacen.GuardarCorreo(correo);
        return NoContent();
    }

    [HttpPost]
    public ActionResult<DispositivoDto> Crear([FromBody] GuardarDispositivoDto solicitud)
    {
        if (!TieneIdentidad(solicitud))
        {
            return BadRequest();
        }

        var creado = _almacen.Crear(Desde(solicitud));
        return CreatedAtAction(nameof(Listar), Mapa(creado, SemaforoDispositivos.Hoy()));
    }

    [HttpPut("{id:guid}")]
    public ActionResult<DispositivoDto> Actualizar(Guid id, [FromBody] GuardarDispositivoDto solicitud)
    {
        if (!TieneIdentidad(solicitud))
        {
            return BadRequest();
        }

        var actualizado = _almacen.Actualizar(id, Desde(solicitud));
        if (actualizado is null)
        {
            return NotFound();
        }

        return Mapa(actualizado, SemaforoDispositivos.Hoy());
    }

    private async Task<bool> PuedeAdministrarAsync(CancellationToken ct)
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador))
        {
            return false;
        }

        var usuario = await _nucleo.BuscarPorProveedorAsync(identificador, ct);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return false;
        }

        var permisos = await _nucleo.PermisosDeUsuarioAsync(usuario.Id, ct);
        return permisos.Contains(PermisosNucleo.UsuariosAprobar, StringComparer.Ordinal);
    }

    private static bool TieneIdentidad(GuardarDispositivoDto solicitud) =>
        !string.IsNullOrWhiteSpace(solicitud.Dm) || !string.IsNullOrWhiteSpace(solicitud.CodigoActivo);

    private static DispositivoMedico Desde(GuardarDispositivoDto solicitud) => new()
    {
        Dm = solicitud.Dm,
        Marca = solicitud.Marca,
        Modelo = solicitud.Modelo,
        Riesgo = solicitud.Riesgo,
        Serie = solicitud.Serie,
        CodigoActivo = solicitud.CodigoActivo,
        InventarioBiomedica = solicitud.InventarioBiomedica,
        RegistroInvima = solicitud.RegistroInvima,
        PeriodicidadMantenimiento = solicitud.PeriodicidadMantenimiento,
        UltimoMantenimiento = solicitud.UltimoMantenimiento,
        MesCalificacion = solicitud.MesCalificacion,
        MesVerificacionMetrologica = solicitud.MesVerificacionMetrologica,
        PeriodicidadCalibracion = solicitud.PeriodicidadCalibracion,
        UltimaCalibracion = solicitud.UltimaCalibracion,
        Ubicacion = solicitud.Ubicacion,
        Area = solicitud.Area,
        Observaciones = solicitud.Observaciones
    };

    private static DispositivoDto Mapa(DispositivoMedico item, DateOnly hoy) => new()
    {
        Id = item.Id,
        Dm = item.Dm,
        Marca = item.Marca,
        Modelo = item.Modelo,
        Riesgo = item.Riesgo,
        Serie = item.Serie,
        CodigoActivo = item.CodigoActivo,
        InventarioBiomedica = item.InventarioBiomedica,
        RegistroInvima = item.RegistroInvima,
        PeriodicidadMantenimiento = item.PeriodicidadMantenimiento,
        UltimoMantenimiento = item.UltimoMantenimiento,
        MesCalificacion = item.MesCalificacion,
        MesVerificacionMetrologica = item.MesVerificacionMetrologica,
        PeriodicidadCalibracion = item.PeriodicidadCalibracion,
        UltimaCalibracion = item.UltimaCalibracion,
        Ubicacion = item.Ubicacion,
        Area = item.Area,
        Observaciones = item.Observaciones,
        Mantenimiento = Semaforo(SemaforoDispositivos.Mantenimiento(item, hoy)),
        Calibracion = Semaforo(SemaforoDispositivos.Calibracion(item, hoy)),
        Calificacion = Semaforo(SemaforoDispositivos.Calificacion(item, hoy))
    };

    private static SemaforoDispositivoDto Semaforo(SemaforoCalculado semaforo) => new()
    {
        Estado = semaforo.Estado,
        Etiqueta = semaforo.Etiqueta
    };
}
