using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AvisosProtocolo
{
    private const string Marca = "Formulario de protocolos";
    private readonly IEnviadorCorreo _correo;

    public AvisosProtocolo(IEnviadorCorreo correo)
    {
        _correo = correo;
    }

    public Task<bool> NuevoAsync(Protocolo protocolo, string destino, string saludo, string origen, CancellationToken ct) =>
        AlRevisorAsync(destino, saludo, "Nuevo protocolo para revisión", $"Hay un protocolo nuevo de {Html(protocolo.Nombre)} para revisar.", origen, protocolo.Id, "Abrir protocolo", ct);

    public Task<bool> ReenviadoAsync(Protocolo protocolo, string destino, string saludo, string origen, CancellationToken ct) =>
        AlRevisorAsync(destino, saludo, "Protocolo corregido", $"{Html(protocolo.Nombre)} corrigió el protocolo y lo reenvió.", origen, protocolo.Id, "Revisar protocolo", ct);

    public Task<bool> CorreccionAsync(Protocolo protocolo, string origen, CancellationToken ct)
    {
        var cuerpo = $"<p style=\"margin:0;\">El protocolo <strong>{Html(protocolo.Nombre)}</strong> requiere correcciones. Los comentarios están en el formulario.</p>";
        return _correo.EnviarAsync(
            protocolo.Correo,
            "Correcciones del protocolo",
            PlantillaCorreo.Armar("Correcciones requeridas", protocolo.Nombre, cuerpo, $"{origen}/udd/protocolos/externo/{protocolo.Token}", "Corregir el formulario", Marca),
            null,
            ct);
    }

    public Task<bool> AprobadoAsync(Protocolo protocolo, CancellationToken ct)
    {
        var cuerpo = $"<p style=\"margin:0;\">El protocolo <strong>{Html(protocolo.Nombre)}</strong> fue aceptado.</p>";
        return _correo.EnviarAsync(
            protocolo.Correo,
            "Protocolo aceptado",
            PlantillaCorreo.Armar("Protocolo aceptado", protocolo.Nombre, cuerpo, marca: Marca),
            null,
            ct);
    }

    private Task<bool> AlRevisorAsync(string destino, string saludo, string titulo, string cuerpo, string origen, Guid id, string textoEnlace, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(destino))
        {
            return Task.FromResult(false);
        }

        return _correo.EnviarAsync(
            destino,
            titulo,
            PlantillaCorreo.Armar(titulo, saludo, $"<p style=\"margin:0;\">{cuerpo}</p>", $"{origen}/udd/protocolos/{id}", textoEnlace, Marca),
            null,
            ct);
    }

    private static string Html(string texto) => System.Net.WebUtility.HtmlEncode(texto);
}
