using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IAlmacenNucleo _almacen;

    public RolesController(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    [HttpGet]
    [RequierePermiso(PermisosNucleo.RolesConsultar)]
    public async Task<ActionResult<IReadOnlyList<RolDto>>> Listar()
    {
        var roles = await _almacen.ListarRolesAsync(HttpContext.RequestAborted);
        return roles.Select(rol => new RolDto
        {
            Id = rol.Id,
            Codigo = rol.Codigo,
            Nombre = rol.Nombre,
            Permisos = rol.Permisos.Select(vinculo => vinculo.Permiso.Codigo).OrderBy(codigo => codigo).ToList()
        }).ToList();
    }
}
