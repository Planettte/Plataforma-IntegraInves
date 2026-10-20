using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/captcha")]
public sealed class CaptchaController : ControllerBase
{
    private readonly RetoCaptcha _captcha;

    public CaptchaController(RetoCaptcha captcha)
    {
        _captcha = captcha;
    }

    [HttpGet]
    public ContentResult Imagen() => Content(_captcha.Emitir(HttpContext), "image/svg+xml; charset=utf-8");

    [HttpGet("estado")]
    public ActionResult<EstadoCaptchaDto> Estado() =>
        new EstadoCaptchaDto { Listo = _captcha.Listo(HttpContext) };

    [EnableRateLimiting("envios-publicos")]
    [HttpPost]
    public IActionResult Resolver([FromBody] RespuestaCaptchaDto solicitud) =>
        _captcha.Resolver(HttpContext, solicitud.Respuesta) ? NoContent() : BadRequest();
}
