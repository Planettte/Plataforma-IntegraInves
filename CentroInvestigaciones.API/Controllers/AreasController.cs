using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/areas")]
public sealed class AreasController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;

    public AreasController(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AreaDto>>> Listar()
    {
        var areas = await _almacen.ListarAreasAsync(HttpContext.RequestAborted);
        return areas.Select(area => new AreaDto
        {
            Id = area.Id,
            Codigo = area.Codigo,
            Nombre = area.Nombre
        }).ToList();
    }
}
