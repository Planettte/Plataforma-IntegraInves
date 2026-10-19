namespace CentroInvestigaciones.Domain;

public class EventoAuditoria
{
    public Guid Id { get; set; }

    public DateTime FechaUtc { get; set; }

    public Guid? UsuarioId { get; set; }

    public string Accion { get; set; } = "";

    public string Entidad { get; set; } = "";

    public string? EntidadId { get; set; }

    public string Resultado { get; set; } = "";

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public string? DireccionRed { get; set; }

    public string? AgenteUsuario { get; set; }
}
