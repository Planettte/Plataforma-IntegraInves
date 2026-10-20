using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Infraestructura;

internal static class RespuestaOperacion
{
    public static ActionResult De(HttpContext contexto, InvalidOperationException excepcion, string registro)
    {
        contexto.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(registro)
            .LogWarning(excepcion, "La operación no se completó.");
        return excepcion.Message.Contains("encontró", StringComparison.Ordinal)
            ? new NotFoundResult()
            : new BadRequestObjectResult("No se pudo completar la solicitud.");
    }
}
