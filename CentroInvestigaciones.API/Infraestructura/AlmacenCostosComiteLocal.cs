using System.Text.Json;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenCostosComiteLocal : IAlmacenCostosComite
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();

    public AlmacenCostosComiteLocal(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "costos-comite.json");
    }

    public CostosComite Leer()
    {
        lock (_bloqueo)
        {
            if (!File.Exists(_ruta))
            {
                return new CostosComite();
            }

            try
            {
                return JsonSerializer.Deserialize<CostosComite>(File.ReadAllText(_ruta), Json) ?? new CostosComite();
            }
            catch (JsonException)
            {
                return new CostosComite();
            }
        }
    }

    public void Guardar(CostosComite costos)
    {
        lock (_bloqueo)
        {
            costos.ValorEtica = costos.ValorEtica.Trim();
            costos.ResponsableEtica = costos.ResponsableEtica.Trim();
            costos.ValorInvestigacion = costos.ValorInvestigacion.Trim();
            costos.ResponsableInvestigacion = costos.ResponsableInvestigacion.Trim();
            EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(costos, Json));
        }
    }
}
