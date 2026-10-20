using System.Security.Claims;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.HojasVida;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.estudios.hojas-de-vida")]
[Route("api/hojas-de-vida")]
public sealed class HojasVidaController : ControllerBase
{
    private const long TamanoMaximo = 20 * 1024 * 1024;
    private readonly IAlmacenHojasVida _almacen;
    private readonly GeneradorPdfHojaVida _pdf;

    public HojasVidaController(IAlmacenHojasVida almacen, GeneradorPdfHojaVida pdf)
    {
        _almacen = almacen;
        _pdf = pdf;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<HojaVidaResumenDto>> Listar()
    {
        return _almacen.Listar().Select(MapaResumen).ToList();
    }

    [HttpGet("{id:guid}")]
    public ActionResult<HojaVidaDto> Obtener(Guid id)
    {
        var hoja = _almacen.Buscar(id);
        return hoja is null ? NotFound() : Mapa(hoja);
    }

    [HttpGet("{id:guid}/historial")]
    public ActionResult<IReadOnlyList<EventoHojaVidaDto>> Historial(Guid id)
    {
        var hoja = _almacen.Buscar(id);
        if (hoja is null)
        {
            return NotFound();
        }

        return hoja.Historial
            .OrderByDescending(item => item.FechaUtc)
            .Select(item => new EventoHojaVidaDto
            {
                Version = item.Version,
                FechaUtc = item.FechaUtc,
                Usuario = item.Usuario,
                Nota = item.Nota
            })
            .ToList();
    }

    [HttpPost]
    public ActionResult<HojaVidaDto> Crear([FromBody] CrearHojaVidaDto solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NombreCompleto))
        {
            return BadRequest();
        }

        var hoja = _almacen.Crear(solicitud.NombreCompleto, solicitud.TituloAcademico ?? "", Usuario());
        return CreatedAtAction(nameof(Obtener), new { id = hoja.Id }, Mapa(hoja));
    }

    [HttpPut("{id:guid}")]
    public ActionResult<ResultadoGuardadoHojaDto> Actualizar(Guid id, [FromBody] GuardarHojaVidaDto solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NombreCompleto) || _almacen.Buscar(id) is null)
        {
            return string.IsNullOrWhiteSpace(solicitud.NombreCompleto) ? BadRequest() : NotFound();
        }

        try
        {
            var (hoja, nueva) = _almacen.Actualizar(id, Desde(solicitud), Usuario());
            return new ResultadoGuardadoHojaDto { Hoja = Mapa(hoja), VersionNueva = nueva };
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Eliminar(Guid id)
    {
        return _almacen.Eliminar(id) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/soportes")]
    [RequestSizeLimit(TamanoMaximo * 8)]
    public async Task<ActionResult<IReadOnlyList<SoporteHojaDto>>> SubirSoportes(Guid id)
    {
        if (_almacen.Buscar(id) is null)
        {
            return NotFound();
        }

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest();
        }

        foreach (var archivo in Request.Form.Files)
        {
            var tipo = TipoPermitido(archivo.FileName);
            if (tipo is null || archivo.Length == 0 || archivo.Length > TamanoMaximo)
            {
                return BadRequest();
            }

            await using var flujo = archivo.OpenReadStream();
            _almacen.AgregarSoporte(id, archivo.FileName, tipo, flujo, Usuario());
        }

        return _almacen.Buscar(id)!.Soportes.OrderBy(item => item.Orden).Select(MapaSoporte).ToList();
    }

    [HttpPut("{id:guid}/soportes/orden")]
    public IActionResult Ordenar(Guid id, [FromBody] OrdenSoportesDto solicitud)
    {
        if (_almacen.Buscar(id) is null)
        {
            return NotFound();
        }

        _almacen.ReordenarSoportes(id, solicitud.Ids, Usuario());
        return NoContent();
    }

    [HttpDelete("{id:guid}/soportes/{soporteId:guid}")]
    public IActionResult EliminarSoporte(Guid id, Guid soporteId)
    {
        try
        {
            return _almacen.EliminarSoporte(id, soporteId, Usuario()) ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/firma")]
    [RequestSizeLimit(TamanoMaximo)]
    public async Task<IActionResult> SubirFirma(Guid id)
    {
        if (_almacen.Buscar(id) is null)
        {
            return NotFound();
        }

        var archivo = Request.HasFormContentType ? Request.Form.Files.FirstOrDefault() : null;
        if (archivo is null || archivo.Length == 0 || archivo.Length > TamanoMaximo)
        {
            return BadRequest();
        }

        var tipo = TipoPermitido(archivo.FileName);
        if (tipo is null || !tipo.StartsWith("image/", StringComparison.Ordinal))
        {
            return BadRequest();
        }

        var fecha = Request.HasFormContentType ? Request.Form["fecha"].ToString() : "";
        await using var flujo = archivo.OpenReadStream();
        _almacen.GuardarFirma(id, archivo.FileName, tipo, fecha, flujo, Usuario());
        return NoContent();
    }

    [HttpDelete("{id:guid}/firma")]
    public IActionResult QuitarFirma(Guid id)
    {
        try
        {
            return _almacen.QuitarFirma(id, Usuario()) ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/pdf")]
    public IActionResult Pdf(Guid id, [FromBody] SolicitudPdfHojaDto solicitud)
    {
        var hoja = _almacen.Buscar(id);
        if (hoja is null)
        {
            return NotFound();
        }

        var idioma = solicitud.Idioma == "en" ? "en" : "es";
        try
        {
            var bytes = _pdf.Generar(hoja, idioma, solicitud.SoportesIds ?? [], solicitud.Nota);
            var prefijo = idioma == "en" ? "CV" : "HV";
            var nombre = $"{prefijo}_{NombreArchivo(hoja.NombreCompleto)}_{(idioma == "en" ? "EN" : "ES")}_v{hoja.Version}.pdf";
            return File(bytes, "application/pdf", nombre);
        }
        catch (InvalidOperationException excepcion)
        {
            return RespuestaOperacion.De(HttpContext, excepcion, nameof(HojasVidaController));
        }
    }

    private string Usuario() =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? "usuario";

    private static string? TipoPermitido(string nombre)
    {
        return Path.GetExtension(nombre).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => null
        };
    }

    private static string NombreArchivo(string nombre)
    {
        var limpio = string.Join("_", nombre.Split([' ', '\\', '/', ':', '*', '?', '"', '<', '>', '|'], StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(limpio) ? "persona" : limpio;
    }

    private static HojaVida Desde(GuardarHojaVidaDto solicitud) => new()
    {
        NombreCompleto = solicitud.NombreCompleto,
        TituloAcademico = solicitud.TituloAcademico,
        DirInstitucion = solicitud.DirInstitucion,
        DirCompleta = solicitud.DirCompleta,
        DirCiudad = solicitud.DirCiudad,
        DirEstado = solicitud.DirEstado,
        DirProvincia = solicitud.DirProvincia,
        DirPais = solicitud.DirPais,
        DirCodigo = solicitud.DirCodigo,
        Telefono = solicitud.Telefono,
        Extension = solicitud.Extension,
        VinculacionModalidad = solicitud.VinculacionModalidad,
        VinculacionFechaInicio = solicitud.VinculacionFechaInicio,
        Formacion = solicitud.Formacion.Select(item => new FormacionHoja
        {
            Institucion = item.Institucion,
            Titulo = item.Titulo,
            Anio = item.Anio,
            Area = item.Area
        }).ToList(),
        ExperienciaLaboral = solicitud.ExperienciaLaboral.Select(item => new LaboralHoja
        {
            Posicion = item.Posicion,
            Institucion = item.Institucion,
            Locacion = item.Locacion,
            FechaInicio = item.FechaInicio,
            FechaFin = item.FechaFin
        }).ToList(),
        ExperienciaInvestigacion = solicitud.ExperienciaInvestigacion.Select(item => new InvestigacionHoja
        {
            Codigo = item.Codigo,
            Indicacion = item.Indicacion,
            Fase = item.Fase,
            Papel = item.Papel,
            Anio = item.Anio
        }).ToList(),
        GcpSiNo = solicitud.GcpSiNo,
        GcpDetalle = solicitud.GcpDetalle,
        OtrosSiNo = solicitud.OtrosSiNo,
        OtrosDetalle = solicitud.OtrosDetalle,
        FirmaFecha = solicitud.FirmaFecha
    };

    private static HojaVidaResumenDto MapaResumen(HojaVida hoja) => new()
    {
        Id = hoja.Id,
        NombreCompleto = hoja.NombreCompleto,
        TituloAcademico = hoja.TituloAcademico,
        Version = hoja.Version,
        FechaEdicionUtc = hoja.FechaEdicionUtc
    };

    private static HojaVidaDto Mapa(HojaVida hoja) => new()
    {
        Id = hoja.Id,
        NombreCompleto = hoja.NombreCompleto,
        TituloAcademico = hoja.TituloAcademico,
        DirInstitucion = hoja.DirInstitucion,
        DirCompleta = hoja.DirCompleta,
        DirCiudad = hoja.DirCiudad,
        DirEstado = hoja.DirEstado,
        DirProvincia = hoja.DirProvincia,
        DirPais = hoja.DirPais,
        DirCodigo = hoja.DirCodigo,
        Telefono = hoja.Telefono,
        Extension = hoja.Extension,
        VinculacionModalidad = hoja.VinculacionModalidad,
        VinculacionFechaInicio = hoja.VinculacionFechaInicio,
        Formacion = hoja.Formacion.Select(item => new FormacionHojaDto
        {
            Institucion = item.Institucion,
            Titulo = item.Titulo,
            Anio = item.Anio,
            Area = item.Area
        }).ToList(),
        ExperienciaLaboral = hoja.ExperienciaLaboral.Select(item => new LaboralHojaDto
        {
            Posicion = item.Posicion,
            Institucion = item.Institucion,
            Locacion = item.Locacion,
            FechaInicio = item.FechaInicio,
            FechaFin = item.FechaFin
        }).ToList(),
        ExperienciaInvestigacion = hoja.ExperienciaInvestigacion.Select(item => new InvestigacionHojaDto
        {
            Codigo = item.Codigo,
            Indicacion = item.Indicacion,
            Fase = item.Fase,
            Papel = item.Papel,
            Anio = item.Anio
        }).ToList(),
        GcpSiNo = hoja.GcpSiNo,
        GcpDetalle = hoja.GcpDetalle,
        OtrosSiNo = hoja.OtrosSiNo,
        OtrosDetalle = hoja.OtrosDetalle,
        TieneFirma = !string.IsNullOrWhiteSpace(hoja.FirmaRuta),
        FirmaNombre = hoja.FirmaNombre,
        FirmaFecha = hoja.FirmaFecha,
        Version = hoja.Version,
        FechaEdicionUtc = hoja.FechaEdicionUtc,
        Soportes = hoja.Soportes.OrderBy(item => item.Orden).Select(MapaSoporte).ToList()
    };

    private static SoporteHojaDto MapaSoporte(SoporteHoja soporte) => new()
    {
        Id = soporte.Id,
        Nombre = soporte.Nombre,
        Tipo = soporte.Tipo,
        Orden = soporte.Orden
    };
}
