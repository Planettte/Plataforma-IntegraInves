using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using CentroInvestigaciones.Application.Protocolos;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenProtocolosLocal : IAlmacenProtocolos
{
    private const int TopeTexto = 30_000;
    private const int TopeFilas = 40;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();
    private readonly List<Protocolo> _protocolos = [];

    public AlmacenProtocolosLocal(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "protocolos.json");
        Cargar();
    }

    public IReadOnlyList<Protocolo> Listar()
    {
        lock (_bloqueo)
        {
            return _protocolos.OrderByDescending(item => item.CreadoEl).ToList();
        }
    }

    public Protocolo? Buscar(Guid id)
    {
        lock (_bloqueo)
        {
            return Clonar(_protocolos.FirstOrDefault(item => item.Id == id));
        }
    }

    public Protocolo? BuscarPorToken(string token)
    {
        lock (_bloqueo)
        {
            var limpio = token.Trim();
            return Clonar(_protocolos.FirstOrDefault(item => item.Token == limpio));
        }
    }

    public Protocolo Crear(Protocolo protocolo)
    {
        lock (_bloqueo)
        {
            var nuevo = Preparar(protocolo, null);
            nuevo.Id = Guid.NewGuid();
            nuevo.Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            nuevo.Estado = EstadosProtocolo.EnRevision;
            nuevo.CreadoEl = DateTimeOffset.Now;
            nuevo.EditadoEl = nuevo.CreadoEl;
            _protocolos.Add(nuevo);
            Guardar();
            return Clonar(nuevo)!;
        }
    }

    public Protocolo Reenviar(string token, Protocolo protocolo)
    {
        lock (_bloqueo)
        {
            var actual = _protocolos.FirstOrDefault(item => item.Token == token.Trim())
                ?? throw new InvalidOperationException("No se encontró el protocolo.");
            if (actual.Estado != EstadosProtocolo.Correccion)
            {
                throw new InvalidOperationException("Este protocolo no está en corrección.");
            }

            var preparado = Preparar(protocolo, actual.Formato);
            actual.Nombre = preparado.Nombre;
            actual.Correo = preparado.Correo;
            actual.RevisorId = preparado.RevisorId;
            actual.Respuestas = preparado.Respuestas;
            actual.Conocimiento = preparado.Conocimiento;
            actual.Tecnologico = preparado.Tecnologico;
            actual.Capacidad = preparado.Capacidad;
            actual.CronogramaFoscal = preparado.CronogramaFoscal;
            actual.CronogramaUdes = preparado.CronogramaUdes;
            actual.Presupuesto = preparado.Presupuesto;
            actual.EticaPoblacion = preparado.EticaPoblacion;
            actual.EticaIntervencion = preparado.EticaIntervencion;
            actual.EticaInstrumentos = preparado.EticaInstrumentos;
            actual.Estado = EstadosProtocolo.EnRevision;
            actual.EditadoEl = DateTimeOffset.Now;
            Guardar();
            return Clonar(actual)!;
        }
    }

    public Protocolo Corregir(Guid id, IReadOnlyDictionary<string, string> comentarios)
    {
        lock (_bloqueo)
        {
            var actual = _protocolos.FirstOrDefault(item => item.Id == id)
                ?? throw new InvalidOperationException("No se encontró el protocolo.");
            if (actual.Estado == EstadosProtocolo.Aprobado)
            {
                throw new InvalidOperationException("El protocolo ya fue aceptado.");
            }

            var limpios = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var par in comentarios)
            {
                if (!CatalogoProtocolo.ClaveComentario(actual.Formato, par.Key))
                {
                    continue;
                }

                var texto = par.Value.Trim();
                if (texto.Length == 0)
                {
                    continue;
                }

                if (texto.Length > 4_000)
                {
                    throw new InvalidOperationException("Un comentario es demasiado largo.");
                }

                limpios[par.Key] = texto;
            }

            if (limpios.Count == 0)
            {
                throw new InvalidOperationException("Escribe al menos un comentario.");
            }

            actual.Comentarios = limpios;
            actual.Estado = EstadosProtocolo.Correccion;
            actual.EditadoEl = DateTimeOffset.Now;
            Guardar();
            return Clonar(actual)!;
        }
    }

    public Protocolo Aceptar(Guid id)
    {
        lock (_bloqueo)
        {
            var actual = _protocolos.FirstOrDefault(item => item.Id == id)
                ?? throw new InvalidOperationException("No se encontró el protocolo.");
            if (actual.Estado != EstadosProtocolo.EnRevision)
            {
                throw new InvalidOperationException("Solo se acepta un protocolo en revisión.");
            }

            actual.Estado = EstadosProtocolo.Aprobado;
            actual.EditadoEl = DateTimeOffset.Now;
            Guardar();
            return Clonar(actual)!;
        }
    }

    private static Protocolo Preparar(Protocolo origen, string? formatoFijo)
    {
        var formato = formatoFijo ?? origen.Formato.Trim();
        if (!FormatosProtocolo.Valido(formato))
        {
            throw new InvalidOperationException("Selecciona el formato.");
        }

        var nombre = origen.Nombre.Trim();
        if (nombre.Length == 0 || nombre.Length > 200)
        {
            throw new InvalidOperationException("Ingresa el nombre completo.");
        }

        var correo = origen.Correo.Trim();
        if (!MailAddress.TryCreate(correo, out _))
        {
            throw new InvalidOperationException("Ingresa un correo válido.");
        }

        if (origen.RevisorId == Guid.Empty)
        {
                throw new InvalidOperationException("Selecciona el médico de apoyo a la investigación.");
        }

        var protocolo = new Protocolo
        {
            Formato = formato,
            Nombre = nombre,
            Correo = correo,
            RevisorId = origen.RevisorId,
            Respuestas = Textos(formato, origen.Respuestas),
            Conocimiento = formato == FormatosProtocolo.Foscal
                ? Filas(origen.Conocimiento, fila => string.IsNullOrWhiteSpace(fila.Resultado) && string.IsNullOrWhiteSpace(fila.Indicador) && string.IsNullOrWhiteSpace(fila.Beneficiario), fila => new FilaConocimientoProtocolo
                {
                    Resultado = Recortar(fila.Resultado),
                    Indicador = Recortar(fila.Indicador),
                    Beneficiario = Recortar(fila.Beneficiario)
                })
                : [],
            Tecnologico = formato == FormatosProtocolo.Foscal ? Productos(origen.Tecnologico) : [],
            Capacidad = formato == FormatosProtocolo.Foscal ? Productos(origen.Capacidad) : [],
            CronogramaFoscal = formato == FormatosProtocolo.Foscal ? Cronograma(origen.CronogramaFoscal, conResponsable: true) : [],
            CronogramaUdes = formato == FormatosProtocolo.Udes ? Cronograma(origen.CronogramaUdes, conResponsable: false) : [],
            Presupuesto = formato == FormatosProtocolo.Udes ? Presupuesto(origen.Presupuesto) : [],
            EticaPoblacion = formato == FormatosProtocolo.Udes ? Marcas(origen.EticaPoblacion, CatalogoProtocolo.EticaPoblacion.Length) : [],
            EticaIntervencion = formato == FormatosProtocolo.Udes ? Marcas(origen.EticaIntervencion, CatalogoProtocolo.EticaIntervencion.Length) : [],
            EticaInstrumentos = formato == FormatosProtocolo.Udes ? Marcas(origen.EticaInstrumentos, CatalogoProtocolo.EticaInstrumentos.Length) : []
        };
        return protocolo;
    }

    private static Dictionary<string, string> Textos(string formato, Dictionary<string, string>? origen)
    {
        var textos = new Dictionary<string, string>(StringComparer.Ordinal);
        if (origen is null)
        {
            return textos;
        }

        foreach (var par in origen)
        {
            if (!CatalogoProtocolo.ClaveTexto(formato, par.Key))
            {
                continue;
            }

            var texto = par.Value.Trim();
            if (texto.Length == 0)
            {
                continue;
            }

            if (texto.Length > TopeTexto)
            {
                throw new InvalidOperationException("Un campo es demasiado largo.");
            }

            textos[par.Key] = texto;
        }

        return textos;
    }

    private static List<FilaProductoProtocolo> Productos(IReadOnlyList<FilaProductoProtocolo>? origen) =>
        Filas(origen, fila =>
            string.IsNullOrWhiteSpace(fila.Objetivo)
            && string.IsNullOrWhiteSpace(fila.Resultado)
            && string.IsNullOrWhiteSpace(fila.Indicador)
            && string.IsNullOrWhiteSpace(fila.Beneficiario), fila => new FilaProductoProtocolo
        {
            Objetivo = Recortar(fila.Objetivo),
            Resultado = Recortar(fila.Resultado),
            Indicador = Recortar(fila.Indicador),
            Beneficiario = Recortar(fila.Beneficiario)
        });

    private static List<FilaCronogramaProtocolo> Cronograma(IReadOnlyList<FilaCronogramaProtocolo>? origen, bool conResponsable) =>
        Filas(origen, fila => string.IsNullOrWhiteSpace(fila.Actividad) && string.IsNullOrWhiteSpace(fila.Responsable) && !Meses(fila.Meses).Contains(true), fila => new FilaCronogramaProtocolo
        {
            Actividad = Recortar(fila.Actividad),
            Responsable = conResponsable ? Recortar(fila.Responsable) : "",
            Meses = Meses(fila.Meses)
        });

    private static List<T> Filas<T>(IReadOnlyList<T>? origen, Func<T, bool> vacia, Func<T, T> copiar)
    {
        var filas = new List<T>();
        if (origen is null)
        {
            return filas;
        }

        foreach (var fila in origen)
        {
            if (vacia(fila))
            {
                continue;
            }

            filas.Add(copiar(fila));
            if (filas.Count > TopeFilas)
            {
                throw new InvalidOperationException("Hay demasiadas filas.");
            }
        }

        return filas;
    }

    private static List<FilaPresupuestoProtocolo> Presupuesto(IReadOnlyList<FilaPresupuestoProtocolo>? origen)
    {
        var filas = new List<FilaPresupuestoProtocolo>();
        for (var indice = 0; indice < CatalogoProtocolo.Rubros.Length; indice++)
        {
            var valores = new List<string>();
            var recibida = origen is not null && indice < origen.Count ? origen[indice].Valores : [];
            for (var columna = 0; columna < CatalogoProtocolo.Fuentes.Length; columna++)
            {
                var texto = columna < recibida.Count ? recibida[columna].Trim() : "";
                if (texto.Length > 40)
                {
                    throw new InvalidOperationException("Un valor del presupuesto es demasiado largo.");
                }

                valores.Add(texto);
            }

            filas.Add(new FilaPresupuestoProtocolo
            {
                Rubro = CatalogoProtocolo.Rubros[indice],
                Valores = valores
            });
        }

        return filas;
    }

    private static List<bool> Meses(IReadOnlyList<bool>? origen) => Marcas(origen, 12);

    private static List<bool> Marcas(IReadOnlyList<bool>? origen, int cantidad)
    {
        var marcas = new List<bool>(cantidad);
        for (var indice = 0; indice < cantidad; indice++)
        {
            marcas.Add(origen is not null && indice < origen.Count && origen[indice]);
        }

        return marcas;
    }

    private static string Recortar(string? valor)
    {
        var texto = valor?.Trim() ?? "";
        if (texto.Length > TopeTexto)
        {
            throw new InvalidOperationException("Un campo es demasiado largo.");
        }

        return texto;
    }

    private static Protocolo? Clonar(Protocolo? protocolo) =>
        protocolo is null ? null : JsonSerializer.Deserialize<Protocolo>(JsonSerializer.Serialize(protocolo, Json), Json);

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            return;
        }

        var foto = JsonSerializer.Deserialize<Foto>(File.ReadAllText(_ruta), Json) ?? new Foto();
        _protocolos.AddRange(foto.Protocolos);
    }

    private void Guardar()
    {
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(new Foto { Protocolos = _protocolos }, Json));
    }

    private sealed class Foto
    {
        public List<Protocolo> Protocolos { get; set; } = [];
    }
}
