using System.Security.Claims;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Farmacia;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.estudios.inventario-farmacia")]
[Route("api/farmacia")]
public sealed class FarmaciaController : ControllerBase
{
    private readonly IAlmacenFarmacia _almacen;
    private readonly GeneradorPdfFarmacia _pdf;

    public FarmaciaController(IAlmacenFarmacia almacen, GeneradorPdfFarmacia pdf)
    {
        _almacen = almacen;
        _pdf = pdf;
    }

    [HttpGet("formatos")]
    public ActionResult<List<FormatoFarmaciaDto>> ListarFormatos() =>
        _almacen.ListarFormatos().Select(Mapa).ToList();

    [HttpGet("formatos/{id:guid}")]
    public ActionResult<FormatoFarmaciaDto> ObtenerFormato(Guid id)
    {
        var formato = FormatoActivo(id);
        return formato is null ? NotFound() : Mapa(formato);
    }

    [HttpPost("formatos")]
    public ActionResult<FormatoFarmaciaDto> CrearFormato([FromBody] FormatoFarmaciaDto solicitud)
    {
        if (!Valido(solicitud, out var error))
        {
            return BadRequest(error);
        }

        solicitud.Id = Guid.Empty;
        try
        {
            return Mapa(_almacen.GuardarFormato(Dominio(solicitud)));
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(excepcion.Message);
        }
    }

    [HttpPut("formatos/{id:guid}")]
    public ActionResult<FormatoFarmaciaDto> ActualizarFormato(Guid id, [FromBody] FormatoFarmaciaDto _)
    {
        if (FormatoActivo(id) is null)
        {
            return NotFound();
        }

        return BadRequest("El formato está cerrado.");
    }

    [HttpDelete("formatos/{id:guid}")]
    public IActionResult EliminarFormato(Guid id) =>
        _almacen.DesactivarFormato(id, UsuarioId(), NombreUsuario()) ? NoContent() : NotFound();

    [HttpPost("formatos/{id:guid}/pdf")]
    public IActionResult Pdf(Guid id)
    {
        var formato = FormatoActivo(id);
        if (formato is null)
        {
            return NotFound();
        }

        var nombre = string.IsNullOrWhiteSpace(formato.NumeroEstudio) ? "contabilidad" : formato.NumeroEstudio.Trim();
        foreach (var caracter in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(caracter, '-');
        }

        return File(_pdf.Generar(formato), "application/pdf", $"contabilidad-{nombre}.pdf");
    }

    [HttpGet("inventario")]
    public ActionResult<List<FilaFarmaciaDto>> ListarInventario() =>
        _almacen.ListarInventario().Select(Mapa).ToList();

    [HttpPost("inventario")]
    public ActionResult<FilaFarmaciaDto> CrearInventario([FromBody] FilaFarmaciaDto solicitud)
    {
        if (!FilaValida(solicitud, out var error))
        {
            return BadRequest(error);
        }

        solicitud.Id = Guid.Empty;
        return Mapa(_almacen.GuardarInventario(Dominio(solicitud)));
    }

    [HttpPut("inventario/{id:guid}")]
    public ActionResult<FilaFarmaciaDto> ActualizarInventario(Guid id, [FromBody] FilaFarmaciaDto solicitud)
    {
        if (_almacen.ListarInventario().All(item => item.Id != id))
        {
            return NotFound();
        }

        if (!FilaValida(solicitud, out var error))
        {
            return BadRequest(error);
        }

        solicitud.Id = id;
        return Mapa(_almacen.GuardarInventario(Dominio(solicitud)));
    }

    [HttpDelete("inventario/{id:guid}")]
    public IActionResult EliminarInventario(Guid id) =>
        _almacen.EliminarInventario(id) ? NoContent() : NotFound();

    private static bool Valido(FormatoFarmaciaDto solicitud, out string error)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NumeroEstudio) && string.IsNullOrWhiteSpace(solicitud.DescripcionMedicamento))
        {
            error = "Indica el número del estudio o la descripción del medicamento.";
            return false;
        }

        foreach (var fila in solicitud.Filas)
        {
            if (!FechasValidas(fila, out error))
            {
                return false;
            }
        }

        error = "";
        return true;
    }

    private FormatoFarmacia? FormatoActivo(Guid id)
    {
        var formato = _almacen.BuscarFormato(id);
        return formato is { Activo: true } ? formato : null;
    }

    private Guid? UsuarioId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string NombreUsuario()
    {
        var nombre = User.FindFirstValue(ClaimTypes.Name);
        return string.IsNullOrWhiteSpace(nombre) ? "usuario" : nombre.Trim();
    }

    private static bool FilaValida(FilaFarmaciaDto fila, out string error)
    {
        if (string.IsNullOrWhiteSpace(fila.CodigoMedicamento) && string.IsNullOrWhiteSpace(fila.Lote))
        {
            error = "Indica el código del medicamento o el lote.";
            return false;
        }

        return FechasValidas(fila, out error);
    }

    private static bool FechasValidas(FilaFarmaciaDto fila, out string error)
    {
        foreach (var fecha in new[] { fila.FechaRecibido, fila.FechaVencimiento, fila.FechaEntrega, fila.FechaRetorno, fila.FechaDisposicion })
        {
            if (!string.IsNullOrWhiteSpace(fecha) && !DateOnly.TryParse(fecha, out _))
            {
                error = "Una fecha no es válida.";
                return false;
            }
        }

        error = "";
        return true;
    }

    private static FormatoFarmacia Dominio(FormatoFarmaciaDto dto) => new()
    {
        Id = dto.Id,
        NumeroEstudio = dto.NumeroEstudio ?? "",
        Patrocinador = dto.Patrocinador ?? "",
        Coordinador = dto.Coordinador ?? "",
        NumeroCentro = dto.NumeroCentro ?? "",
        Titulo = dto.Titulo ?? "",
        InvestigadorPrincipal = dto.InvestigadorPrincipal ?? "",
        CondicionesAlmacenamiento = dto.CondicionesAlmacenamiento ?? "",
        DescripcionMedicamento = dto.DescripcionMedicamento ?? "",
        Dosis = dto.Dosis ?? "",
        Observaciones = dto.Observaciones ?? "",
        ResponsableFarmacia = dto.ResponsableFarmacia ?? "",
        FirmaCierre = dto.FirmaCierre ?? "",
        CantidadFinal = dto.CantidadFinal ?? "",
        Filas = (dto.Filas ?? []).Select(Dominio).ToList()
    };

    private static FilaFarmacia Dominio(FilaFarmaciaDto dto) => new()
    {
        Id = dto.Id,
        NumeroEnvio = dto.NumeroEnvio ?? "",
        FechaRecibido = dto.FechaRecibido ?? "",
        Lote = dto.Lote ?? "",
        FechaVencimiento = dto.FechaVencimiento ?? "",
        CodigoMedicamento = dto.CodigoMedicamento ?? "",
        Conforme = dto.Conforme ?? "",
        SujetoCodigo = dto.SujetoCodigo ?? "",
        FechaEntrega = dto.FechaEntrega ?? "",
        Entrega = dto.Entrega ?? "",
        Recibe = dto.Recibe ?? "",
        FechaRetorno = dto.FechaRetorno ?? "",
        Empaque = dto.Empaque ?? "",
        Unidades = dto.Unidades ?? "",
        EntregaRetorno = dto.EntregaRetorno ?? "",
        RecibeRetorno = dto.RecibeRetorno ?? "",
        ControlMonitoria = dto.ControlMonitoria ?? "",
        FechaDisposicion = dto.FechaDisposicion ?? "",
        RetornoPatrocinador = dto.RetornoPatrocinador ?? "",
        DestruccionActa = dto.DestruccionActa ?? ""
    };

    private static FormatoFarmaciaDto Mapa(FormatoFarmacia formato) => new()
    {
        Id = formato.Id,
        NumeroEstudio = formato.NumeroEstudio,
        Patrocinador = formato.Patrocinador,
        Coordinador = formato.Coordinador,
        NumeroCentro = formato.NumeroCentro,
        Titulo = formato.Titulo,
        InvestigadorPrincipal = formato.InvestigadorPrincipal,
        CondicionesAlmacenamiento = formato.CondicionesAlmacenamiento,
        DescripcionMedicamento = formato.DescripcionMedicamento,
        Dosis = formato.Dosis,
        Observaciones = formato.Observaciones,
        ResponsableFarmacia = formato.ResponsableFarmacia,
        FirmaCierre = formato.FirmaCierre,
        CantidadFinal = formato.CantidadFinal,
        GeneradoEl = formato.GeneradoEl,
        Filas = formato.Filas.Select(Mapa).ToList()
    };

    private static FilaFarmaciaDto Mapa(FilaFarmacia fila) => new()
    {
        Id = fila.Id,
        NumeroEnvio = fila.NumeroEnvio,
        FechaRecibido = fila.FechaRecibido,
        Lote = fila.Lote,
        FechaVencimiento = fila.FechaVencimiento,
        CodigoMedicamento = fila.CodigoMedicamento,
        Conforme = fila.Conforme,
        SujetoCodigo = fila.SujetoCodigo,
        FechaEntrega = fila.FechaEntrega,
        Entrega = fila.Entrega,
        Recibe = fila.Recibe,
        FechaRetorno = fila.FechaRetorno,
        Empaque = fila.Empaque,
        Unidades = fila.Unidades,
        EntregaRetorno = fila.EntregaRetorno,
        RecibeRetorno = fila.RecibeRetorno,
        ControlMonitoria = fila.ControlMonitoria,
        FechaDisposicion = fila.FechaDisposicion,
        RetornoPatrocinador = fila.RetornoPatrocinador,
        DestruccionActa = fila.DestruccionActa
    };
}
