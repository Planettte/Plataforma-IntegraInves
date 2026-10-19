using System.Security.Claims;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CentroInvestigaciones.API.Autorizacion;

public sealed class FiltroPermiso : IAsyncActionFilter
{
    private readonly IAlmacenNucleo _almacen;

    public FiltroPermiso(IAlmacenNucleo almacen)
    {
        _almacen = almacen;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var requisito = contexto.ActionDescriptor.EndpointMetadata
            .OfType<RequierePermisoAttribute>()
            .FirstOrDefault();

        if (requisito is null)
        {
            await siguiente();
            return;
        }

        if (contexto.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            contexto.Result = new UnauthorizedResult();
            return;
        }

        var identificador = contexto.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador) || !_almacen.Disponible)
        {
            contexto.Result = new ForbidResult();
            return;
        }

        var usuario = await _almacen.BuscarPorProveedorAsync(identificador, contexto.HttpContext.RequestAborted);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo)
        {
            contexto.Result = new ForbidResult();
            return;
        }

        var permisos = await _almacen.PermisosDeUsuarioAsync(usuario.Id, contexto.HttpContext.RequestAborted);
        if (!permisos.Contains(requisito.Codigo, StringComparer.Ordinal))
        {
            contexto.Result = new ForbidResult();
            return;
        }

        await siguiente();
    }
}
