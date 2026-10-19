namespace CentroInvestigaciones.Domain;

public sealed class NotificacionUsuario
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Titulo { get; set; } = "";

    public string Mensaje { get; set; } = "";

    public string? Enlace { get; set; }

    public DateTime FechaUtc { get; set; }

    public DateTime? VistaUtc { get; set; }
}
