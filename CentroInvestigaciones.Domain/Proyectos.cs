namespace CentroInvestigaciones.Domain;

public static class EstadosActividad
{
    public const string EnProgreso = "En progreso";

    public const string Completado = "Completado";

    public const string Pausado = "Pausado";

    public static readonly string[] Todos = [EnProgreso, Completado, Pausado];

    public static bool Valido(string? valor) =>
        Todos.Contains(valor ?? "", StringComparer.Ordinal);
}

public sealed class ProyectoSeguimiento
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Estado { get; set; } = "Activo";

    public DateTimeOffset CreadoEl { get; set; }

    public Guid ResponsableId { get; set; }

    public string Semillero { get; set; } = "";
}

public sealed class FaseProyecto
{
    public Guid Id { get; set; }

    public Guid ProyectoId { get; set; }

    public string Nombre { get; set; } = "";

    public int Orden { get; set; }

    public int Peso { get; set; }
}

public sealed class ActividadProyecto
{
    public Guid Id { get; set; }

    public Guid FaseId { get; set; }

    public string Descripcion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaLimite { get; set; } = "";

    public int Peso { get; set; }

    public string Estado { get; set; } = EstadosActividad.EnProgreso;
}

public sealed class InvestigadorProyecto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";
}

public sealed class AsignacionActividad
{
    public Guid Id { get; set; }

    public Guid ActividadId { get; set; }

    public Guid InvestigadorId { get; set; }

    public int Peso { get; set; }

    public string Estado { get; set; } = EstadosActividad.EnProgreso;
}

public sealed class ProrrogaActividad
{
    public Guid Id { get; set; }

    public Guid ActividadId { get; set; }

    public Guid? InvestigadorId { get; set; }

    public string FechaAnterior { get; set; } = "";
}

public sealed class TableroProyecto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Semillero { get; set; } = "";

    public Guid ResponsableId { get; set; }

    public decimal Progreso { get; set; }

    public List<TableroFase> Fases { get; set; } = [];

    public List<InvestigadorProyecto> Investigadores { get; set; } = [];
}

public sealed class TableroFase
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public int Orden { get; set; }

    public int Peso { get; set; }

    public decimal Progreso { get; set; }

    public List<AvanceInvestigador> Investigadores { get; set; } = [];

    public List<TableroActividad> Actividades { get; set; } = [];
}

public sealed class AvanceInvestigador
{
    public string Nombre { get; set; } = "";

    public decimal Porcentaje { get; set; }
}

public sealed class TableroActividad
{
    public Guid Id { get; set; }

    public string Descripcion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaLimite { get; set; } = "";

    public List<string> FechasAnteriores { get; set; } = [];

    public int Peso { get; set; }

    public string Estado { get; set; } = "";

    public List<AsignadoActividad> Asignados { get; set; } = [];
}

public sealed class AsignadoActividad
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public int Peso { get; set; }

    public int Prorrogas { get; set; }
}
