namespace CentroInvestigaciones.Domain;

public static class PlantillasFijas
{
    public const string CoordinadorBackup = "Wendy Katherine Mantilla Duarte";
}

public sealed class InvestigadorPlantilla
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Titulo { get; set; } = "";
}

public sealed class CoordinadorPlantilla
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Correo { get; set; } = "";
}

public sealed class EstudioDePlantillas
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Codigo { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public Guid? InvestigadorPrincipalId { get; set; }

    public Guid? SubinvestigadorId { get; set; }

    public Guid? CoordinadorPrincipalId { get; set; }

    public Guid? CoordinadorApoyoId { get; set; }

    public List<string> PlantillasRevisadas { get; set; } = [];

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime FechaEdicionUtc { get; set; }
}
