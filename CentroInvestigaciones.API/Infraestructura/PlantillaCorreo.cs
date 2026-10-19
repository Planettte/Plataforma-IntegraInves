using System.Net;

namespace CentroInvestigaciones.API.Infraestructura;

public static class PlantillaCorreo
{
    public static string Armar(string titulo, string saludo, string cuerpoHtml, string? enlace = null, string? textoEnlace = null, string marca = "Registro de estudios")
    {
        var boton = "";
        if (!string.IsNullOrWhiteSpace(enlace) && !string.IsNullOrWhiteSpace(textoEnlace))
        {
            var href = WebUtility.HtmlEncode(enlace);
            var texto = WebUtility.HtmlEncode(textoEnlace);
            boton = $"""
                <p style="margin:22px 0 0;">
                  <a href="{href}" style="display:inline-block;background:#2c5c94;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">{texto}</a>
                </p>
                """;
        }

        var hola = string.IsNullOrWhiteSpace(saludo)
            ? ""
            : $"<p style=\"margin:0 0 16px;\">Hola {WebUtility.HtmlEncode(saludo)},</p>";

        return $$"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background:#e8eef5;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#e8eef5;padding:28px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;background:#ffffff;border-radius:14px;overflow:hidden;">
                      <tr>
                        <td style="background:#2c5c94;color:#ffffff;padding:22px 28px;">
                          <div style="font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:700;">{{WebUtility.HtmlEncode(titulo)}}</div>
                          <div style="font-family:Segoe UI,Arial,sans-serif;font-size:13px;margin-top:4px;opacity:0.9;">FOSCAL · {{WebUtility.HtmlEncode(marca)}}</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:28px;color:#243044;font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:1.55;">
                          {{hola}}
                          {{cuerpoHtml}}
                          {{boton}}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 28px 22px;color:#7b8794;font-family:Segoe UI,Arial,sans-serif;font-size:12px;text-align:center;">
                          Mensaje automático — no responder.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string Recuadro(string html) =>
        $"""<div style="margin-top:14px;background:#fff6eb;border:1px solid #f0d7b8;border-radius:10px;padding:14px 16px;color:#8a5a24;">{html}</div>""";
}
