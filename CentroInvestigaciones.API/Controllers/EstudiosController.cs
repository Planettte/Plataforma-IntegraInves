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
    private readonly IAlmacenEstudios _almacen;
    private readonly ServicioFlujoEstudios _flujo;
    private readonly OrigenPublico _origen;
    private readonly IAlmacenCostosComite _costos;

    public EstudiosController(IAlmacenEstudios almacen, ServicioFlujoEstudios flujo, OrigenPublico origen, IAlmacenCostosComite costos)
    {
        _almacen = almacen;
        _flujo = flujo;
        _origen = origen;
        _costos = costos;
    }

    [HttpGet("costos-comite")]
    public ActionResult<CostosComiteDto> LeerCostos() => Mapa(_costos.Leer());

    [HttpPut("costos-comite")]
    public ActionResult<CostosComiteDto> GuardarCostos([FromBody] CostosComiteDto solicitud)
    {
        var costos = new CostosComite
        {
            ValorEtica = solicitud.ValorEtica ?? "",
            ResponsableEtica = solicitud.ResponsableEtica ?? "",
            ValorInvestigacion = solicitud.ValorInvestigacion ?? "",
            ResponsableInvestigacion = solicitud.ResponsableInvestigacion ?? ""
        };
        _costos.Guardar(costos);
        return Mapa(_costos.Leer());
    }

    private static CostosComiteDto Mapa(CostosComite costos) => new()
    {
        ValorEtica = costos.ValorEtica,
        ResponsableEtica = costos.ResponsableEtica,
        ValorInvestigacion = costos.ValorInvestigacion,
        ResponsableInvestigacion = costos.ResponsableInvestigacion
    };

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
    [RequestSizeLimit(ArchivoRecibido.TamanoMaximo)]
    public async Task<IActionResult> AvalArchivo(Guid id, IFormFile archivo)
    {
        var estudio = _almacen.Buscar(id);
        if (estudio is null || estudio.Estado == EstadosEstudio.AprobadoCei || archivo is null)
        {
            return BadRequest();
        }

        await using var contenido = await ArchivoRecibido.LeerAsync(archivo, HttpContext.RequestAborted);
        if (contenido is null)
        {
            return BadRequest("El archivo no es válido.");
        }

        _almacen.QuitarPorClave(id, ClavesArchivoEstudio.AvalComite);
        var guardado = _almacen.AgregarArchivo(id, ClavesArchivoEstudio.AvalComite, archivo.FileName, archivo.ContentType, contenido);
        return guardado is null ? BadRequest() : NoContent();
    }

    [HttpGet("{id:guid}/archivos/{archivoId:guid}")]
    public IActionResult Descargar(Guid id, Guid archivoId)
    {
        var estudio = _almacen.Buscar(id);
        var archivo = estudio?.Archivos.FirstOrDefault(item => item.Id == archivoId && !ClavesArchivoEstudio.EsFirma(item.Clave));
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

    private string Origen() => _origen.Valor;
}
