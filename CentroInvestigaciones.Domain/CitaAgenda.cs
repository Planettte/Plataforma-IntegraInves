namespace CentroInvestigaciones.Domain;

public sealed class CitaAgenda
{
    public Guid Id { get; set; }

    public string Agenda { get; set; } = "";

    public DateOnly Fecha { get; set; }

    public string Persona { get; set; } = "";

    public Guid? UsuarioId { get; set; }

    public string HoraInicio { get; set; } = "";

    public string HoraFin { get; set; } = "";

    public string Tipo { get; set; } = "citas";

    public bool Importante { get; set; }

    public Dictionary<string, string> Campos { get; set; } = [];

    public string CreadoPorPersona { get; set; } = "";

    public string CreadoPorCorreo { get; set; } = "";
}

public sealed class MonitoriaAgenda
{
    public Guid Id { get; set; }

    public DateOnly Fecha { get; set; }

    public string Descripcion { get; set; } = "";

    public string CreadoPorPersona { get; set; } = "";

    public string CreadoPorCorreo { get; set; } = "";
}
