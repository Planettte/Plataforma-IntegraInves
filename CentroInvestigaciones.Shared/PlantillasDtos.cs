namespace CentroInvestigaciones.Shared;

public sealed class InvestigadorPlantillaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Titulo { get; set; } = "";
}

public sealed class CoordinadorPlantillaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";
}

public sealed class NombrePlantillaDto
{
    public string Nombre { get; set; } = "";

    public string Titulo { get; set; } = "";
}

public sealed class CoordinadorAltaDto
{
    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";
}

public sealed class EstudioPlantillaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Codigo { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public Guid? InvestigadorPrincipalId { get; set; }

    public Guid? SubinvestigadorId { get; set; }

    public Guid? CoordinadorPrincipalId { get; set; }

    public Guid? CoordinadorApoyoId { get; set; }

    public string InvestigadorPrincipal { get; set; } = "";

    public string Subinvestigador { get; set; } = "";

    public string CoordinadorPrincipal { get; set; } = "";

    public string CorreoCoordinadorPrincipal { get; set; } = "";

    public string CoordinadorApoyo { get; set; } = "";

    public string CorreoCoordinadorApoyo { get; set; } = "";

    public List<string> PlantillasRevisadas { get; set; } = [];
}

public sealed class GuardarEstudioPlantillaDto
{
    public string Nombre { get; set; } = "";

    public string Codigo { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public Guid? InvestigadorPrincipalId { get; set; }

    public Guid? SubinvestigadorId { get; set; }

    public Guid? CoordinadorPrincipalId { get; set; }

    public Guid? CoordinadorApoyoId { get; set; }
}

public sealed class RevisadasPlantillaDto
{
    public List<string> Codigos { get; set; } = [];
}

public sealed class CartaCatalogoDto
{
    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";
}

public sealed class VistaCartaDto
{
    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string InvestigadorPrincipal { get; set; } = "";

    public string TituloPrincipal { get; set; } = "";

    public string Subinvestigador { get; set; } = "";

    public string TituloSub { get; set; } = "";

    public List<LineaCartaDto> Lineas { get; set; } = [];
}

public sealed class LineaCartaDto
{
    public List<TrozoCartaDto> Trozos { get; set; } = [];

    public bool Espacio { get; set; }

    public bool Vineta { get; set; }

    public bool Salto { get; set; }

    public TablaCartaDto? Tabla { get; set; }
}

public sealed class TablaCartaDto
{
    public List<string> Columnas { get; set; } = [];

    public List<List<TrozoCartaDto>> Filas { get; set; } = [];
}

public sealed class TrozoCartaDto
{
    public string Tipo { get; set; } = "texto";

    public string Clave { get; set; } = "";

    public string Texto { get; set; } = "";

    public string Valor { get; set; } = "";

    public bool Amplio { get; set; }

    public bool Bloqueado { get; set; }
}

public sealed class GenerarCartaDto
{
    public string Firmante { get; set; } = "principal";

    public bool ConFirma { get; set; }

    public Dictionary<string, string> Campos { get; set; } = [];
}

public sealed class EnviarCartaDto
{
    public string Firmante { get; set; } = "principal";

    public bool ConFirma { get; set; }

    public bool AlPrincipal { get; set; }

    public bool AlApoyo { get; set; }

    public Dictionary<string, string> Campos { get; set; } = [];
}

public sealed class MensajeDto
{
    public string Mensaje { get; set; } = "";
}
