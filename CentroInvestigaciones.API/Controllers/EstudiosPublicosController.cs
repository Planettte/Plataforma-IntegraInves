using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/estudios/publico")]
public sealed class EstudiosPublicosController : ControllerBase
{
    private const long TamanoMaximo = 20 * 1024 * 1024;
    private readonly IAlmacenEstudios _almacen;
    private readonly ServicioFlujoEstudios _flujo;

    public EstudiosPublicosController(IAlmacenEstudios almacen, ServicioFlujoEstudios flujo)
    {
        _almacen = almacen;
        _flujo = flujo;
    }

    [HttpGet("{token}")]
    public ActionResult<EstudioDto> Obtener(string token)
    {
        var estudio = _almacen.BuscarPorToken(token);
        return estudio is null ? NotFound() : ServicioFlujoEstudios.ADto(estudio);
    }

    [HttpPost]
    public async Task<ActionResult<ResultadoEstudioDto>> Crear([FromBody] GuardarFase1EstudioDto solicitud)
    {
        var resultado = await _flujo.GuardarFase1Async(solicitud, null, Origen(), HttpContext.RequestAborted);
        return resultado is null
            ? BadRequest()
            : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado, "Error al notificar al usuario. El registro sí quedó guardado.");
    }

    [HttpPost("pdf")]
    public IActionResult VistaPreviaPdf([FromQuery] int formato, [FromBody] GuardarFase1EstudioDto solicitud)
    {
        var generado = _flujo.VistaPreviaPdf(solicitud, formato);
        return generado is null
            ? BadRequest()
            : File(generado.Value.Pdf, "application/pdf", generado.Value.Nombre);
    }

    [HttpPut("{token}")]
    public async Task<ActionResult<ResultadoEstudioDto>> Corregir(string token, [FromBody] GuardarFase1EstudioDto solicitud)
    {
        if (_almacen.BuscarPorToken(token)?.TipoRegistro != solicitud.TipoRegistro)
        {
            return BadRequest();
        }

        var resultado = await _flujo.GuardarFase1Async(solicitud, token, Origen(), HttpContext.RequestAborted);
        return resultado is null
            ? BadRequest()
            : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado, "Error al notificar al usuario. El registro sí quedó guardado.");
    }

    [HttpPost("{token}/archivos")]
    [RequestSizeLimit(TamanoMaximo)]
    public async Task<IActionResult> Subir(string token, [FromQuery] string clave, IFormFile archivo)
    {
        var estudio = _almacen.BuscarPorToken(token);
        if (estudio is null || archivo is null || archivo.Length == 0 || archivo.Length > TamanoMaximo || !ClavePermitida(estudio, clave))
        {
            return BadRequest();
        }

        if (clave is ClavesArchivoEstudio.Fase2Aval or ClavesArchivoEstudio.Fase2Cei or ClavesArchivoEstudio.Fase2Acta)
        {
            _almacen.QuitarPorClave(estudio.Id, clave);
        }

        await using var lectura = archivo.OpenReadStream();
        var guardado = _almacen.AgregarArchivo(estudio.Id, clave, archivo.FileName, archivo.ContentType, lectura);
        return guardado is null ? BadRequest() : NoContent();
    }

    [HttpPost("{token}/fase2")]
    public async Task<ActionResult<ResultadoEstudioDto>> Fase2(string token, [FromBody] Fase2EstudioDto solicitud)
    {
        var resultado = await _flujo.CerrarFase2Async(token, solicitud.NumeroAprobacionCei, Origen(), HttpContext.RequestAborted);
        return resultado is null ? BadRequest() : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado);
    }

    [HttpGet("{token}/archivos/{archivoId:guid}")]
    public IActionResult Descargar(string token, Guid archivoId)
    {
        var estudio = _almacen.BuscarPorToken(token);
        var archivo = estudio?.Archivos.FirstOrDefault(item => item.Id == archivoId && !ClavesArchivoEstudio.EsFirma(item.Clave));
        var stream = archivo is null ? null : _almacen.AbrirPorToken(token, archivoId);
        return stream is null || archivo is null
            ? NotFound()
            : File(stream, TipoDescarga.De(archivo.Nombre), archivo.Nombre);
    }

    private static bool ClavePermitida(Estudio estudio, string clave)
    {
        if (clave == ClavesArchivoEstudio.Adicional)
        {
            return estudio.Estado == EstadosEstudio.RecepcionIncompleta;
        }

        return estudio.Estado == EstadosEstudio.AprobadoComite
            && clave is ClavesArchivoEstudio.Fase2Aval or ClavesArchivoEstudio.Fase2Cei or ClavesArchivoEstudio.Fase2Acta;
    }

    private ResultadoEstudioDto Resultado(Estudio estudio, bool enviado, string? avisoFallo = null) => new()
    {
        Estudio = ServicioFlujoEstudios.ADto(estudio),
        CorreoEnviado = enviado,
        Aviso = enviado
            ? null
            : avisoFallo ?? "El aviso al comité no salió por correo. El registro sí quedó guardado."
    };

    private string Origen() => $"{Request.Scheme}://{Request.Host}";
}
