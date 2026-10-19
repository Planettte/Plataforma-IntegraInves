using System.Globalization;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public static class CartasPlantilla
{
    private static readonly string[] Bloqueados = ["codigo", "patrocinador", "investigador", "especialidad"];

    public static VistaCartaDto? Vista(
        EstudioDePlantillas estudio,
        string codigo,
        string investigadorPrincipal,
        string tituloPrincipal,
        string subinvestigador,
        string tituloSub)
    {
        var carta = LectorCartas.Buscar(codigo);
        if (carta is null)
        {
            return null;
        }

        var fecha = DateTime.Now.ToString("d 'de' MMMM 'de' yyyy", new CultureInfo("es-CO"));
        var valores = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["codigo"] = estudio.Codigo,
            ["patrocinador"] = estudio.Patrocinador,
            ["fecha"] = fecha,
            ["investigador"] = investigadorPrincipal,
            ["especialidad"] = tituloPrincipal
        };

        var lineas = LectorCartas.Lineas(codigo)
            .Select(linea => new LineaCartaDto
            {
                Espacio = linea.Espacio,
                Vineta = linea.Vineta,
                Salto = linea.Salto,
                Trozos = linea.Trozos.Select(trozo => Copiar(trozo, valores)).ToList(),
                Tabla = linea.Tabla is null ? null : new TablaCartaDto
                {
                    Columnas = linea.Tabla.Columnas.ToList(),
                    Filas = linea.Tabla.Filas.Select(fila => fila.Select(trozo => Copiar(trozo, valores)).ToList()).ToList()
                }
            })
            .ToList();

        return new VistaCartaDto
        {
            Codigo = carta.Codigo,
            Nombre = carta.Nombre,
            InvestigadorPrincipal = investigadorPrincipal,
            TituloPrincipal = tituloPrincipal,
            Subinvestigador = subinvestigador,
            TituloSub = tituloSub,
            Lineas = lineas
        };
    }

    private static TrozoCartaDto Copiar(TrozoCartaDto trozo, IReadOnlyDictionary<string, string> valores) => new()
    {
        Tipo = trozo.Tipo,
        Clave = trozo.Clave,
        Texto = trozo.Texto,
        Amplio = trozo.Amplio,
        Bloqueado = Bloqueados.Contains(trozo.Clave),
        Valor = trozo.Tipo == "campo" && valores.TryGetValue(trozo.Clave, out var valor) ? valor : ""
    };
}
