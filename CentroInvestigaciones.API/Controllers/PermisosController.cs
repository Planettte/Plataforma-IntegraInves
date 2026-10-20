using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/permisos")]
public sealed class PermisosController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;

    public PermisosController(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    [HttpGet]
    [RequierePermiso(PermisosNucleo.PermisosConsultar)]
    public async Task<ActionResult<IReadOnlyList<PermisoDto>>> Listar()
    {
        var permisos = await _almacen.ListarPermisosAsync(HttpContext.RequestAborted);
        return permisos.Select(permiso => new PermisoDto
        {
            Codigo = permiso.Codigo,
            Nombre = permiso.Nombre
        }).ToList();
    }
}
