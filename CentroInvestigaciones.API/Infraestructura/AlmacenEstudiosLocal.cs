using System.Security.Cryptography;
using System.Text.Json;
using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenEstudiosLocal : IAlmacenEstudios
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _ruta;
    private readonly IDepositoEstudios _deposito;
    private readonly Lock _bloqueo = new();
    private readonly List<Estudio> _estudios = [];

    public AlmacenEstudiosLocal(IWebHostEnvironment entorno, IDepositoEstudios deposito)
    {
        _deposito = deposito;
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "estudios.json");
        Cargar();
    }

    public IReadOnlyList<Estudio> Listar()
    {
        lock (_bloqueo)
        {
            return _estudios.OrderByDescending(item => item.FechaCreacionUtc).ToList();
        }
    }

    public Estudio? Buscar(Guid id)
    {
        lock (_bloqueo)
        {
            return _estudios.FirstOrDefault(item => item.Id == id);
        }
    }

    public Estudio? BuscarPorToken(string token)
    {
        lock (_bloqueo)
        {
            return _estudios.FirstOrDefault(item => item.Token == token);
        }
    }

    public Estudio Crear(Estudio datos)
    {
        lock (_bloqueo)
        {
            var ahora = DateTime.UtcNow;
            datos.Id = Guid.NewGuid();
            datos.Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            datos.Conteo = (_estudios.Where(item => item.TipoRegistro == datos.TipoRegistro).Select(item => item.Conteo).DefaultIfEmpty(0).Max()) + 1;
            datos.Estado = EstadosEstudio.RecepcionIncompleta;
            datos.FechaCreacionUtc = ahora;
            datos.FechaEdicionUtc = ahora;
            datos.Formatos ??= new FormatosAcademicos();
            datos.Archivos ??= [];
            _estudios.Add(datos);
            GuardarSinBloqueo();
            return datos;
        }
    }

    public Estudio? ActualizarFase1(string token, Estudio datos)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Token == token);
            if (estudio is null || estudio.Estado != EstadosEstudio.RecepcionIncompleta)
            {
                return null;
            }

            estudio.Anio = datos.Anio;
            estudio.Titulo = datos.Titulo;
            estudio.Especialidad = datos.Especialidad;
            estudio.Tipo = datos.Tipo;
            estudio.Acronimo = datos.Acronimo;
            estudio.TipoInvestigacion = datos.TipoInvestigacion;
            estudio.Institucion = datos.Institucion;
            estudio.InvestigadorPrincipal = datos.InvestigadorPrincipal;
            estudio.Correo = datos.Correo;
            estudio.CoInvestigadores = datos.CoInvestigadores;
            estudio.Coordinador = datos.Coordinador;
            estudio.Patrocinador = datos.Patrocinador;
            estudio.Paises = datos.Paises;
            estudio.Objetivo = datos.Objetivo;
            estudio.Formatos = datos.Formatos ?? new FormatosAcademicos();
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public ArchivoEstudio? AgregarArchivo(Guid estudioId, string clave, string nombre, string tipoContenido, Stream contenido)
    {
        _ = tipoContenido;
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == estudioId);
            if (estudio is null)
            {
                return null;
            }

            var ruta = _deposito.Guardar(estudio.Id, nombre, contenido);
            var archivo = new ArchivoEstudio
            {
                Id = Guid.NewGuid(),
                Clave = clave,
                Nombre = nombre,
                TipoContenido = TipoDescarga.De(nombre),
                Ruta = ruta,
                FechaUtc = DateTime.UtcNow
            };
            estudio.Archivos.Add(archivo);
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return archivo;
        }
    }

    public void QuitarPorClave(Guid estudioId, string clave)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == estudioId);
            if (estudio is null)
            {
                return;
            }

            var quitar = estudio.Archivos.Where(item => item.Clave == clave).ToList();
            foreach (var archivo in quitar)
            {
                _deposito.Eliminar(archivo.Ruta);
                estudio.Archivos.Remove(archivo);
            }

            if (quitar.Count > 0)
            {
                GuardarSinBloqueo();
            }
        }
    }

    public Estudio? SolicitarCorreccion(Guid id, string mensaje)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null || estudio.Estado is not (EstadosEstudio.RecepcionIncompleta or EstadosEstudio.Fase2EnRevision))
            {
                return null;
            }

            var fase2 = estudio.Estado == EstadosEstudio.Fase2EnRevision;
            estudio.MensajeCorreccion = mensaje.Trim();
            estudio.Estado = fase2 ? EstadosEstudio.AprobadoComite : EstadosEstudio.RecepcionIncompleta;
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public Estudio? EmitirAval(Guid id, string mensaje)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null || estudio.Estado != EstadosEstudio.RecepcionIncompleta)
            {
                return null;
            }

            estudio.MensajeAval = mensaje.Trim();
            estudio.Estado = EstadosEstudio.AprobadoComite;
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public Estudio? GuardarFase2(string token, string numeroAprobacionCei)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Token == token);
            if (estudio is null || estudio.Estado != EstadosEstudio.AprobadoComite)
            {
                return null;
            }

            estudio.NumeroAprobacionCei = numeroAprobacionCei.Trim();
            estudio.Estado = EstadosEstudio.Fase2EnRevision;
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public Estudio? Finalizar(Guid id)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null || estudio.Estado != EstadosEstudio.Fase2EnRevision)
            {
                return null;
            }

            estudio.Estado = EstadosEstudio.AprobadoCei;
            estudio.FechaEdicionUtc = DateTime.UtcNow;
            GuardarSinBloqueo();
            return estudio;
        }
    }

    public void MarcarAvisoRecibido(Guid id, bool enviado)
    {
        lock (_bloqueo)
        {
            var estudio = _estudios.FirstOrDefault(item => item.Id == id);
            if (estudio is null)
            {
                return;
            }

            estudio.AvisoRecibidoEnviado = enviado;
            GuardarSinBloqueo();
        }
    }

    public Stream? Abrir(Guid estudioId, Guid archivoId)
    {
        lock (_bloqueo)
        {
            var archivo = _estudios.FirstOrDefault(item => item.Id == estudioId)?.Archivos.FirstOrDefault(item => item.Id == archivoId);
            return archivo is null ? null : _deposito.Abrir(archivo.Ruta);
        }
    }

    public Stream? AbrirPorToken(string token, Guid archivoId)
    {
        lock (_bloqueo)
        {
            var archivo = _estudios.FirstOrDefault(item => item.Token == token)?.Archivos.FirstOrDefault(item => item.Id == archivoId);
            return archivo is null ? null : _deposito.Abrir(archivo.Ruta);
        }
    }

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            return;
        }

        var lista = JsonSerializer.Deserialize<List<Estudio>>(File.ReadAllText(_ruta), Json);
        if (lista is not null)
        {
            _estudios.AddRange(lista);
        }
    }

    private void GuardarSinBloqueo()
    {
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(_estudios, Json));
    }
}
