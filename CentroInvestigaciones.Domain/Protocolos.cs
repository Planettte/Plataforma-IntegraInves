namespace CentroInvestigaciones.Domain;

public static class FormatosProtocolo
{
    public const string Uis = "UIS";

    public const string Foscal = "FOSCAL";

    public const string Udes = "UDES";

    public static readonly string[] Todos = [Uis, Foscal, Udes];

    public static bool Valido(string? valor) =>
        Todos.Contains(valor ?? "", StringComparer.Ordinal);
}

public static class EstadosProtocolo
{
    public const string EnRevision = "En revisión";

    public const string Correccion = "Corrección";

    public const string Aprobado = "Aprobado";

    public static readonly string[] Todos = [EnRevision, Correccion, Aprobado];
}

public sealed class Protocolo
{
    public Guid Id { get; set; }

    public string Token { get; set; } = "";

    public string Estado { get; set; } = EstadosProtocolo.EnRevision;

    public string Formato { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";

    public Guid RevisorId { get; set; }

    public Dictionary<string, string> Respuestas { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Comentarios { get; set; } = new(StringComparer.Ordinal);

    public List<FilaConocimientoProtocolo> Conocimiento { get; set; } = [];

    public List<FilaProductoProtocolo> Tecnologico { get; set; } = [];

    public List<FilaProductoProtocolo> Capacidad { get; set; } = [];

    public List<FilaCronogramaProtocolo> CronogramaFoscal { get; set; } = [];

    public List<FilaCronogramaProtocolo> CronogramaUdes { get; set; } = [];

    public List<FilaPresupuestoProtocolo> Presupuesto { get; set; } = [];

    public List<bool> EticaPoblacion { get; set; } = [];

    public List<bool> EticaIntervencion { get; set; } = [];

    public List<bool> EticaInstrumentos { get; set; } = [];

    public DateTimeOffset CreadoEl { get; set; }

    public DateTimeOffset EditadoEl { get; set; }
}

public sealed class FilaConocimientoProtocolo
{
    public string Resultado { get; set; } = "";

    public string Indicador { get; set; } = "";

    public string Beneficiario { get; set; } = "";
}

public sealed class FilaProductoProtocolo
{
    public string Objetivo { get; set; } = "";

    public string Resultado { get; set; } = "";

    public string Indicador { get; set; } = "";

    public string Beneficiario { get; set; } = "";
}

public sealed class FilaCronogramaProtocolo
{
    public string Actividad { get; set; } = "";

    public string Responsable { get; set; } = "";

    public List<bool> Meses { get; set; } = [];
}

public sealed class FilaPresupuestoProtocolo
{
    public string Rubro { get; set; } = "";

    public List<string> Valores { get; set; } = [];
}
