using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.comite.estudios")]
[Route("api/estudios")]
public sealed class EstudiosController : ControllerBase
{
    private const long TamanoMaximo = 20 * 1024 * 1024;
    private readonly IAlmacenEstudios _almacen;
    private readonly ServicioFlujoEstudios _flujo;

    public EstudiosController(IAlmacenEstudios almacen, ServicioFlujoEstudios flujo)
    {
        _almacen = almacen;
        _flujo = flujo;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<EstudioResumenDto>> Listar()
    {
        return _almacen.Listar().Select(item => new EstudioResumenDto
        {
            Id = item.Id,
            Conteo = item.Conteo,
            TipoRegistro = item.TipoRegistro,
            Estado = item.Estado,
            Anio = item.Anio,
            Titulo = item.Titulo,
            Institucion = item.Institucion,
            InvestigadorPrincipal = item.InvestigadorPrincipal,
            Correo = item.Correo,
            FechaCreacionUtc = item.FechaCreacionUtc
        }).ToList();
    }

    [HttpGet("{id:guid}")]
    public ActionResult<EstudioDto> Obtener(Guid id)
    {
        var estudio = _almacen.Buscar(id);
        return estudio is null ? NotFound() : ServicioFlujoEstudios.ADto(estudio);
    }

    [HttpPost("{id:guid}/correccion")]
    public async Task<ActionResult<ResultadoEstudioDto>> Correccion(Guid id, [FromBody] MensajeEstudioDto solicitud)
    {
        var resultado = await _flujo.CorregirAsync(id, solicitud.Mensaje, Origen(), HttpContext.RequestAborted);
        return resultado is null ? BadRequest() : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado);
    }

    [HttpPost("{id:guid}/aval")]
    public async Task<ActionResult<ResultadoEstudioDto>> Aval(Guid id, [FromBody] MensajeEstudioDto solicitud)
    {
        var resultado = await _flujo.AvalarAsync(id, solicitud.Mensaje, Origen(), HttpContext.RequestAborted);
        return resultado is null ? BadRequest() : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado);
    }

    [HttpPost("{id:guid}/finalizar")]
    public async Task<ActionResult<ResultadoEstudioDto>> Finalizar(Guid id)
    {
        var resultado = await _flujo.FinalizarAsync(id, HttpContext.RequestAborted);
        return resultado is null ? BadRequest() : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado);
    }

    [HttpPost("{id:guid}/reenviar")]
    public async Task<ActionResult<ResultadoEstudioDto>> Reenviar(Guid id, [FromBody] ReenvioEstudioDto solicitud)
    {
        var resultado = await _flujo.ReenviarAsync(id, solicitud.Tipo, Origen(), HttpContext.RequestAborted);
        return resultado is null ? BadRequest() : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado);
    }

    [HttpPost("{id:guid}/aval-archivo")]
    [RequestSizeLimit(TamanoMaximo)]
    public async Task<IActionResult> AvalArchivo(Guid id, IFormFile archivo)
    {
        var estudio = _almacen.Buscar(id);
        if (estudio is null || estudio.Estado == EstadosEstudio.AprobadoCei || archivo.Length == 0 || archivo.Length > TamanoMaximo)
        {
            return BadRequest();
        }

        _almacen.QuitarPorClave(id, ClavesArchivoEstudio.AvalComite);
        await using var lectura = archivo.OpenReadStream();
        var guardado = _almacen.AgregarArchivo(id, ClavesArchivoEstudio.AvalComite, archivo.FileName, archivo.ContentType, lectura);
        return guardado is null ? BadRequest() : NoContent();
    }

    [HttpGet("{id:guid}/archivos/{archivoId:guid}")]
    public IActionResult Descargar(Guid id, Guid archivoId)
    {
        var estudio = _almacen.Buscar(id);
        var archivo = estudio?.Archivos.FirstOrDefault(item => item.Id == archivoId);
        var stream = archivo is null ? null : _almacen.Abrir(id, archivoId);
        return stream is null || archivo is null
            ? NotFound()
            : File(stream, TipoDescarga.De(archivo.Nombre), archivo.Nombre);
    }

    private ResultadoEstudioDto Resultado(Estudio estudio, bool enviado) => new()
    {
        Estudio = ServicioFlujoEstudios.ADto(estudio),
        CorreoEnviado = enviado,
        Aviso = enviado
            ? null
            : "Error al notificar al usuario."
    };

    private string Origen() => $"{Request.Scheme}://{Request.Host}";
}
