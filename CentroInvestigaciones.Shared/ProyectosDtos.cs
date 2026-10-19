namespace CentroInvestigaciones.Shared;

public sealed class ResponsableProyectoDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";
}

public sealed class ProyectoListaDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Semillero { get; set; } = "";

    public Guid ResponsableId { get; set; }

    public DateTimeOffset CreadoEl { get; set; }
}

public sealed class CrearProyectoDto
{
    public Guid ResponsableId { get; set; }

    public string Nombre { get; set; } = "";

    public string Semillero { get; set; } = "";

    public List<FaseEdicionDto> Fases { get; set; } = [];
}

public sealed class ActualizarProyectoDto
{
    public string Nombre { get; set; } = "";

    public string Semillero { get; set; } = "";
}

public sealed class FaseEdicionDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public int Peso { get; set; }
}

public sealed class ActividadEdicionDto
{
    public Guid Id { get; set; }

    public Guid FaseId { get; set; }

    public string Descripcion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaLimite { get; set; } = "";

    public int Peso { get; set; }
}

public sealed class EstadoActividadDto
{
    public string Estado { get; set; } = "";
}

public sealed class InvestigadorProyectoDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";
}

public sealed class AsignacionDto
{
    public Guid ActividadId { get; set; }

    public Guid InvestigadorId { get; set; }
}

public sealed class ProrrogaDto
{
    public string Fecha { get; set; } = "";

    public List<Guid> Investigadores { get; set; } = [];
}

public sealed class TableroProyectoDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Semillero { get; set; } = "";

    public Guid ResponsableId { get; set; }

    public string ResponsableNombre { get; set; } = "";

    public decimal Progreso { get; set; }

    public List<TableroFaseDto> Fases { get; set; } = [];

    public List<InvestigadorProyectoDto> Investigadores { get; set; } = [];
}

public sealed class TableroFaseDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public int Orden { get; set; }

    public int Peso { get; set; }

    public decimal Progreso { get; set; }

    public List<AvanceInvestigadorDto> Investigadores { get; set; } = [];

    public List<TableroActividadDto> Actividades { get; set; } = [];
}

public sealed class AvanceInvestigadorDto
{
    public string Nombre { get; set; } = "";

    public decimal Porcentaje { get; set; }
}

public sealed class TableroActividadDto
{
    public Guid Id { get; set; }

    public string Descripcion { get; set; } = "";

    public string FechaInicio { get; set; } = "";

    public string FechaLimite { get; set; } = "";

    public List<string> FechasAnteriores { get; set; } = [];

    public int Peso { get; set; }

    public string Estado { get; set; } = "";

    public List<AsignadoTableroDto> Asignados { get; set; } = [];
}

public sealed class AsignadoTableroDto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public int Peso { get; set; }

    public int Prorrogas { get; set; }
}
