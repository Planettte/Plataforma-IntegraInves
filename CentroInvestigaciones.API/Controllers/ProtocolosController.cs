using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Application.Protocolos;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso(RevisoresProtocolo.Permiso)]
[Route("api/protocolos")]
public sealed class ProtocolosController : ControllerBase
{
    private readonly IAlmacenProtocolos _almacen;
    private readonly IAlmacenNucleo _nucleo;
    private readonly AvisosProtocolo _avisos;
    private readonly GeneradorPdfProtocolos _pdf;

    public ProtocolosController(IAlmacenProtocolos almacen, IAlmacenNucleo nucleo, AvisosProtocolo avisos, GeneradorPdfProtocolos pdf)
    {
        _almacen = almacen;
        _nucleo = nucleo;
        _avisos = avisos;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProtocoloListaDto>>> Listar(CancellationToken ct)
    {
        var protocolos = _almacen.Listar();
        var nombres = await _nucleo.NombresUsuariosAsync(protocolos.Select(item => item.RevisorId).Distinct(), ct);
        return protocolos.Select(item => new ProtocoloListaDto
        {
            Id = item.Id,
            Nombre = item.Nombre,
            Correo = item.Correo,
            Formato = item.Formato,
            RevisorId = item.RevisorId,
            RevisorNombre = nombres.GetValueOrDefault(item.RevisorId) ?? "",
            Estado = item.Estado,
            CreadoEl = item.CreadoEl
        }).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProtocoloDto>> Obtener(Guid id, CancellationToken ct)
    {
        var protocolo = _almacen.Buscar(id);
        if (protocolo is null)
        {
            return NotFound();
        }

        var (nombre, _) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
        return MapaProtocolo.Hacia(protocolo, nombre);
    }

    [HttpPut("{id:guid}/correccion")]
    public async Task<ActionResult<ResultadoProtocoloDto>> Corregir(Guid id, [FromBody] CorreccionProtocoloDto solicitud, CancellationToken ct)
    {
        try
        {
            var protocolo = _almacen.Corregir(id, solicitud.Comentarios);
            var enviado = await _avisos.CorreccionAsync(protocolo, Origen(), ct);
            var (nombre, _) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
            return Resultado(protocolo, nombre, enviado, "Error al notificar al usuario. La corrección sí quedó guardada.");
        }
        catch (InvalidOperationException excepcion)
        {
            return Fallo(excepcion);
        }
    }

    [HttpPost("{id:guid}/aceptar")]
    public async Task<ActionResult<ResultadoProtocoloDto>> Aceptar(Guid id, CancellationToken ct)
    {
        try
        {
            var protocolo = _almacen.Aceptar(id);
            var enviado = await _avisos.AprobadoAsync(protocolo, ct);
            var (nombre, _) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
            return Resultado(protocolo, nombre, enviado, "Error al notificar al usuario. La aceptación sí quedó guardada.");
        }
        catch (InvalidOperationException excepcion)
        {
            return Fallo(excepcion);
        }
    }

    [HttpPost("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var protocolo = _almacen.Buscar(id);
        if (protocolo is null)
        {
            return NotFound();
        }

        var (nombreRevisor, _) = await RevisoresProtocolo.DatosAsync(_nucleo, protocolo.RevisorId, ct);
        var dto = MapaProtocolo.Hacia(protocolo, nombreRevisor);
        var nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? dto.Formato : dto.Nombre.Trim();
        foreach (var caracter in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(caracter, '-');
        }

        return File(_pdf.Generar(dto), "application/pdf", $"Protocolo_{dto.Formato}_{nombre}.pdf");
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
