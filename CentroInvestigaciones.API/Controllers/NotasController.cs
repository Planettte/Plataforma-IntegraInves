using System.Security.Claims;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notas")]
public sealed class NotasController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;

    public NotasController(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotaDto>>> Listar()
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        var notas = await _almacen.ListarNotasAsync(usuario.Id, HttpContext.RequestAborted);
        return notas.Select(Mapa).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<NotaDto>> Crear([FromBody] GuardarNotaDto solicitud)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Titulo) || string.IsNullOrWhiteSpace(solicitud.Contenido))
        {
            return BadRequest();
        }

        var nota = new Nota
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Titulo = solicitud.Titulo.Trim(),
            Contenido = solicitud.Contenido.Trim(),
            FechaCreacionUtc = DateTime.UtcNow
        };
        await _almacen.AgregarNotaAsync(nota, HttpContext.RequestAborted);
        await _almacen.GuardarAsync(HttpContext.RequestAborted);
        return CreatedAtAction(nameof(Listar), Mapa(nota));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] GuardarNotaDto solicitud)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        var nota = await _almacen.BuscarNotaAsync(id, HttpContext.RequestAborted);
        if (nota is null || nota.UsuarioId != usuario.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Titulo) || string.IsNullOrWhiteSpace(solicitud.Contenido))
        {
            return BadRequest();
        }

        nota.Titulo = solicitud.Titulo.Trim();
        nota.Contenido = solicitud.Contenido.Trim();
        nota.FechaActualizacionUtc = DateTime.UtcNow;
        await _almacen.GuardarAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            return Forbid();
        }

        var nota = await _almacen.BuscarNotaAsync(id, HttpContext.RequestAborted);
        if (nota is null || nota.UsuarioId != usuario.Id)
        {
            return NotFound();
        }

        await _almacen.EliminarNotaAsync(nota, HttpContext.RequestAborted);
        await _almacen.GuardarAsync(HttpContext.RequestAborted);
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

    private static NotaDto Mapa(Nota nota) => new()
    {
        Id = nota.Id,
        Titulo = nota.Titulo,
        Contenido = nota.Contenido,
        FechaCreacionUtc = nota.FechaCreacionUtc,
        FechaActualizacionUtc = nota.FechaActualizacionUtc
    };
}
