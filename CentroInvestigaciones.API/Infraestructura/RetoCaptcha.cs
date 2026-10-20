using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class RetoCaptcha
{
    private const string CookieReto = "CentroInvestigaciones.Reto";
    private const string CookieListo = "CentroInvestigaciones.Externo";
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly IDataProtector _protector;

    public RetoCaptcha(IDataProtectionProvider proteccion)
    {
        _protector = proteccion.CreateProtector("centro-ingreso-captcha");
    }

    public string Emitir(HttpContext http)
    {
        var codigo = Codigo();
        Escribir(http, CookieReto, codigo, TimeSpan.FromMinutes(5));
        return Imagen(codigo);
    }

    public bool Resolver(HttpContext http, string? respuesta)
    {
        var esperado = Leer(http, CookieReto);
        if (esperado is null || !string.Equals(esperado, Normalizar(respuesta), StringComparison.Ordinal))
        {
            return false;
        }

        http.Response.Cookies.Delete(CookieReto, Opciones(http, TimeSpan.Zero));
        Escribir(http, CookieListo, "ok", TimeSpan.FromHours(2));
        return true;
    }

    public bool Listo(HttpContext http) => Leer(http, CookieListo) == "ok";

    private void Escribir(HttpContext http, string nombre, string valor, TimeSpan vida)
    {
        var vence = DateTimeOffset.UtcNow.Add(vida).ToUnixTimeSeconds();
        http.Response.Cookies.Append(nombre, _protector.Protect($"{valor}|{vence}"), Opciones(http, vida));
    }

    private string? Leer(HttpContext http, string nombre)
    {
        if (!http.Request.Cookies.TryGetValue(nombre, out var protegido))
        {
            return null;
        }

        try
        {
            var plano = _protector.Unprotect(protegido);
            var corte = plano.LastIndexOf('|');
            if (corte <= 0 || !long.TryParse(plano[(corte + 1)..], out var vence))
            {
                return null;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > vence ? null : plano[..corte];
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static CookieOptions Opciones(HttpContext http, TimeSpan vida) => new()
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = http.Request.IsHttps,
        MaxAge = vida <= TimeSpan.Zero ? TimeSpan.Zero : vida,
        Path = "/"
    };

    private static string Codigo()
    {
        Span<char> letras = stackalloc char[5];
        for (var i = 0; i < letras.Length; i++)
        {
            letras[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return new string(letras);
    }

    private static string Normalizar(string? respuesta) =>
        (respuesta ?? "").Trim().Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    private static string Imagen(string codigo)
    {
        var letras = new StringBuilder();
        for (var i = 0; i < codigo.Length; i++)
        {
            var x = 28 + (i * 36);
            var y = i % 2 == 0 ? 38 : 48;
            var giro = i % 2 == 0 ? -8 : 8;
            letras.Append($"<text x=\"{x}\" y=\"{y}\" transform=\"rotate({giro} {x} {y})\">{codigo[i]}</text>");
        }

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="220" height="64" viewBox="0 0 220 64" role="img">
              <rect width="220" height="64" rx="8" fill="#f4f8fc"/>
              <path d="M8 40 C 40 10, 80 58, 120 28 S 180 12, 212 36" fill="none" stroke="#8aafd4" stroke-width="2"/>
              <path d="M6 22 C 50 48, 90 8, 140 40 S 190 52, 214 18" fill="none" stroke="#1e4a8a" stroke-width="1.5"/>
              <g fill="#1e4a8a" font-family="Segoe UI, sans-serif" font-size="28" font-weight="700">{letras}</g>
            </svg>
            """;
    }
}
