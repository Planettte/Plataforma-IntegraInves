using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/estudios/publico")]
public sealed class EstudiosPublicosController : ControllerBase
{
    private readonly IAlmacenEstudios _almacen;
    private readonly ServicioFlujoEstudios _flujo;
    private readonly RetoCaptcha _captcha;
    private readonly OrigenPublico _origen;

    public EstudiosPublicosController(IAlmacenEstudios almacen, ServicioFlujoEstudios flujo, RetoCaptcha captcha, OrigenPublico origen)
    {
        _almacen = almacen;
        _flujo = flujo;
        _captcha = captcha;
        _origen = origen;
    }

    private ActionResult? SinCaptcha() =>
        _captcha.Listo(HttpContext) ? null : StatusCode(StatusCodes.Status403Forbidden);

    [HttpGet("{token}")]
    public ActionResult<EstudioDto> Obtener(string token)
    {
        var estudio = _almacen.BuscarPorToken(token);
        return estudio is null ? NotFound() : ServicioFlujoEstudios.ADto(estudio);
    }

    [EnableRateLimiting("envios-publicos")]
    [HttpPost]
    public async Task<ActionResult<ResultadoEstudioDto>> Crear([FromBody] GuardarFase1EstudioDto solicitud)
    {
        if (SinCaptcha() is { } rechazo)
        {
            return rechazo;
        }

        var resultado = await _flujo.GuardarFase1Async(solicitud, null, Origen(), HttpContext.RequestAborted);
        return resultado is null
            ? BadRequest()
            : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado, "Error al notificar al usuario. El registro sí quedó guardado.");
    }

    [EnableRateLimiting("envios-publicos")]
    [HttpPost("pdf")]
    public IActionResult VistaPreviaPdf([FromQuery] int formato, [FromBody] GuardarFase1EstudioDto solicitud)
    {
        if (SinCaptcha() is { } rechazo)
        {
            return rechazo;
        }

        var generado = _flujo.VistaPreviaPdf(solicitud, formato);
        return generado is null
            ? BadRequest()
            : File(generado.Value.Pdf, "application/pdf", generado.Value.Nombre);
    }

    [EnableRateLimiting("envios-publicos")]
    [HttpPut("{token}")]
    public async Task<ActionResult<ResultadoEstudioDto>> Corregir(string token, [FromBody] GuardarFase1EstudioDto solicitud)
    {
        if (SinCaptcha() is { } rechazo)
        {
            return rechazo;
        }

        if (_almacen.BuscarPorToken(token)?.TipoRegistro != solicitud.TipoRegistro)
        {
            return BadRequest();
        }

        var resultado = await _flujo.GuardarFase1Async(solicitud, token, Origen(), HttpContext.RequestAborted);
        return resultado is null
            ? BadRequest()
            : Resultado(resultado.Value.Estudio, resultado.Value.CorreoEnviado, "Error al notificar al usuario. El registro sí quedó guardado.");
    }

    [EnableRateLimiting("envios-publicos")]
    [HttpPost("{token}/archivos")]
    [RequestSizeLimit(ArchivoRecibido.TamanoMaximo)]
    public async Task<IActionResult> Subir(string token, [FromQuery] string clave, IFormFile archivo)
    {
        if (SinCaptcha() is { } rechazo)
        {
            return rechazo;
        }

        var estudio = _almacen.BuscarPorToken(token);
        if (estudio is null || archivo is null || !ClavePermitida(estudio, clave))
        {
            return BadRequest();
        }

        if (clave == ClavesArchivoEstudio.Adicional
            && estudio.Archivos.Count(item => item.Clave == ClavesArchivoEstudio.Adicional) >= ArchivoRecibido.MaximoAdicionales)
        {
            return BadRequest("Este estudio ya tiene el máximo de archivos.");
        }

        await using var contenido = await ArchivoRecibido.LeerAsync(archivo, HttpContext.RequestAborted);
        if (contenido is null)
        {
            return BadRequest("El archivo no es válido.");
        }

        if (clave is ClavesArchivoEstudio.Fase2Aval or ClavesArchivoEstudio.Fase2Cei or ClavesArchivoEstudio.Fase2Acta)
        {
            _almacen.QuitarPorClave(estudio.Id, clave);
        }

        var guardado = _almacen.AgregarArchivo(estudio.Id, clave, archivo.FileName, archivo.ContentType, contenido);
        return guardado is null ? BadRequest() : NoContent();
    }

    [EnableRateLimiting("envios-publicos")]
    [HttpPost("{token}/fase2")]
    public async Task<ActionResult<ResultadoEstudioDto>> Fase2(string token, [FromBody] Fase2EstudioDto solicitud)
    {
        if (SinCaptcha() is { } rechazo)
        {
            return rechazo;
        }

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

    private string Origen() => _origen.Valor;
}
