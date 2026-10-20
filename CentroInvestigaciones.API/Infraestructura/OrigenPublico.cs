namespace CentroInvestigaciones.API.Infraestructura;

public sealed class OrigenPublico
{
    public OrigenPublico(IConfiguration configuracion)
    {
        var texto = configuracion["Plataforma:OrigenPublico"]?.Trim().TrimEnd('/') ?? "";
        Valor = Uri.TryCreate(texto, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.GetLeftPart(UriPartial.Authority)
            : "";
    }

    public string Valor { get; }

    public bool Configurado => Valor.Length > 0;
}
