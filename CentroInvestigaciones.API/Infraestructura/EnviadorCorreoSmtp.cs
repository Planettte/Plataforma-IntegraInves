using System.Net;
using System.Net.Mail;
using CentroInvestigaciones.Application.Estudios;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class EnviadorCorreoSmtp : IEnviadorCorreo
{
    private readonly IConfiguration _configuracion;
    private readonly ILogger<EnviadorCorreoSmtp> _registro;

    public EnviadorCorreoSmtp(IConfiguration configuracion, ILogger<EnviadorCorreoSmtp> registro)
    {
        _configuracion = configuracion;
        _registro = registro;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(Contrasena) && !string.IsNullOrWhiteSpace(Usuario);

    public string? DestinoComite => Texto("DestinoComite");

    public async Task<bool> EnviarAsync(
        string destino,
        string asunto,
        string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto = null,
        CancellationToken ct = default)
    {
        if (!EstaConfigurado || string.IsNullOrWhiteSpace(destino))
        {
            return false;
        }

        try
        {
            using var mensaje = new MailMessage(Remitente, destino.Trim(), asunto, cuerpoHtml)
            {
                IsBodyHtml = true
            };
            if (adjunto is { Contenido.Length: > 0 } archivo)
            {
                mensaje.Attachments.Add(new Attachment(new MemoryStream(archivo.Contenido), archivo.Nombre, archivo.Tipo));
            }

            using var cliente = new SmtpClient(Servidor, Puerto)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(Usuario, Contrasena)
            };
            await cliente.SendMailAsync(mensaje, ct);
            return true;
        }
        catch (Exception excepcion)
        {
            _registro.LogWarning(excepcion, "No se pudo enviar el correo de estudios a {Destino}.", destino);
            return false;
        }
    }

    private string Servidor => Texto("Servidor") ?? "smtp.gmail.com";

    private int Puerto => int.TryParse(Texto("Puerto"), out var puerto) ? puerto : 587;

    private string Usuario => Texto("Usuario") ?? DesarrolloLocal.CorreoAdministrador;

    private string Remitente => Texto("Remitente") ?? Usuario;

    private string? Contrasena => Texto("Contrasena");

    private string? Texto(string clave)
    {
        var valor = _configuracion[$"Correo:{clave}"];
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
