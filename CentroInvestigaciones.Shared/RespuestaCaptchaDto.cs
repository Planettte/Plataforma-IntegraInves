namespace CentroInvestigaciones.Shared;

public sealed class RespuestaCaptchaDto
{
    public string Respuesta { get; set; } = "";
}

public sealed class EstadoCaptchaDto
{
    public bool Listo { get; set; }
}
