using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Application.Protocolos;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/protocolos/publico")]
public sealed class ProtocolosPublicosController : ControllerBase
{
    private readonly IAlmacenProtocolos _almacen;
    private readonly IAlmacenNucleo _nucleo;
    private readonly AvisosProtocolo _avisos;
    private readonly IWebHostEnvironment _entorno;

    public ProtocolosPublicosController(IAlmacenProtocolos almacen, IAlmacenNucleo nucleo, AvisosProtocolo avisos, IWebHostEnvironment entorno)
    {
        _almacen = almacen;
        _nucleo = nucleo;
        _avisos = avisos;
        _entorno = entorno;
    }

    [HttpGet("revisores")]
    public async Task<ActionResult<List<RevisorProtocoloDto>>> Revisores(CancellationToken ct) =>
        await RevisoresProtocolo.ListarAsync(_nucleo, ct);

    [HttpGet("instrucciones")]
    public IActionResult Instrucciones()
    {
        var ruta = Path.Combine(_entorno.ContentRootPath, "Recursos", "como-escribir-un-protocolo.pdf");
        if (!System.IO.File.Exists(ruta))
        {
            return NotFound();
        }

        return PhysicalFile(ruta, "application/pdf", "Cómo escribir un protocolo.pdf");
    }

    [HttpGet("{token}")]
    public async Task<ActionResult<ProtocoloDto>> Obtener(string token, CancellationToken ct)
    {
        var protocolo = _almacen.BuscarPorToken(token);
        return protocolo is null ? NotFound() : await HaciaAsync(protocolo, ct);
    }

    [HttpPost]
    public async Task<ActionResult<ResultadoProtocoloDto>> Crear([FromBody] GuardarProtocoloDto solicitud, CancellationToken ct)
    {
        if (!await RevisoresProtocolo.EsMedicoUddAsync(_nucleo, solicitud.RevisorId, ct))
        {
            return BadRequest("El médico de apoyo debe ser una cuenta de UDD con esta herramienta.");
        }

        try
        {
            var protocolo = _almacen.Crear(MapaProtocolo.Desde(solicitud));
            var (nombre, correo) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
            var enviado = await _avisos.NuevoAsync(protocolo, correo, nombre, Origen(), ct);
            return Resultado(protocolo, nombre, enviado, "Error al notificar al revisor. El protocolo sí quedó guardado.");
        }
        catch (InvalidOperationException excepcion)
        {
            return Fallo(excepcion);
        }
    }

    [HttpPut("{token}")]
    public async Task<ActionResult<ResultadoProtocoloDto>> Reenviar(string token, [FromBody] GuardarProtocoloDto solicitud, CancellationToken ct)
    {
        if (!await RevisoresProtocolo.EsMedicoUddAsync(_nucleo, solicitud.RevisorId, ct))
        {
            return BadRequest("El médico de apoyo debe ser una cuenta de UDD con esta herramienta.");
        }

        try
        {
            var protocolo = _almacen.Reenviar(token, MapaProtocolo.Desde(solicitud));
            var (nombre, correo) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
            var enviado = await _avisos.ReenviadoAsync(protocolo, correo, nombre, Origen(), ct);
            return Resultado(protocolo, nombre, enviado, "Error al notificar al revisor. El protocolo sí quedó guardado.");
        }
        catch (InvalidOperationException excepcion)
        {
            return Fallo(excepcion);
        }
    }

    private async Task<ProtocoloDto> HaciaAsync(CentroInvestigaciones.Domain.Protocolo protocolo, CancellationToken ct)
    {
        var (nombre, _) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
        return MapaProtocolo.Hacia(protocolo, nombre);
    }

    private ResultadoProtocoloDto Resultado(CentroInvestigaciones.Domain.Protocolo protocolo, string revisor, bool enviado, string avisoFallo) => new()
    {
        Protocolo = MapaProtocolo.Hacia(protocolo, revisor),
        CorreoEnviado = enviado,
        Aviso = enviado ? null : avisoFallo
    };

    private ActionResult Fallo(InvalidOperationException excepcion) =>
        excepcion.Message.Contains("encontró", StringComparison.Ordinal)
            ? NotFound(excepcion.Message)
            : BadRequest(excepcion.Message);

    private string Origen() => $"{Request.Scheme}://{Request.Host}";
}
