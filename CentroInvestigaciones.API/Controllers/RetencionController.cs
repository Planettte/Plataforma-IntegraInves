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
        solicitud.Direcciones = (solicitud.Direcciones ?? [])
            .Where(fila => !DireccionVacia(fila))
            .ToList();
        solicitud.Prestamos = (solicitud.Prestamos ?? [])
            .Where(fila => !PrestamoVacio(fila))
            .ToList();

        foreach (var fila in solicitud.Direcciones)
        {
            fila.Fecha1 = (fila.Fecha1 ?? "").Trim();
            fila.Direccion1 = (fila.Direccion1 ?? "").Trim();
            fila.Fecha2 = (fila.Fecha2 ?? "").Trim();
            fila.Direccion2 = (fila.Direccion2 ?? "").Trim();
            if (LadoIncompleto(fila.Fecha1, fila.Direccion1) || LadoIncompleto(fila.Fecha2, fila.Direccion2))
            {
                return BadRequest("Completa la fecha y la dirección.");
            }
        }

        foreach (var fila in solicitud.Prestamos)
        {
            fila.FechaPrestamo = (fila.FechaPrestamo ?? "").Trim();
            fila.NombreDocumento = (fila.NombreDocumento ?? "").Trim();
            fila.Motivo = (fila.Motivo ?? "").Trim();
            fila.NombreSolicitante = (fila.NombreSolicitante ?? "").Trim();
            fila.FechaDevolucion = (fila.FechaDevolucion ?? "").Trim();
            fila.FirmaSolicitante ??= "";
            fila.FirmaRecibe ??= "";
            if (string.IsNullOrWhiteSpace(fila.NombreDocumento)
                || string.IsNullOrWhiteSpace(fila.Motivo)
                || string.IsNullOrWhiteSpace(fila.NombreSolicitante))
            {
                return BadRequest("El préstamo debe indicar el documento, el motivo y el solicitante.");
            }

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

    private static bool DireccionVacia(DireccionRetencionDto fila) =>
        Blanco(fila.Fecha1) && Blanco(fila.Direccion1) && Blanco(fila.Fecha2) && Blanco(fila.Direccion2);

    private static bool PrestamoVacio(PrestamoRetencionDto fila) =>
        Blanco(fila.FechaPrestamo)
        && Blanco(fila.NombreDocumento)
        && Blanco(fila.Motivo)
        && Blanco(fila.NombreSolicitante)
        && Blanco(fila.FirmaSolicitante)
        && Blanco(fila.FechaDevolucion)
        && Blanco(fila.FirmaRecibe);

    private static bool LadoIncompleto(string fecha, string direccion) =>
        Blanco(fecha) != Blanco(direccion);

    private static bool Blanco(string? valor) => string.IsNullOrWhiteSpace(valor);
}
