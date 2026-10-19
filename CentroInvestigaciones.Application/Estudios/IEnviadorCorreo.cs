namespace CentroInvestigaciones.Application.Estudios;

public interface IEnviadorCorreo
{
    bool EstaConfigurado { get; }

    string? DestinoComite { get; }

    Task<bool> EnviarAsync(
        string destino,
        string asunto,
        string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto = null,
        CancellationToken ct = default);
}
