using System.Text.Json;
using CentroInvestigaciones.Application.HojasVida;
using CentroInvestigaciones.Domain;
using PdfSharp.Pdf;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenHojasVidaLocal : IAlmacenHojasVida
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _ruta;
    private readonly string _carpetaDatos;
    private readonly IDepositoArchivos _deposito;
    private readonly Lock _bloqueo = new();
    private readonly List<HojaVida> _hojas = [];

    public AlmacenHojasVidaLocal(IWebHostEnvironment entorno, IDepositoArchivos deposito)
    {
        _deposito = deposito;
        _carpetaDatos = Path.Combine(entorno.ContentRootPath, "datos-locales", "hojas-de-vida");
        Directory.CreateDirectory(_carpetaDatos);
        _ruta = Path.Combine(entorno.ContentRootPath, "datos-locales", "hojas-de-vida.json");
        Cargar();
        var importadas = ImportadorHojasVida.ImportarSiCorresponde(_carpetaDatos, _hojas, _deposito);
        var ejemplo = SembrarEjemplo();
        if (importadas > 0 || ejemplo)
        {
            GuardarSinBloqueo();
        }
    }

    public IReadOnlyList<HojaVida> Listar()
    {
        lock (_bloqueo)
        {
            return _hojas.OrderBy(item => item.NombreCompleto).ToList();
        }
    }

    public HojaVida? Buscar(Guid id)
    {
        lock (_bloqueo)
        {
            return _hojas.FirstOrDefault(item => item.Id == id);
        }
    }

    public HojaVida Crear(string nombreCompleto, string tituloAcademico, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = new HojaVida
            {
                Id = Guid.NewGuid(),
                NombreCompleto = nombreCompleto.Trim(),
                TituloAcademico = tituloAcademico.Trim(),
                Version = 1,
                FechaEdicionUtc = DateTime.UtcNow
            };
            Registrar(hoja, usuario, "Alta de persona");
            _hojas.Add(hoja);
            GuardarSinBloqueo();
            return hoja;
        }
    }

    public (HojaVida Hoja, bool VersionNueva) Actualizar(Guid id, HojaVida datos, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(id);
            var antes = FirmaDatos(hoja);
            CopiarDatos(hoja, datos);
            var cambio = antes != FirmaDatos(hoja);
            if (cambio)
            {
                hoja.Version++;
                hoja.FechaEdicionUtc = DateTime.UtcNow;
                Registrar(hoja, usuario, "Actualización de datos");
                GuardarSinBloqueo();
            }

            return (hoja, cambio);
        }
    }

    public bool Eliminar(Guid id)
    {
        lock (_bloqueo)
        {
            var hoja = _hojas.FirstOrDefault(item => item.Id == id);
            if (hoja is null)
            {
                return false;
            }

            _deposito.EliminarHoja(id);
            _hojas.Remove(hoja);
            GuardarSinBloqueo();
            return true;
        }
    }

    public SoporteHoja AgregarSoporte(Guid hojaId, string nombre, string tipo, Stream contenido, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            var limpio = Path.GetFileName(nombre);
            var existente = hoja.Soportes.FirstOrDefault(item =>
                string.Equals(item.Nombre, limpio, StringComparison.OrdinalIgnoreCase));
            if (existente is not null)
            {
                _deposito.Eliminar(existente.Ruta);
                existente.Ruta = _deposito.Guardar(hojaId, "soportes", $"{existente.Id:N}{Path.GetExtension(limpio)}", contenido);
                existente.Tipo = tipo;
                SubirVersion(hoja, usuario, $"Soporte reemplazado: {limpio}");
                GuardarSinBloqueo();
                return existente;
            }

            var soporte = new SoporteHoja
            {
                Id = Guid.NewGuid(),
                Nombre = limpio,
                Tipo = tipo,
                Orden = hoja.Soportes.Count == 0 ? 0 : hoja.Soportes.Max(item => item.Orden) + 1
            };
            soporte.Ruta = _deposito.Guardar(hojaId, "soportes", $"{soporte.Id:N}{Path.GetExtension(limpio)}", contenido);
            hoja.Soportes.Add(soporte);
            SubirVersion(hoja, usuario, $"Soporte agregado: {limpio}");
            GuardarSinBloqueo();
            return soporte;
        }
    }

    public void ReordenarSoportes(Guid hojaId, IReadOnlyList<Guid> orden, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            var anterior = string.Join(",", hoja.Soportes.OrderBy(item => item.Orden).Select(item => item.Id));
            var indice = 0;
            foreach (var id in orden.Distinct())
            {
                var soporte = hoja.Soportes.FirstOrDefault(item => item.Id == id);
                if (soporte is not null)
                {
                    soporte.Orden = indice++;
                }
            }

            var actual = string.Join(",", hoja.Soportes.OrderBy(item => item.Orden).Select(item => item.Id));
            if (anterior == actual)
            {
                return;
            }

            SubirVersion(hoja, usuario, "Orden de soportes");
            GuardarSinBloqueo();
        }
    }

    public bool EliminarSoporte(Guid hojaId, Guid soporteId, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            var soporte = hoja.Soportes.FirstOrDefault(item => item.Id == soporteId);
            if (soporte is null)
            {
                return false;
            }

            _deposito.Eliminar(soporte.Ruta);
            hoja.Soportes.Remove(soporte);
            SubirVersion(hoja, usuario, $"Soporte eliminado: {soporte.Nombre}");
            GuardarSinBloqueo();
            return true;
        }
    }

    public void GuardarFirma(Guid hojaId, string nombre, string tipo, string fecha, Stream contenido, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            if (!string.IsNullOrWhiteSpace(hoja.FirmaRuta))
            {
                _deposito.Eliminar(hoja.FirmaRuta);
            }

            var limpio = Path.GetFileName(nombre);
            hoja.FirmaNombre = limpio;
            hoja.FirmaTipo = tipo;
            hoja.FirmaFecha = fecha;
            hoja.FirmaRuta = _deposito.Guardar(hojaId, "firma", limpio, contenido);
            SubirVersion(hoja, usuario, "Firma actualizada");
            GuardarSinBloqueo();
        }
    }

    public bool QuitarFirma(Guid hojaId, string usuario)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            if (string.IsNullOrWhiteSpace(hoja.FirmaRuta) && string.IsNullOrWhiteSpace(hoja.FirmaFecha))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(hoja.FirmaRuta))
            {
                _deposito.Eliminar(hoja.FirmaRuta);
            }

            hoja.FirmaRuta = null;
            hoja.FirmaNombre = null;
            hoja.FirmaTipo = null;
            hoja.FirmaFecha = "";
            SubirVersion(hoja, usuario, "Firma retirada");
            GuardarSinBloqueo();
            return true;
        }
    }

    public Stream AbrirSoporte(Guid hojaId, Guid soporteId)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            var soporte = hoja.Soportes.FirstOrDefault(item => item.Id == soporteId)
                ?? throw new KeyNotFoundException();
            return _deposito.Abrir(soporte.Ruta);
        }
    }

    public Stream? AbrirFirma(Guid hojaId)
    {
        lock (_bloqueo)
        {
            var hoja = Exigir(hojaId);
            return string.IsNullOrWhiteSpace(hoja.FirmaRuta) ? null : _deposito.Abrir(hoja.FirmaRuta);
        }
    }

    private bool SembrarEjemplo()
    {
        if (_hojas.Count > 0)
        {
            return false;
        }

        var hoja = new HojaVida
        {
            Id = Guid.NewGuid(),
            NombreCompleto = "Ana María Ejemplo",
            TituloAcademico = "Médica ejemplo",
            DirInstitucion = "Fundación Oftalmológica de Santander",
            DirCiudad = "Floridablanca",
            DirEstado = "Santander",
            DirPais = "Colombia",
            GcpSiNo = "No",
            OtrosSiNo = "No",
            Version = 1,
            FechaEdicionUtc = DateTime.UtcNow,
            Formacion =
            [
                new FormacionHoja
                {
                    Institucion = "Universidad de ejemplo, Bucaramanga",
                    Titulo = "Médica",
                    Anio = "2018",
                    Area = "Medicina"
                }
            ]
        };
        Registrar(hoja, "sistema", "Persona de ejemplo");
        var soporte = new SoporteHoja
        {
            Id = Guid.NewGuid(),
            Nombre = "Soporte de ejemplo.pdf",
            Tipo = "application/pdf",
            Orden = 0
        };
        using var pdf = PdfEjemplo();
        soporte.Ruta = _deposito.Guardar(hoja.Id, "soportes", $"{soporte.Id:N}.pdf", pdf);
        hoja.Soportes.Add(soporte);
        _hojas.Add(hoja);
        return true;
    }

    private static MemoryStream PdfEjemplo()
    {
        var documento = new PdfDocument();
        documento.AddPage();
        var memoria = new MemoryStream();
        documento.Save(memoria);
        memoria.Position = 0;
        return memoria;
    }

    private HojaVida Exigir(Guid id) =>
        _hojas.FirstOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException();

    private static void SubirVersion(HojaVida hoja, string usuario, string nota)
    {
        hoja.Version++;
        hoja.FechaEdicionUtc = DateTime.UtcNow;
        Registrar(hoja, usuario, nota);
    }

    private static void Registrar(HojaVida hoja, string usuario, string nota)
    {
        hoja.Historial.Add(new EventoHojaVida
        {
            Version = hoja.Version,
            FechaUtc = hoja.FechaEdicionUtc,
            Usuario = usuario,
            Nota = nota
        });
    }

    private static void CopiarDatos(HojaVida destino, HojaVida origen)
    {
        destino.NombreCompleto = origen.NombreCompleto.Trim();
        destino.TituloAcademico = origen.TituloAcademico.Trim();
        destino.DirInstitucion = origen.DirInstitucion.Trim();
        destino.DirCompleta = origen.DirCompleta.Trim();
        destino.DirCiudad = origen.DirCiudad.Trim();
        destino.DirEstado = origen.DirEstado.Trim();
        destino.DirProvincia = origen.DirProvincia.Trim();
        destino.DirPais = origen.DirPais.Trim();
        destino.DirCodigo = origen.DirCodigo.Trim();
        destino.Telefono = origen.Telefono.Trim();
        destino.Extension = origen.Extension.Trim();
        destino.VinculacionModalidad = origen.VinculacionModalidad.Trim();
        destino.VinculacionFechaInicio = origen.VinculacionFechaInicio.Trim();
        destino.Formacion = origen.Formacion;
        destino.ExperienciaLaboral = origen.ExperienciaLaboral;
        destino.ExperienciaInvestigacion = origen.ExperienciaInvestigacion;
        destino.GcpSiNo = string.IsNullOrWhiteSpace(origen.GcpSiNo) ? "No" : origen.GcpSiNo.Trim();
        destino.GcpDetalle = origen.GcpSiNo == "Sí" ? origen.GcpDetalle.Trim() : "";
        destino.OtrosSiNo = string.IsNullOrWhiteSpace(origen.OtrosSiNo) ? "No" : origen.OtrosSiNo.Trim();
        destino.OtrosDetalle = origen.OtrosSiNo == "Sí" ? origen.OtrosDetalle.Trim() : "";
        destino.FirmaFecha = origen.FirmaFecha.Trim();
    }

    private static string FirmaDatos(HojaVida hoja) =>
        JsonSerializer.Serialize(new
        {
            hoja.NombreCompleto,
            hoja.TituloAcademico,
            hoja.DirInstitucion,
            hoja.DirCompleta,
            hoja.DirCiudad,
            hoja.DirEstado,
            hoja.DirProvincia,
            hoja.DirPais,
            hoja.DirCodigo,
            hoja.Telefono,
            hoja.Extension,
            hoja.VinculacionModalidad,
            hoja.VinculacionFechaInicio,
            hoja.Formacion,
            hoja.ExperienciaLaboral,
            hoja.ExperienciaInvestigacion,
            hoja.GcpSiNo,
            hoja.GcpDetalle,
            hoja.OtrosSiNo,
            hoja.OtrosDetalle,
            hoja.FirmaFecha
        });

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            return;
        }

        var hojas = JsonSerializer.Deserialize<List<HojaVida>>(File.ReadAllText(_ruta), Json);
        if (hojas is not null)
        {
            _hojas.AddRange(hojas);
        }
    }

    private void GuardarSinBloqueo()
    {
        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(_hojas, Json));
    }
}
