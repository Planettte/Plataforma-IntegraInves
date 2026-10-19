using System.Net;
using System.Security.Claims;
using CentroInvestigaciones.API.Autorizacion;
using CentroInvestigaciones.API.Infraestructura;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Application.Plantillas;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroInvestigaciones.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso("herramientas.estudios.plantillas")]
[Route("api/plantillas")]
public sealed class PlantillasController : ControllerBase
{
    private readonly IAlmacenPlantillas _almacen;
    private readonly IFirmasInvestigador _firmas;
    private readonly GeneradorPdfCarta _pdf;
    private readonly IEnviadorCorreo _correo;
    private readonly IAlmacenNucleo _nucleo;

    public PlantillasController(
        IAlmacenPlantillas almacen,
        IFirmasInvestigador firmas,
        GeneradorPdfCarta pdf,
        IEnviadorCorreo correo,
        IAlmacenNucleo nucleo)
    {
        _almacen = almacen;
        _firmas = firmas;
        _pdf = pdf;
        _correo = correo;
        _nucleo = nucleo;
    }

    [HttpGet("investigadores")]
    public ActionResult<IReadOnlyList<InvestigadorPlantillaDto>> Investigadores() =>
        _almacen.Investigadores().Select(Mapa).ToList();

    [HttpPost("investigadores")]
    public async Task<ActionResult<InvestigadorPlantillaDto>> CrearInvestigador([FromBody] NombrePlantillaDto solicitud)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Nombre) || string.IsNullOrWhiteSpace(solicitud.Titulo))
        {
            return BadRequest(new MensajeDto { Mensaje = "Escribe el nombre y el título del investigador." });
        }

        if (Repetido(_almacen.Investigadores().Select(item => Carpeta(item)), Carpeta(solicitud.Nombre, solicitud.Titulo)))
        {
            return BadRequest(new MensajeDto { Mensaje = "Ese investigador ya está en la lista." });
        }

        var persona = _almacen.AgregarInvestigador(solicitud.Nombre, solicitud.Titulo);
        _firmas.Asegurar(persona.Nombre, persona.Titulo);
        return Mapa(persona);
    }

    [HttpPut("investigadores/{id:guid}")]
    public async Task<ActionResult<InvestigadorPlantillaDto>> ActualizarInvestigador(Guid id, [FromBody] NombrePlantillaDto solicitud)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Nombre) || string.IsNullOrWhiteSpace(solicitud.Titulo))
        {
            return BadRequest(new MensajeDto { Mensaje = "Escribe el nombre y el título del investigador." });
        }

        var anterior = _almacen.Investigadores().FirstOrDefault(item => item.Id == id);
        if (anterior is null)
        {
            return NotFound();
        }

        var persona = _almacen.ActualizarInvestigador(id, solicitud.Nombre, solicitud.Titulo);
        if (persona is null)
        {
            return NotFound();
        }

        _firmas.Renombrar(anterior.Nombre, anterior.Titulo, persona.Nombre, persona.Titulo);
        return Mapa(persona);
    }

    [HttpDelete("investigadores/{id:guid}")]
    public async Task<IActionResult> EliminarInvestigador(Guid id)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        var anterior = _almacen.Investigadores().FirstOrDefault(item => item.Id == id);
        if (!_almacen.EliminarInvestigador(id) || anterior is null)
        {
            return BadRequest(new MensajeDto { Mensaje = "No se puede quitar: está asignado a un estudio o ya no existe." });
        }

        _firmas.Eliminar(anterior.Nombre, anterior.Titulo);
        return NoContent();
    }

    [HttpGet("coordinadores")]
    public ActionResult<IReadOnlyList<CoordinadorPlantillaDto>> Coordinadores() =>
        _almacen.Coordinadores().Select(Mapa).ToList();

    [HttpPost("coordinadores")]
    public async Task<ActionResult<CoordinadorPlantillaDto>> CrearCoordinador([FromBody] CoordinadorAltaDto solicitud)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Nombre) || !CorreoValido(solicitud.Correo))
        {
            return BadRequest(new MensajeDto { Mensaje = "Escribe el nombre y un correo válido." });
        }

        if (solicitud.Nombre.Trim().Equals(PlantillasFijas.CoordinadorBackup, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new MensajeDto { Mensaje = "El coordinador backup ya existe." });
        }

        return Mapa(_almacen.AgregarCoordinador(solicitud.Nombre, solicitud.Correo));
    }

    [HttpPut("coordinadores/{id:guid}")]
    public async Task<ActionResult<CoordinadorPlantillaDto>> ActualizarCoordinador(Guid id, [FromBody] CoordinadorAltaDto solicitud)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.Nombre) || !CorreoValido(solicitud.Correo))
        {
            return BadRequest(new MensajeDto { Mensaje = "Escribe el nombre y un correo válido." });
        }

        var persona = _almacen.ActualizarCoordinador(id, solicitud.Nombre, solicitud.Correo);
        return persona is null ? NotFound() : Mapa(persona);
    }

    [HttpDelete("coordinadores/{id:guid}")]
    public async Task<IActionResult> EliminarCoordinador(Guid id)
    {
        if (!await PuedeAdministrarListasAsync())
        {
            return Forbid();
        }

        return _almacen.EliminarCoordinador(id)
            ? NoContent()
            : BadRequest(new MensajeDto { Mensaje = "No se puede quitar: es el coordinador backup, está asignado a un estudio o ya no existe." });
    }

    [HttpGet("cartas")]
    public ActionResult<IReadOnlyList<CartaCatalogoDto>> Cartas() => LectorCartas.Catalogo().ToList();

    [HttpGet("estudios")]
    public ActionResult<IReadOnlyList<EstudioPlantillaDto>> Estudios() =>
        _almacen.Estudios().Select(Mapa).ToList();

    [HttpGet("estudios/{id:guid}")]
    public ActionResult<EstudioPlantillaDto> Estudio(Guid id)
    {
        var estudio = _almacen.BuscarEstudio(id);
        return estudio is null ? NotFound() : Mapa(estudio);
    }

    [HttpPost("estudios")]
    public ActionResult<EstudioPlantillaDto> CrearEstudio([FromBody] GuardarEstudioPlantillaDto solicitud)
    {
        var error = Validar(solicitud);
        if (error is not null)
        {
            return BadRequest(new MensajeDto { Mensaje = error });
        }

        var estudio = _almacen.CrearEstudio(Desde(solicitud));
        return CreatedAtAction(nameof(Estudio), new { id = estudio.Id }, Mapa(estudio));
    }

    [HttpPut("estudios/{id:guid}")]
    public ActionResult<EstudioPlantillaDto> ActualizarEstudio(Guid id, [FromBody] GuardarEstudioPlantillaDto solicitud)
    {
        var error = Validar(solicitud);
        if (error is not null)
        {
            return BadRequest(new MensajeDto { Mensaje = error });
        }

        var estudio = _almacen.ActualizarEstudio(id, Desde(solicitud));
        return estudio is null ? NotFound() : Mapa(estudio);
    }

    [HttpDelete("estudios/{id:guid}")]
    public IActionResult EliminarEstudio(Guid id) =>
        _almacen.EliminarEstudio(id) ? NoContent() : NotFound();

    [HttpPut("estudios/{id:guid}/revisadas")]
    public ActionResult<EstudioPlantillaDto> Revisadas(Guid id, [FromBody] RevisadasPlantillaDto solicitud)
    {
        var validos = LectorCartas.Catalogo().Select(item => item.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var codigos = (solicitud.Codigos ?? []).Where(validos.Contains).ToList();
        var estudio = _almacen.GuardarRevisadas(id, codigos);
        return estudio is null ? NotFound() : Mapa(estudio);
    }

    [HttpGet("estudios/{id:guid}/cartas/{codigo}")]
    public ActionResult<VistaCartaDto> Carta(Guid id, string codigo)
    {
        var estudio = _almacen.BuscarEstudio(id);
        if (estudio is null)
        {
            return NotFound();
        }

        var vista = ArmarVista(estudio, codigo);
        return vista is null ? NotFound() : vista;
    }

    [HttpPost("estudios/{id:guid}/cartas/{codigo}/pdf")]
    public IActionResult Pdf(Guid id, string codigo, [FromBody] GenerarCartaDto solicitud)
    {
        var estudio = _almacen.BuscarEstudio(id);
        var carta = LectorCartas.Buscar(codigo);
        if (estudio is null || carta is null)
        {
            return NotFound();
        }

        var pdf = _pdf.Generar(LectorCartas.Lineas(codigo), CamposDeCarta(estudio, solicitud.Firmante, solicitud.Campos), Firma(estudio, solicitud.Firmante, solicitud.ConFirma), solicitud.Firmante);
        return File(pdf, "application/pdf", NombreArchivo(carta.Nombre));
    }

    [HttpPost("estudios/{id:guid}/cartas/{codigo}/enviar")]
    public async Task<IActionResult> Enviar(Guid id, string codigo, [FromBody] EnviarCartaDto solicitud, CancellationToken ct)
    {
        var estudio = _almacen.BuscarEstudio(id);
        var carta = LectorCartas.Buscar(codigo);
        if (estudio is null || carta is null)
        {
            return NotFound();
        }

        if (!solicitud.AlPrincipal && !solicitud.AlApoyo)
        {
            return BadRequest(new MensajeDto { Mensaje = "Elige al menos un coordinador." });
        }

        if (!_correo.EstaConfigurado)
        {
            return BadRequest(new MensajeDto { Mensaje = "El correo de la plataforma no está configurado." });
        }

        var destinos = Destinos(estudio, solicitud.AlPrincipal, solicitud.AlApoyo);
        if (destinos.Error is not null)
        {
            return BadRequest(new MensajeDto { Mensaje = destinos.Error });
        }

        var pdf = _pdf.Generar(LectorCartas.Lineas(codigo), CamposDeCarta(estudio, solicitud.Firmante, solicitud.Campos), Firma(estudio, solicitud.Firmante, solicitud.ConFirma), solicitud.Firmante);
        var archivo = NombreArchivo(carta.Nombre);
        var asunto = $"{carta.Nombre} — {estudio.Nombre}";
        foreach (var destino in destinos.Correos)
        {
            var html = PlantillaCorreo.Armar(
                carta.Nombre,
                destino.Nombre,
                $"<p>Adjuntamos la carta <strong>{WebUtility.HtmlEncode(carta.Nombre)}</strong> del estudio <strong>{WebUtility.HtmlEncode(estudio.Nombre)}</strong>.</p>");
            var enviado = await _correo.EnviarAsync(destino.Correo, asunto, html, (archivo, pdf, "application/pdf"), ct);
            if (!enviado)
            {
                return BadRequest(new MensajeDto { Mensaje = $"No se pudo enviar el correo a {destino.Correo}." });
            }
        }

        return Ok(new MensajeDto { Mensaje = "Enviado" });
    }

    private VistaCartaDto? ArmarVista(EstudioDePlantillas estudio, string codigo)
    {
        var principal = BuscarInvestigador(estudio.InvestigadorPrincipalId);
        var sub = BuscarInvestigador(estudio.SubinvestigadorId);
        return CartasPlantilla.Vista(
            estudio,
            codigo,
            principal?.Nombre ?? "",
            principal?.Titulo ?? "",
            sub?.Nombre ?? "",
            sub?.Titulo ?? "");
    }

    private byte[]? Firma(EstudioDePlantillas estudio, string? firmante, bool conFirma)
    {
        if (!conFirma)
        {
            return null;
        }

        var persona = BuscarInvestigador(string.Equals(firmante, "sub", StringComparison.OrdinalIgnoreCase)
            ? estudio.SubinvestigadorId
            : estudio.InvestigadorPrincipalId);
        return persona is null ? null : _firmas.Buscar(persona.Nombre, persona.Titulo);
    }

    private Dictionary<string, string> CamposDeCarta(EstudioDePlantillas estudio, string? firmante, Dictionary<string, string>? campos)
    {
        var resultado = new Dictionary<string, string>(campos ?? [], StringComparer.Ordinal);
        var persona = BuscarInvestigador(string.Equals(firmante, "sub", StringComparison.OrdinalIgnoreCase)
            ? estudio.SubinvestigadorId
            : estudio.InvestigadorPrincipalId);
        resultado["codigo"] = estudio.Codigo;
        resultado["patrocinador"] = estudio.Patrocinador;
        resultado["investigador"] = persona?.Nombre ?? "";
        resultado["especialidad"] = persona?.Titulo ?? "";
        return resultado;
    }

    private InvestigadorPlantilla? BuscarInvestigador(Guid? id) =>
        id is null ? null : _almacen.Investigadores().FirstOrDefault(item => item.Id == id);

    private (List<(string Nombre, string Correo)> Correos, string? Error) Destinos(EstudioDePlantillas estudio, bool principal, bool apoyo)
    {
        var lista = new List<(string Nombre, string Correo)>();
        if (principal)
        {
            var persona = BuscarCoordinador(estudio.CoordinadorPrincipalId);
            if (persona is null || !CorreoValido(persona.Correo))
            {
                return ([], "El coordinador principal no tiene un correo.");
            }

            lista.Add((persona.Nombre, persona.Correo.Trim()));
        }

        if (apoyo)
        {
            var persona = _almacen.Coordinadores().FirstOrDefault(item =>
                item.Nombre.Equals(PlantillasFijas.CoordinadorBackup, StringComparison.OrdinalIgnoreCase))
                ?? BuscarCoordinador(estudio.CoordinadorApoyoId);
            if (persona is null || !CorreoValido(persona.Correo))
            {
                return ([], "El coordinador backup no tiene un correo. Un administrador debe registrarlo.");
            }

            if (lista.All(item => !string.Equals(item.Correo, persona.Correo.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                lista.Add((persona.Nombre, persona.Correo.Trim()));
            }
        }

        return (lista, null);
    }

    private CoordinadorPlantilla? BuscarCoordinador(Guid? id) =>
        id is null ? null : _almacen.Coordinadores().FirstOrDefault(item => item.Id == id);

    private string? Validar(GuardarEstudioPlantillaDto solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            return "Escribe el nombre del estudio.";
        }

        if (solicitud.InvestigadorPrincipalId is Guid principal && _almacen.Investigadores().All(item => item.Id != principal))
        {
            return "El investigador principal no está en la lista.";
        }

        if (solicitud.SubinvestigadorId is Guid sub && _almacen.Investigadores().All(item => item.Id != sub))
        {
            return "El subinvestigador no está en la lista.";
        }

        if (solicitud.CoordinadorPrincipalId is Guid coordinador && _almacen.Coordinadores().All(item => item.Id != coordinador))
        {
            return "El coordinador principal no está en la lista.";
        }

        if (solicitud.CoordinadorApoyoId is Guid apoyo && _almacen.Coordinadores().All(item => item.Id != apoyo))
        {
            return "El coordinador backup no está en la lista.";
        }

        return null;
    }

    private static EstudioDePlantillas Desde(GuardarEstudioPlantillaDto solicitud) => new()
    {
        Nombre = solicitud.Nombre,
        Codigo = solicitud.Codigo ?? "",
        Patrocinador = solicitud.Patrocinador ?? "",
        InvestigadorPrincipalId = solicitud.InvestigadorPrincipalId,
        SubinvestigadorId = solicitud.SubinvestigadorId,
        CoordinadorPrincipalId = solicitud.CoordinadorPrincipalId,
        CoordinadorApoyoId = solicitud.CoordinadorApoyoId
    };

    private EstudioPlantillaDto Mapa(EstudioDePlantillas estudio)
    {
        var principal = _almacen.Investigadores().FirstOrDefault(item => item.Id == estudio.InvestigadorPrincipalId);
        var sub = _almacen.Investigadores().FirstOrDefault(item => item.Id == estudio.SubinvestigadorId);
        var coordinador = _almacen.Coordinadores().FirstOrDefault(item => item.Id == estudio.CoordinadorPrincipalId);
        var apoyo = _almacen.Coordinadores().FirstOrDefault(item =>
            item.Nombre.Equals(PlantillasFijas.CoordinadorBackup, StringComparison.OrdinalIgnoreCase))
            ?? _almacen.Coordinadores().FirstOrDefault(item => item.Id == estudio.CoordinadorApoyoId);
        return new EstudioPlantillaDto
        {
            Id = estudio.Id,
            Nombre = estudio.Nombre,
            Codigo = estudio.Codigo,
            Patrocinador = estudio.Patrocinador,
            InvestigadorPrincipalId = estudio.InvestigadorPrincipalId,
            SubinvestigadorId = estudio.SubinvestigadorId,
            CoordinadorPrincipalId = estudio.CoordinadorPrincipalId,
            CoordinadorApoyoId = estudio.CoordinadorApoyoId,
            InvestigadorPrincipal = principal?.Nombre ?? "",
            Subinvestigador = sub?.Nombre ?? "",
            CoordinadorPrincipal = coordinador?.Nombre ?? "",
            CorreoCoordinadorPrincipal = coordinador?.Correo ?? "",
            CoordinadorApoyo = apoyo?.Nombre ?? "",
            CorreoCoordinadorApoyo = apoyo?.Correo ?? "",
            PlantillasRevisadas = estudio.PlantillasRevisadas.ToList()
        };
    }

    private static InvestigadorPlantillaDto Mapa(InvestigadorPlantilla persona) =>
        new() { Id = persona.Id, Nombre = persona.Nombre, Titulo = persona.Titulo };

    private async Task<bool> PuedeAdministrarListasAsync()
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificador))
        {
            return false;
        }

        var usuario = await _nucleo.BuscarPorProveedorAsync(identificador, HttpContext.RequestAborted);
        if (usuario is null)
        {
            return false;
        }

        var permisos = await _nucleo.PermisosDeUsuarioAsync(usuario.Id, HttpContext.RequestAborted);
        return permisos.Contains(PermisosNucleo.UsuariosAprobar, StringComparer.Ordinal);
    }

    private static string Carpeta(InvestigadorPlantilla persona) => Carpeta(persona.Nombre, persona.Titulo);

    private static string Carpeta(string nombre, string titulo) =>
        string.IsNullOrWhiteSpace(titulo) ? nombre.Trim() : $"{nombre.Trim()} - {titulo.Trim()}";

    private static CoordinadorPlantillaDto Mapa(CoordinadorPlantilla persona) =>
        new() { Id = persona.Id, Nombre = persona.Nombre, Correo = persona.Correo };

    private static bool Repetido(IEnumerable<string> nombres, string nombre) =>
        nombres.Any(item => string.Equals(item.Trim(), nombre.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool CorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return false;
        }

        var partes = correo.Trim().Split('@');
        return partes.Length == 2 && partes[0].Length > 0 && partes[1].Contains('.');
    }

    private static string NombreArchivo(string nombre)
    {
        var limpio = string.Join("-", nombre.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (limpio.Length > 80)
        {
            limpio = limpio[..80];
        }

        return string.IsNullOrWhiteSpace(limpio) ? "carta.pdf" : $"{limpio}.pdf";
    }
}
