using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CentroInvestigaciones.Shared;

public static class LectorCartas
{
    private static readonly Regex Marca = new(@"\{\{(.*?)\}\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static readonly Lazy<IReadOnlyList<CartaFuente>> Cache = new(Cargar);

    public static IReadOnlyList<CartaCatalogoDto> Catalogo() =>
        Cache.Value.Select(carta => new CartaCatalogoDto { Codigo = carta.Codigo, Nombre = carta.Nombre }).ToList();

    public static CartaCatalogoDto? Buscar(string codigo) =>
        Catalogo().FirstOrDefault(carta => string.Equals(carta.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<LineaCartaDto> Lineas(string codigo)
    {
        var carta = Cache.Value.FirstOrDefault(item => string.Equals(item.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        return carta is null ? [] : Interpretar(carta.Lineas);
    }

    private static List<LineaCartaDto> Interpretar(IReadOnlyList<string> origen)
    {
        var resultado = new List<LineaCartaDto>();
        var pendientes = new List<string>();
        var libre = 0;
        var tituloListo = false;
        var bloqueEsTitulo = false;
        var bloqueEsVineta = false;
        var claves = new HashSet<string>(StringComparer.Ordinal);

        void Vaciar()
        {
            if (pendientes.Count == 0)
            {
                bloqueEsTitulo = false;
                bloqueEsVineta = false;
                return;
            }

            var texto = string.Join(" ", pendientes.Where(item => item.Length > 0));
            pendientes.Clear();
            var eraTitulo = bloqueEsTitulo;
            var eraVineta = bloqueEsVineta;
            bloqueEsTitulo = false;
            bloqueEsVineta = false;
            if (texto.Length == 0)
            {
                return;
            }

            var esTitulo = eraTitulo || (!tituloListo && texto.StartsWith("Estudio de", StringComparison.OrdinalIgnoreCase));
            var clave = esTitulo ? Reservar("titulo", ref libre, claves) : SiguienteLibre(ref libre, claves);
            if (esTitulo)
            {
                tituloListo = true;
            }

            var lineaCampo = Linea(Campo(clave, texto, esTitulo || texto.Length > 70));
            lineaCampo.Vineta = eraVineta && !esTitulo;
            resultado.Add(lineaCampo);
        }

        foreach (var cruda in origen)
        {
            var linea = (cruda ?? "").Replace('\u00a0', ' ').Trim();
            if (linea.Length == 0)
            {
                continue;
            }

            if (linea.Equals("@@pagina", StringComparison.Ordinal))
            {
                Vaciar();
                resultado.Add(new LineaCartaDto { Salto = true });
                continue;
            }

            if (linea.Equals("@@espacio", StringComparison.Ordinal))
            {
                Vaciar();
                if (resultado.Count > 0)
                {
                    resultado[^1].Espacio = true;
                }

                continue;
            }

            if (linea.StartsWith("@@tabla:", StringComparison.Ordinal))
            {
                Vaciar();
                resultado.Add(LeerTabla(linea["@@tabla:".Length..], ref libre, claves));
                continue;
            }

            var coincidencias = Marca.Matches(linea);
            var sola = coincidencias.Count == 1
                && linea.StartsWith("{{", StringComparison.Ordinal)
                && linea.EndsWith("}}", StringComparison.Ordinal);

            if (sola)
            {
                var interior = Limpiar(coincidencias[0].Groups[1].Value);
                if (interior.Length == 0)
                {
                    Vaciar();
                    continue;
                }

                if (EsFirma(interior) || EsNombre(interior))
                {
                    Vaciar();
                    if (EsFirma(interior))
                    {
                        resultado.Add(Linea(new TrozoCartaDto { Tipo = "firma", Clave = "firma", Texto = "Firma" }));
                    }
                    else
                    {
                        resultado.Add(Linea(Campo(Reservar("investigador", ref libre, claves), "Nombre del investigador", false)));
                    }

                    continue;
                }

                var crudo = coincidencias[0].Groups[1].Value;
                if (EsInicioDeTitulo(crudo) && !tituloListo)
                {
                    if (pendientes.Count > 0)
                    {
                        Vaciar();
                    }

                    bloqueEsTitulo = true;
                    pendientes.Add(interior);
                    continue;
                }

                if (pendientes.Count == 0 && !bloqueEsTitulo && !tituloListo && ContinuaCampo(resultado, crudo))
                {
                    var ultimo = resultado[^1].Trozos[^1];
                    ultimo.Texto = $"{ultimo.Texto} {interior}".Trim();
                    ultimo.Amplio = ultimo.Texto.Length > 90;
                    continue;
                }

                var marca = crudo.TrimStart();
                var nuevaVineta = marca.StartsWith('"') || marca.StartsWith('“');
                if (pendientes.Count > 0 && nuevaVineta && !bloqueEsTitulo)
                {
                    Vaciar();
                }

                if (pendientes.Count == 0 && !bloqueEsTitulo)
                {
                    bloqueEsVineta = nuevaVineta;
                }

                pendientes.Add(interior);
                continue;
            }

            Vaciar();
            if (coincidencias.Count == 0)
            {
                resultado.Add(Linea(new TrozoCartaDto { Tipo = "texto", Texto = linea }));
                continue;
            }

            var cursor = 0;
            var fusiono = false;
            if (coincidencias[0].Index == 0 && resultado.Count > 0)
            {
                var crudo = coincidencias[0].Groups[1].Value;
                var interior = Limpiar(crudo);
                if (interior.Length > 0 && !EsFirma(interior) && !EsNombre(interior) && ContinuaCampo(resultado, crudo))
                {
                    var ultimo = resultado[^1].Trozos[^1];
                    ultimo.Texto = $"{ultimo.Texto} {interior}".Trim();
                    ultimo.Amplio = ultimo.Texto.Length > 90;
                    cursor = coincidencias[0].Index + coincidencias[0].Length;
                    fusiono = true;
                }
            }

            var trozos = new List<TrozoCartaDto>();
            foreach (Match marca in coincidencias)
            {
                if (marca.Index < cursor)
                {
                    continue;
                }

                if (marca.Index > cursor)
                {
                    var previo = linea[cursor..marca.Index];
                    if (previo.Length > 0)
                    {
                        trozos.Add(new TrozoCartaDto { Tipo = "texto", Texto = previo });
                    }
                }

                var interior = Limpiar(marca.Groups[1].Value);
                if (interior.Length > 0)
                {
                    if (EsFirma(interior))
                    {
                        trozos.Add(new TrozoCartaDto { Tipo = "firma", Clave = "firma", Texto = "Firma" });
                    }
                    else
                    {
                        var clave = ClaveEnLinea(linea, interior, ref libre, claves);
                        trozos.Add(Campo(clave, interior, interior.Length > 60));
                    }
                }

                cursor = marca.Index + marca.Length;
            }

            if (cursor < linea.Length)
            {
                trozos.Add(new TrozoCartaDto { Tipo = "texto", Texto = linea[cursor..] });
            }

            if (trozos.Count > 0)
            {
                if (fusiono && resultado.Count > 0 && trozos[0].Tipo == "texto")
                {
                    resultado[^1].Trozos.AddRange(trozos);
                }
                else
                {
                    resultado.Add(new LineaCartaDto { Trozos = trozos });
                }
            }
        }

        Vaciar();
        if (!resultado.Any(linea => linea.Trozos.Any(trozo => trozo.Tipo == "firma")))
        {
            var firma = Linea(new TrozoCartaDto { Tipo = "firma", Clave = "firma", Texto = "Firma" });
            var cierre = resultado.FindIndex(linea =>
                linea.Trozos.Count == 1
                && linea.Trozos[0].Tipo == "texto"
                && linea.Trozos[0].Texto.Trim().TrimEnd('.').Equals("Cordialmente", StringComparison.OrdinalIgnoreCase));
            if (cierre >= 0)
            {
                resultado.Insert(cierre + 1, firma);
            }
            else
            {
                resultado.Add(firma);
            }
        }

        Presentar(resultado);
        return resultado;
    }

    private static void AsegurarEspacioTrasCampos(List<LineaCartaDto> lineas)
    {
        foreach (var linea in lineas)
        {
            if (linea.Tabla is not null)
            {
                continue;
            }

            for (var i = 0; i < linea.Trozos.Count; i++)
            {
                if (linea.Trozos[i].Tipo != "campo")
                {
                    continue;
                }

                if (i + 1 >= linea.Trozos.Count)
                {
                    continue;
                }

                var siguiente = linea.Trozos[i + 1];
                if (siguiente.Tipo == "texto" && siguiente.Texto.Length > 0 && !char.IsWhiteSpace(siguiente.Texto[0]))
                {
                    siguiente.Texto = " " + siguiente.Texto;
                }
            }
        }
    }

    private static void Presentar(List<LineaCartaDto> lineas)
    {
        AgruparVinetas(lineas);
        AsegurarEspacioTrasCampos(lineas);
        for (var i = 0; i < lineas.Count; i++)
        {
            var linea = lineas[i];
            var texto = string.Concat(linea.Trozos.Select(trozo => trozo.Tipo == "texto" ? trozo.Texto : "")).Trim();
            if (i > 0 && (linea.Vineta || linea.Tabla is not null
                || texto.StartsWith("Se adjuntan los siguientes soportes", StringComparison.OrdinalIgnoreCase)))
            {
                lineas[i - 1].Espacio = true;
            }

            if (linea.Vineta || linea.Tabla is not null || linea.Salto)
            {
                continue;
            }

            if (linea.Trozos.Any(trozo => trozo.Clave is "fecha" or "titulo" or "codigo" or "patrocinador")
                || texto.Equals("Floridablanca - Colombia", StringComparison.OrdinalIgnoreCase))
            {
                linea.Espacio = true;
            }

            if (texto.TrimEnd('.').Equals("Cordialmente", StringComparison.OrdinalIgnoreCase))
            {
                linea.Espacio = true;
                if (i > 0)
                {
                    lineas[i - 1].Espacio = true;
                }
            }

            if (linea.Trozos.Any(trozo => trozo.Amplio && trozo.Clave != "titulo") && i > 0)
            {
                lineas[i - 1].Espacio = true;
                linea.Espacio = true;
            }
        }
    }

    private static void AgruparVinetas(List<LineaCartaDto> lineas)
    {
        for (var i = 0; i < lineas.Count; i++)
        {
            var texto = TextoVisible(lineas[i]).TrimStart();
            if (!texto.StartsWith('"') && !texto.StartsWith('“') && !texto.StartsWith('•'))
            {
                continue;
            }

            lineas[i].Vineta = true;
            QuitarMarca(lineas[i]);
            while (i + 1 < lineas.Count && EsContinuacionVineta(lineas[i + 1]))
            {
                var extra = TextoVisible(lineas[i + 1]).Trim();
                lineas[i].Trozos.Add(new TrozoCartaDto { Tipo = "texto", Texto = " " + extra });
                lineas.RemoveAt(i + 1);
            }
        }
    }

    private static bool EsContinuacionVineta(LineaCartaDto linea)
    {
        if (linea.Trozos.Any(trozo => trozo.Tipo is "campo" or "firma"))
        {
            return false;
        }

        var texto = TextoVisible(linea).Trim();
        if (texto.Length == 0 || texto.StartsWith('"') || texto.StartsWith('“') || texto.StartsWith('•'))
        {
            return false;
        }

        if (texto.TrimEnd('.').Equals("Cordialmente", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Se adjuntan los siguientes soportes", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return texto.Any(caracter => caracter is not ('_' or '-' or ' '));
    }

    private static void QuitarMarca(LineaCartaDto linea)
    {
        var primero = linea.Trozos.FirstOrDefault(trozo => trozo.Tipo == "texto" && !string.IsNullOrWhiteSpace(trozo.Texto));
        if (primero is null)
        {
            return;
        }

        primero.Texto = primero.Texto.TrimStart().TrimStart('"', '“', '•').TrimStart();
    }

    private static string TextoVisible(LineaCartaDto linea) =>
        string.Concat(linea.Trozos.Select(trozo => trozo.Tipo == "texto" ? trozo.Texto : trozo.Texto));

    private static string ClaveEnLinea(string linea, string interior, ref int libre, HashSet<string> claves)
    {
        if (EsNombre(interior) || EsNombre(linea))
        {
            return Reservar("investigador", ref libre, claves);
        }

        if (linea.Contains("Protocolo", StringComparison.OrdinalIgnoreCase))
        {
            return Reservar("codigo", ref libre, claves);
        }

        if (linea.Contains("Patrocinador", StringComparison.OrdinalIgnoreCase))
        {
            return Reservar("patrocinador", ref libre, claves);
        }

        if (linea.Contains("Floridablanca", StringComparison.OrdinalIgnoreCase))
        {
            return Reservar("fecha", ref libre, claves);
        }

        if (interior.Contains("Especialidad", StringComparison.OrdinalIgnoreCase)
            || linea.Contains("Especialidad", StringComparison.OrdinalIgnoreCase))
        {
            return Reservar("especialidad", ref libre, claves);
        }

        return SiguienteLibre(ref libre, claves);
    }

    private static string Reservar(string clave, ref int libre, HashSet<string> claves)
    {
        if (claves.Add(clave))
        {
            return clave;
        }

        return SiguienteLibre(ref libre, claves);
    }

    private static string SiguienteLibre(ref int libre, HashSet<string> claves)
    {
        string clave;
        do
        {
            libre++;
            clave = $"libre-{libre}";
        }
        while (!claves.Add(clave));

        return clave;
    }

    private static bool EsInicioDeTitulo(string texto)
    {
        var valor = texto.TrimStart();
        return valor.StartsWith('"') || valor.StartsWith('“') || valor.StartsWith("Estudio de", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContinuaCampo(List<LineaCartaDto> resultado, string crudo)
    {
        if (resultado.Count == 0 || resultado[^1].Trozos.Count == 0 || EsInicioDeTitulo(crudo))
        {
            return false;
        }

        var ultimo = resultado[^1].Trozos[^1];
        return ultimo.Tipo == "campo"
            && ultimo.Clave is not ("titulo" or "codigo" or "patrocinador" or "fecha" or "investigador" or "especialidad")
            && !ultimo.Texto.EndsWith('.');
    }

    private static bool EsFirma(string texto) =>
        texto.Contains("FIRMA", StringComparison.OrdinalIgnoreCase);

    private static bool EsNombre(string texto) =>
        texto.Contains("Nombre del Investigador", StringComparison.OrdinalIgnoreCase);

    private static LineaCartaDto LeerTabla(string cuerpo, ref int libre, HashSet<string> claves)
    {
        var partes = cuerpo.Split("::", StringSplitOptions.None);
        var columnas = partes[0].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        var filas = new List<List<TrozoCartaDto>>();
        for (var indice = 1; indice < partes.Length; indice++)
        {
            if (partes[indice].Length == 0)
            {
                continue;
            }

            var fila = new List<TrozoCartaDto>();
            foreach (var celda in partes[indice].Split('|'))
            {
                var coincidencia = Marca.Match(celda);
                var texto = coincidencia.Success ? Limpiar(coincidencia.Groups[1].Value) : Limpiar(celda);
                fila.Add(Campo(SiguienteLibre(ref libre, claves), texto, false));
            }

            while (fila.Count < columnas.Count)
            {
                fila.Add(Campo(SiguienteLibre(ref libre, claves), "", false));
            }

            if (fila.Count > columnas.Count)
            {
                fila.RemoveRange(columnas.Count, fila.Count - columnas.Count);
            }

            filas.Add(fila);
        }

        return new LineaCartaDto
        {
            Tabla = new TablaCartaDto { Columnas = columnas, Filas = filas }
        };
    }

    private static LineaCartaDto Linea(TrozoCartaDto trozo) => new() { Trozos = [trozo] };

    private static TrozoCartaDto Campo(string clave, string texto, bool amplio) => new()
    {
        Tipo = "campo",
        Clave = clave,
        Texto = texto,
        Amplio = amplio
    };

    private static string Limpiar(string texto)
    {
        var valor = texto.Replace('\u00a0', ' ').Trim();
        valor = Regex.Replace(valor, @"\s+", " ");
        return valor.Trim('"', '“', '”', '\'', '«', '»', '[', ']', ' ').Trim();
    }

    private static IReadOnlyList<CartaFuente> Cargar()
    {
        var recurso = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("CentroInvestigaciones.Shared.cartas-estudio.json");
        if (recurso is null)
        {
            return [];
        }

        using (recurso)
        {
            return JsonSerializer.Deserialize<List<CartaFuente>>(recurso, Json) ?? [];
        }
    }

    private sealed class CartaFuente
    {
        public string Codigo { get; set; } = "";

        public string Nombre { get; set; } = "";

        public List<string> Lineas { get; set; } = [];
    }
}
