namespace CentroInvestigaciones.Domain;

public class Nota
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public string Titulo { get; set; } = "";

    public string Contenido { get; set; } = "";

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime? FechaActualizacionUtc { get; set; }
}
