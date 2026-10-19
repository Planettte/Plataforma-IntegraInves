namespace CentroInvestigaciones.API.Autenticacion;

public sealed class OpcionesGoogle
{
    public const string Seccion = "Autenticacion:Google";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
