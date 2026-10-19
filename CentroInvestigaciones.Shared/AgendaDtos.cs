namespace CentroInvestigaciones.Shared;

public sealed class DiaAgendaDto
{
    public string Fecha { get; set; } = "";

    public bool Festivo { get; set; }

    public bool PuedeAsignar { get; set; }

    public bool PuedeMonitorias { get; set; }

    public string AgendaPropia { get; set; } = "";

    public string Persona { get; set; } = "";

    public Guid? UsuarioId { get; set; }

    public List<PersonaAgendaDto> Personas { get; set; } = [];

    public List<CitaAgendaDto> Citas { get; set; } = [];

    public List<MonitoriaAgendaDto> Monitorias { get; set; } = [];

    public List<string> FestivosMes { get; set; } = [];

    public List<string> DiasOcupados { get; set; } = [];

    public List<string> DiasMonitorias { get; set; } = [];

    public List<CitaAgendaDto> Importantes { get; set; } = [];
}

public sealed class PersonaAgendaDto
{
    public Guid? UsuarioId { get; set; }

    public string Nombre { get; set; } = "";

    public string Agenda { get; set; } = "";
}

public sealed class CitaAgendaDto
{
    public Guid Id { get; set; }

    public string Agenda { get; set; } = "";

    public string Fecha { get; set; } = "";

    public string Persona { get; set; } = "";

    public Guid? UsuarioId { get; set; }

    public string HoraInicio { get; set; } = "";

    public string HoraFin { get; set; } = "";

    public string Tipo { get; set; } = "citas";

    public bool Importante { get; set; }

    public Dictionary<string, string> Campos { get; set; } = [];

    public string CreadoPorPersona { get; set; } = "";

    public bool PuedeEditar { get; set; }
}

public sealed class GuardarCitaAgendaDto
{
    public string Agenda { get; set; } = "";

    public string Fecha { get; set; } = "";

    public string HoraInicio { get; set; } = "";

    public string HoraFin { get; set; } = "";

    public string Tipo { get; set; } = "citas";

    public bool Importante { get; set; }

    public Guid? UsuarioId { get; set; }

    public string Persona { get; set; } = "";

    public Dictionary<string, string> Campos { get; set; } = [];
}

public sealed class MonitoriaAgendaDto
{
    public Guid Id { get; set; }

    public string Fecha { get; set; } = "";

    public string Descripcion { get; set; } = "";

    public string CreadoPorPersona { get; set; } = "";
}

public sealed class MarcarImportanteDto
{
    public bool Importante { get; set; }
}

public sealed class GuardarMonitoriaAgendaDto
{
    public Guid? Id { get; set; }

    public string Fecha { get; set; } = "";

    public string Descripcion { get; set; } = "";
}
