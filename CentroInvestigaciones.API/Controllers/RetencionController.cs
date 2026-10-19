using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.estudios.retencion-documental")]
[Route("api/retencion")]
public sealed class RetencionController : ControllerBase
{
    private readonly GeneradorPdfRetencion _pdf;

    public RetencionController(GeneradorPdfRetencion pdf)
    {
        _pdf = pdf;
    }

    [HttpPost("pdf")]
    public IActionResult Pdf([FromBody] RetencionDocumentalDto solicitud)
    {
        solicitud ??= new RetencionDocumentalDto();
        solicitud.NumeroEstudio = (solicitud.NumeroEstudio ?? "").Trim();
        solicitud.Patrocinador = (solicitud.Patrocinador ?? "").Trim();
        solicitud.Contenido = (solicitud.Contenido ?? []).Select(valor => (valor ?? "").Trim()).Take(4).ToList();
        solicitud.Direcciones ??= [];
        solicitud.Prestamos ??= [];

        foreach (var fila in solicitud.Prestamos)
        {
            if (fila.FirmaSolicitante.Length > 2_000_000 || fila.FirmaRecibe.Length > 2_000_000)
            {
                return BadRequest("La firma es demasiado grande.");
            }
        }

        var nombre = "Tabla_Retencion_Documental";
        if (!string.IsNullOrWhiteSpace(solicitud.NumeroEstudio))
        {
            var estudio = solicitud.NumeroEstudio;
            foreach (var caracter in Path.GetInvalidFileNameChars())
            {
                estudio = estudio.Replace(caracter, '-');
            }

            nombre += "_" + estudio;
        }

        return File(_pdf.Generar(solicitud), "application/pdf", $"{nombre}.pdf");
    }
}
