using CentroInvestigaciones.Application.Estudios;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class ServicioFlujoEstudios
{
    private const string PermisoEstudios = "herramientas.comite.estudios";

    private readonly IAlmacenEstudios _almacen;
    private readonly IEnviadorCorreo _correo;
    private readonly GeneradorPdfEstudio _pdf;
    private readonly IAlmacenNucleo _nucleo;
    private readonly AlmacenNotificaciones _notificaciones;

    public ServicioFlujoEstudios(
        IAlmacenEstudios almacen,
        IEnviadorCorreo correo,
        GeneradorPdfEstudio pdf,
        IAlmacenNucleo nucleo,
        AlmacenNotificaciones notificaciones)
    {
        _almacen = almacen;
        _correo = correo;
        _pdf = pdf;
        _nucleo = nucleo;
        _notificaciones = notificaciones;
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> GuardarFase1Async(GuardarFase1EstudioDto solicitud, string? token, string origen, CancellationToken ct)
    {
        if (!Fase1Valida(solicitud))
        {
            return null;
        }

        var datos = ADominio(solicitud);
        var estudio = string.IsNullOrWhiteSpace(token)
            ? _almacen.Crear(datos)
            : _almacen.ActualizarFase1(token, datos);
        if (estudio is null)
        {
            return null;
        }

        GuardarFirma(estudio, ClavesArchivoEstudio.FirmaCartaSolicitud, solicitud.FirmaCartaSolicitud);
        GuardarFirma(estudio, ClavesArchivoEstudio.FirmaCartaIntencion, solicitud.FirmaCartaIntencion);
        GuardarFirma(estudio, ClavesArchivoEstudio.FirmaAcuerdo, solicitud.FirmaAcuerdo);
        GuardarFirma(estudio, ClavesArchivoEstudio.FirmaRegistro, solicitud.FirmaRegistro);
        GuardarFirma(estudio, ClavesArchivoEstudio.FirmaHojaVida, solicitud.FirmaHojaVida);
        if (estudio.TipoRegistro == TiposEstudio.Academico)
        {
            RegenerarPdfs(estudio);
        }

        var nuevo = string.IsNullOrWhiteSpace(token);
        var alInvestigador = await EnviarRecibidoAsync(estudio, ct);
        _almacen.MarcarAvisoRecibido(estudio.Id, alInvestigador);
        if (nuevo)
        {
            var aviso = $"Se ha recibido un registro para el estudio {estudio.Titulo}.";
            await NotificarEquipoAsync($"Nuevo registro — {estudio.Titulo}", "Nuevo registro", aviso, $"/comite/estudios/{estudio.Id}", origen, ct);
        }

        return (Refrescar(estudio.Id) ?? estudio, alInvestigador);
    }

    public (byte[] Pdf, string Nombre)? VistaPreviaPdf(GuardarFase1EstudioDto solicitud, int formato)
    {
        if (formato is < 1 or > 6)
        {
            return null;
        }

        var estudio = ADominio(solicitud);
        var firmas = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        AgregarFirma(firmas, ClavesArchivoEstudio.FirmaCartaSolicitud, solicitud.FirmaCartaSolicitud);
        AgregarFirma(firmas, ClavesArchivoEstudio.FirmaCartaIntencion, solicitud.FirmaCartaIntencion);
        AgregarFirma(firmas, ClavesArchivoEstudio.FirmaAcuerdo, solicitud.FirmaAcuerdo);
        AgregarFirma(firmas, ClavesArchivoEstudio.FirmaRegistro, solicitud.FirmaRegistro);
        AgregarFirma(firmas, ClavesArchivoEstudio.FirmaHojaVida, solicitud.FirmaHojaVida);
        var generado = _pdf.GenerarAcademicos(estudio, firmas);
        var item = generado[formato - 1];
        return (item.Pdf, item.Nombre);
    }

    private static void AgregarFirma(Dictionary<string, byte[]> firmas, string clave, string? valor)
    {
        var bytes = Decodificar(valor);
        if (bytes is { Length: > 0 } && ImagenValida.Es(bytes))
        {
            firmas[clave] = bytes;
        }
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> CorregirAsync(Guid id, string mensaje, string origen, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return null;
        }

        var actual = _almacen.Buscar(id);
        if (actual is null)
        {
            return null;
        }

        var fase2 = actual.Estado == EstadosEstudio.Fase2EnRevision;
        var estudio = _almacen.SolicitarCorreccion(id, mensaje);
        if (estudio is null)
        {
            return null;
        }

        var enviado = await EnviarCorreccionAsync(estudio, mensaje, origen, fase2, ct);
        if (enviado)
        {
            _almacen.MarcarAvisoRecibido(estudio.Id, true);
        }

        return (Refrescar(estudio.Id) ?? estudio, enviado);
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> AvalarAsync(Guid id, string mensaje, string origen, CancellationToken ct)
    {
        var estudio = _almacen.EmitirAval(id, mensaje ?? "");
        if (estudio is null || string.IsNullOrWhiteSpace(estudio.Correo))
        {
            return null;
        }

        var enviado = await EnviarAvalAsync(estudio, origen, ct);
        if (enviado)
        {
            _almacen.MarcarAvisoRecibido(estudio.Id, true);
        }

        return (Refrescar(estudio.Id) ?? estudio, enviado);
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> CerrarFase2Async(string token, string numero, string origen, CancellationToken ct)
    {
        var actual = _almacen.BuscarPorToken(token);
        if (actual is null
            || string.IsNullOrWhiteSpace(numero)
            || !Tiene(actual, ClavesArchivoEstudio.Fase2Aval)
            || !Tiene(actual, ClavesArchivoEstudio.Fase2Cei)
            || !Tiene(actual, ClavesArchivoEstudio.Fase2Acta))
        {
            return null;
        }

        var estudio = _almacen.GuardarFase2(token, numero);
        if (estudio is null)
        {
            return null;
        }

        var aviso = $"Se recibieron los documentos de la fase 2 del estudio {estudio.Titulo}.";
        await NotificarEquipoAsync($"Fase 2 recibida — {estudio.Titulo}", "Fase 2 recibida", aviso, $"/comite/estudios/{estudio.Id}", origen, ct);
        return (estudio, true);
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> FinalizarAsync(Guid id, CancellationToken ct)
    {
        var estudio = _almacen.Finalizar(id);
        if (estudio is null)
        {
            return null;
        }

        var enviado = await EnviarCierreAsync(estudio, ct);
        if (enviado)
        {
            _almacen.MarcarAvisoRecibido(estudio.Id, true);
        }

        return (Refrescar(estudio.Id) ?? estudio, enviado);
    }

    public async Task<(Estudio Estudio, bool CorreoEnviado)?> ReenviarAsync(Guid id, string tipo, string origen, CancellationToken ct)
    {
        var estudio = _almacen.Buscar(id);
        if (estudio is null || tipo == "correccion" && string.IsNullOrWhiteSpace(estudio.MensajeCorreccion))
        {
            return null;
        }

        var enviado = tipo switch
        {
            "recibido" => await EnviarRecibidoAsync(estudio, ct),
            "correccion" when !string.IsNullOrWhiteSpace(estudio.MensajeCorreccion) =>
                await EnviarCorreccionAsync(estudio, estudio.MensajeCorreccion, origen, estudio.Estado == EstadosEstudio.AprobadoComite, ct),
            "aval" => await EnviarAvalAsync(estudio, origen, ct),
            "cierre" => await EnviarCierreAsync(estudio, ct),
            _ => false
        };
        if (tipo == "recibido" || enviado)
        {
            _almacen.MarcarAvisoRecibido(estudio.Id, tipo == "recibido" ? enviado : true);
            estudio = Refrescar(estudio.Id) ?? estudio;
        }

        return tipo is "recibido" or "correccion" or "aval" or "cierre" ? (estudio, enviado) : null;
    }

    private Task<bool> EnviarRecibidoAsync(Estudio estudio, CancellationToken ct)
    {
        var titulo = System.Net.WebUtility.HtmlEncode(estudio.Titulo);
        var cuerpo = $"<p style=\"margin:0;\">Recibimos el registro del estudio <strong>{titulo}</strong> y se encuentra en revisión, se informarán novedades por este medio.</p>";
        return _correo.EnviarAsync(
            estudio.Correo,
            $"Registro recibido — {estudio.Titulo}",
            PlantillaCorreo.Armar("Registro recibido", estudio.InvestigadorPrincipal, cuerpo),
            null,
            ct);
    }

    private Task<bool> EnviarCorreccionAsync(Estudio estudio, string mensaje, string origen, bool fase2, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(origen))
        {
            return Task.FromResult(false);
        }

        var titulo = System.Net.WebUtility.HtmlEncode(estudio.Titulo);
        var detalle = System.Net.WebUtility.HtmlEncode(mensaje).Replace("\n", "<br>");
        var cuerpo = $"<p style=\"margin:0;\">Se ha revisado el registro del estudio <strong>{titulo}</strong> y se requieren las siguientes correcciones:</p>"
            + PlantillaCorreo.Recuadro(detalle);
        return _correo.EnviarAsync(
            estudio.Correo,
            $"Corrección requerida — {estudio.Titulo}",
            PlantillaCorreo.Armar(
                "Corrección requerida",
                estudio.InvestigadorPrincipal,
                cuerpo,
                $"{origen}/comite/estudios/externo/{estudio.Token}",
                fase2 ? "Corregir la fase 2" : "Corregir el registro"),
            null,
            ct);
    }

    private Task<bool> EnviarAvalAsync(Estudio estudio, string origen, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(origen))
        {
            return Task.FromResult(false);
        }

        var extra = string.IsNullOrWhiteSpace(estudio.MensajeAval)
            ? ""
            : PlantillaCorreo.Recuadro(System.Net.WebUtility.HtmlEncode(estudio.MensajeAval).Replace("\n", "<br>"));
        var cuerpo = "<p style=\"margin:0;\">El registro ha sido validado, a continuación se adjuntará el aval y el enlace para poder continuar con la fase 2.</p>" + extra;
        return _correo.EnviarAsync(
            estudio.Correo,
            $"Registro validado — {estudio.Titulo}",
            PlantillaCorreo.Armar(
                "Registro validado",
                estudio.InvestigadorPrincipal,
                cuerpo,
                $"{origen}/comite/estudios/externo/{estudio.Token}",
                "Continuar con la fase 2"),
            AdjuntoAval(estudio),
            ct);
    }

    private Task<bool> EnviarCierreAsync(Estudio estudio, CancellationToken ct)
    {
        var titulo = System.Net.WebUtility.HtmlEncode(estudio.Titulo);
        var cuerpo = $"<p style=\"margin:0;\">El registro del estudio <strong>{titulo}</strong> ha sido completado exitosamente.</p>";
        return _correo.EnviarAsync(
            estudio.Correo,
            $"Registro completado — {estudio.Titulo}",
            PlantillaCorreo.Armar("Registro completado", estudio.InvestigadorPrincipal, cuerpo),
            null,
            ct);
    }

    private async Task NotificarEquipoAsync(string asunto, string titulo, string mensaje, string ruta, string origen, CancellationToken ct)
    {
        var (usuarios, _) = await _nucleo.ListarUsuariosAsync(0, 500, null, nameof(EstadoCuenta.Activo), ct);
        var correos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hubo = false;
        foreach (var usuario in usuarios)
        {
            var permisos = await _nucleo.PermisosDeUsuarioAsync(usuario.Id, ct);
            if (!permisos.Contains(PermisoEstudios, StringComparer.Ordinal))
            {
                continue;
            }

            hubo = true;
            _notificaciones.Agregar(usuario.Id, titulo, mensaje, ruta);
            if (string.IsNullOrWhiteSpace(usuario.CorreoElectronico) || !correos.Add(usuario.CorreoElectronico))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(origen))
            {
                continue;
            }

            var cuerpo = $"<p style=\"margin:0;\">{System.Net.WebUtility.HtmlEncode(mensaje)}</p>";
            await _correo.EnviarAsync(
                usuario.CorreoElectronico,
                asunto,
                PlantillaCorreo.Armar(titulo, usuario.Nombre, cuerpo, $"{origen}{ruta}", "Abrir la herramienta"),
                null,
                ct);
        }

        if (!hubo && !string.IsNullOrWhiteSpace(_correo.DestinoComite) && !string.IsNullOrWhiteSpace(origen))
        {
            var cuerpo = $"<p style=\"margin:0;\">{System.Net.WebUtility.HtmlEncode(mensaje)}</p>";
            await _correo.EnviarAsync(
                _correo.DestinoComite,
                asunto,
                PlantillaCorreo.Armar(titulo, "", cuerpo, $"{origen}{ruta}", "Abrir la herramienta"),
                null,
                ct);
        }
    }

    private void RegenerarPdfs(Estudio estudio)
    {
        foreach (var clave in new[]
        {
            ClavesArchivoEstudio.PdfCartaSolicitud,
            ClavesArchivoEstudio.PdfCartaIntencion,
            ClavesArchivoEstudio.PdfAcuerdo,
            ClavesArchivoEstudio.PdfRegistro,
            ClavesArchivoEstudio.PdfHojaVida,
            ClavesArchivoEstudio.PdfResumen
        })
        {
            _almacen.QuitarPorClave(estudio.Id, clave);
        }

        var fresco = Refrescar(estudio.Id) ?? estudio;
        foreach (var (clave, nombre, pdf) in _pdf.GenerarAcademicos(fresco, Firmas(fresco)))
        {
            if (!FormatoEscrito(fresco, clave))
            {
                continue;
            }

            using var memoria = new MemoryStream(pdf);
            _almacen.AgregarArchivo(estudio.Id, clave, nombre, "application/pdf", memoria);
        }
    }

    private static bool FormatoEscrito(Estudio estudio, string clave)
    {
        var formatos = estudio.Formatos ?? new FormatosAcademicos();
        return clave switch
        {
            ClavesArchivoEstudio.PdfCartaSolicitud => Escrito(
                formatos.CartaSolicitud.Protocolo, formatos.CartaSolicitud.VersionProtocolo, formatos.CartaSolicitud.FechaProtocolo,
                formatos.CartaSolicitud.IdiomaProtocolo, formatos.CartaSolicitud.VersionConsentimiento, formatos.CartaSolicitud.FechaConsentimiento,
                formatos.CartaSolicitud.IdiomaConsentimiento, formatos.CartaSolicitud.VersionInstrumentos, formatos.CartaSolicitud.FechaInstrumentos,
                formatos.CartaSolicitud.Nombre),
            ClavesArchivoEstudio.PdfCartaIntencion => Escrito(formatos.CartaIntencion.TituloProtocolo, formatos.CartaIntencion.Nombre),
            ClavesArchivoEstudio.PdfAcuerdo => Escrito(
                formatos.Acuerdo.Nombre, formatos.Acuerdo.Rol, formatos.Acuerdo.Cedula, formatos.Acuerdo.Expedicion,
                formatos.Acuerdo.Protocolo, formatos.Acuerdo.NombreFirma),
            ClavesArchivoEstudio.PdfRegistro => Escrito(
                formatos.Registro.Nombres, formatos.Registro.Apellidos, formatos.Registro.Titulos, formatos.Registro.Correo,
                formatos.Registro.Institucion, formatos.Registro.Direccion, formatos.Registro.Telefono, formatos.Registro.Celular,
                formatos.Registro.Formacion, formatos.Registro.Servicio, formatos.Registro.Experiencia, formatos.Registro.Publicaciones),
            ClavesArchivoEstudio.PdfHojaVida => Escrito(
                formatos.HojaVida.Nombre, formatos.HojaVida.Titulos, formatos.HojaVida.Direccion, formatos.HojaVida.Telefono,
                formatos.HojaVida.Fax, formatos.HojaVida.Academica, formatos.HojaVida.Laboral, formatos.HojaVida.Investigacion, formatos.HojaVida.Gcp),
            ClavesArchivoEstudio.PdfResumen => ResumenEscrito(formatos.Resumen),
            _ => false
        };
    }

    private static bool ResumenEscrito(ResumenAnaliticoEstudio resumen) =>
        Escrito(
            resumen.Titulo, resumen.Linea, resumen.Grupo, resumen.Area, resumen.Institucion1, resumen.Institucion2, resumen.Institucion3,
            resumen.AnioResidencia, resumen.Problema, resumen.Justificacion, resumen.ObjetivoGeneral, resumen.Objetivo1, resumen.Objetivo2,
            resumen.Objetivo3, resumen.EstadoArte, resumen.TipoEstudio, resumen.Poblacion, resumen.Muestra, resumen.Inclusion, resumen.Exclusion,
            resumen.Recoleccion, resumen.Analisis, resumen.Divulgacion, resumen.FechaInicio, resumen.Riesgo, resumen.Confidencialidad,
            resumen.Seguridad, resumen.Consentimiento)
        || resumen.Investigadores.Any(item => Escrito(item.Nombre, item.Rol))
        || resumen.Presupuesto.Any(item => Escrito(item.Horas, item.Valor, item.Responsable, item.Observaciones))
        || resumen.Cronograma.Any(item => Escrito(item.Actividad, item.MesInicial, item.MesFinal));

    private static bool Escrito(params string?[] textos) =>
        textos.Any(item => !string.IsNullOrWhiteSpace(item));

    private void GuardarFirma(Estudio estudio, string clave, string? base64)
    {
        var bytes = Decodificar(base64);
        if (bytes is null || !ImagenValida.Es(bytes))
        {
            return;
        }

        _almacen.QuitarPorClave(estudio.Id, clave);
        using var memoria = new MemoryStream(bytes);
        _almacen.AgregarArchivo(estudio.Id, clave, clave + ".png", "image/png", memoria);
    }

    private Dictionary<string, byte[]> Firmas(Estudio estudio)
    {
        var mapa = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var archivo in estudio.Archivos.Where(item => ClavesArchivoEstudio.EsFirma(item.Clave)))
        {
            using var stream = _almacen.Abrir(estudio.Id, archivo.Id);
            if (stream is null)
            {
                continue;
            }

            using var memoria = new MemoryStream();
            stream.CopyTo(memoria);
            mapa[archivo.Clave] = memoria.ToArray();
        }

        return mapa;
    }

    private (string Nombre, byte[] Contenido, string Tipo)? AdjuntoAval(Estudio estudio)
    {
        var archivo = estudio.Archivos.LastOrDefault(item => item.Clave == ClavesArchivoEstudio.AvalComite);
        if (archivo is null)
        {
            return null;
        }

        using var stream = _almacen.Abrir(estudio.Id, archivo.Id);
        if (stream is null)
        {
            return null;
        }

        using var memoria = new MemoryStream();
        stream.CopyTo(memoria);
        return (archivo.Nombre, memoria.ToArray(), archivo.TipoContenido);
    }

    private Estudio? Refrescar(Guid id) => _almacen.Buscar(id);

    private static bool Tiene(Estudio estudio, string clave) => estudio.Archivos.Any(item => item.Clave == clave);

    private static bool Fase1Valida(GuardarFase1EstudioDto solicitud)
    {
        if (solicitud.Anio is < 2000 or > 2100
            || string.IsNullOrWhiteSpace(solicitud.Titulo)
            || string.IsNullOrWhiteSpace(solicitud.Institucion)
            || string.IsNullOrWhiteSpace(solicitud.InvestigadorPrincipal)
            || string.IsNullOrWhiteSpace(solicitud.Correo)
            || solicitud.TipoRegistro is not (TiposEstudio.Academico or TiposEstudio.Clinico))
        {
            return false;
        }

        if (solicitud.TipoRegistro == TiposEstudio.Academico)
        {
            return !string.IsNullOrWhiteSpace(solicitud.Especialidad) && !string.IsNullOrWhiteSpace(solicitud.Tipo);
        }

        return !string.IsNullOrWhiteSpace(solicitud.Tipo) && !string.IsNullOrWhiteSpace(solicitud.TipoInvestigacion);
    }

    private static Estudio ADominio(GuardarFase1EstudioDto solicitud) => new()
    {
        TipoRegistro = solicitud.TipoRegistro,
        Anio = solicitud.Anio,
        Titulo = solicitud.Titulo.Trim(),
        Especialidad = solicitud.Especialidad.Trim(),
        Tipo = solicitud.Tipo.Trim(),
        Acronimo = solicitud.Acronimo.Trim(),
        TipoInvestigacion = solicitud.TipoInvestigacion.Trim(),
        Institucion = solicitud.Institucion.Trim(),
        InvestigadorPrincipal = solicitud.InvestigadorPrincipal.Trim(),
        Correo = solicitud.Correo.Trim(),
        CoInvestigadores = solicitud.CoInvestigadores.Trim(),
        Coordinador = solicitud.Coordinador.Trim(),
        Patrocinador = solicitud.Patrocinador.Trim(),
        Paises = solicitud.Paises.Trim(),
        Objetivo = solicitud.Objetivo.Trim(),
        Formatos = MapearFormatos(solicitud.Formatos)
    };

    private static FormatosAcademicos MapearFormatos(FormatosAcademicosDto? origen)
    {
        origen ??= new FormatosAcademicosDto();
        return new FormatosAcademicos
        {
            CartaSolicitud = new CartaSolicitudEstudio
            {
                Protocolo = origen.CartaSolicitud.Protocolo,
                VersionProtocolo = origen.CartaSolicitud.VersionProtocolo,
                FechaProtocolo = origen.CartaSolicitud.FechaProtocolo,
                IdiomaProtocolo = origen.CartaSolicitud.IdiomaProtocolo,
                VersionConsentimiento = origen.CartaSolicitud.VersionConsentimiento,
                FechaConsentimiento = origen.CartaSolicitud.FechaConsentimiento,
                IdiomaConsentimiento = origen.CartaSolicitud.IdiomaConsentimiento,
                VersionInstrumentos = origen.CartaSolicitud.VersionInstrumentos,
                FechaInstrumentos = origen.CartaSolicitud.FechaInstrumentos,
                Nombre = origen.CartaSolicitud.Nombre
            },
            CartaIntencion = new CartaIntencionEstudio
            {
                TituloProtocolo = origen.CartaIntencion.TituloProtocolo,
                Nombre = origen.CartaIntencion.Nombre
            },
            Acuerdo = new AcuerdoConfidencialidadEstudio
            {
                Nombre = origen.Acuerdo.Nombre,
                Rol = origen.Acuerdo.Rol,
                Cedula = origen.Acuerdo.Cedula,
                Expedicion = origen.Acuerdo.Expedicion,
                Protocolo = origen.Acuerdo.Protocolo,
                NombreFirma = origen.Acuerdo.NombreFirma
            },
            Registro = new FormularioRegistroEstudio
            {
                Nombres = origen.Registro.Nombres,
                Apellidos = origen.Registro.Apellidos,
                Titulos = origen.Registro.Titulos,
                Correo = origen.Registro.Correo,
                Institucion = origen.Registro.Institucion,
                Direccion = origen.Registro.Direccion,
                Telefono = origen.Registro.Telefono,
                Celular = origen.Registro.Celular,
                Formacion = origen.Registro.Formacion,
                Servicio = origen.Registro.Servicio,
                Experiencia = origen.Registro.Experiencia,
                Publicaciones = origen.Registro.Publicaciones
            },
            HojaVida = new HojaVidaEstudio
            {
                Nombre = origen.HojaVida.Nombre,
                Titulos = origen.HojaVida.Titulos,
                Direccion = origen.HojaVida.Direccion,
                Telefono = origen.HojaVida.Telefono,
                Fax = origen.HojaVida.Fax,
                Academica = origen.HojaVida.Academica,
                Laboral = origen.HojaVida.Laboral,
                Investigacion = origen.HojaVida.Investigacion,
                Gcp = origen.HojaVida.Gcp
            },
            Resumen = new ResumenAnaliticoEstudio
            {
                Titulo = origen.Resumen.Titulo,
                Linea = origen.Resumen.Linea,
                Grupo = origen.Resumen.Grupo,
                Area = origen.Resumen.Area,
                Institucion1 = origen.Resumen.Institucion1,
                Institucion2 = origen.Resumen.Institucion2,
                Institucion3 = origen.Resumen.Institucion3,
                AnioResidencia = origen.Resumen.AnioResidencia,
                Problema = origen.Resumen.Problema,
                Justificacion = origen.Resumen.Justificacion,
                ObjetivoGeneral = origen.Resumen.ObjetivoGeneral,
                Objetivo1 = origen.Resumen.Objetivo1,
                Objetivo2 = origen.Resumen.Objetivo2,
                Objetivo3 = origen.Resumen.Objetivo3,
                EstadoArte = origen.Resumen.EstadoArte,
                TipoEstudio = origen.Resumen.TipoEstudio,
                Poblacion = origen.Resumen.Poblacion,
                Muestra = origen.Resumen.Muestra,
                Inclusion = origen.Resumen.Inclusion,
                Exclusion = origen.Resumen.Exclusion,
                Recoleccion = origen.Resumen.Recoleccion,
                Analisis = origen.Resumen.Analisis,
                Divulgacion = origen.Resumen.Divulgacion,
                FechaInicio = origen.Resumen.FechaInicio,
                Riesgo = origen.Resumen.Riesgo,
                Confidencialidad = origen.Resumen.Confidencialidad,
                Seguridad = origen.Resumen.Seguridad,
                Consentimiento = origen.Resumen.Consentimiento,
                Investigadores = origen.Resumen.Investigadores.Select(item => new PersonaRolEstudio { Nombre = item.Nombre, Rol = item.Rol }).ToList(),
                Presupuesto = origen.Resumen.Presupuesto.Select(item => new PartidaPresupuestoEstudio
                {
                    Item = item.Item,
                    Horas = item.Horas,
                    Valor = item.Valor,
                    Responsable = item.Responsable,
                    Observaciones = item.Observaciones
                }).ToList(),
                Cronograma = origen.Resumen.Cronograma.Select(item => new ActividadCronogramaEstudio
                {
                    Actividad = item.Actividad,
                    MesInicial = item.MesInicial,
                    MesFinal = item.MesFinal
                }).ToList()
            }
        };
    }

    private static byte[]? Decodificar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();
        var coma = texto.IndexOf(',');
        if (texto.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && coma >= 0)
        {
            texto = texto[(coma + 1)..];
        }

        try
        {
            return Convert.FromBase64String(texto);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static EstudioDto ADto(Estudio estudio) => new()
    {
        Id = estudio.Id,
        Conteo = estudio.Conteo,
        TipoRegistro = estudio.TipoRegistro,
        Token = estudio.Token,
        Estado = estudio.Estado,
        Anio = estudio.Anio,
        Titulo = estudio.Titulo,
        Especialidad = estudio.Especialidad,
        Tipo = estudio.Tipo,
        Acronimo = estudio.Acronimo,
        TipoInvestigacion = estudio.TipoInvestigacion,
        Institucion = estudio.Institucion,
        InvestigadorPrincipal = estudio.InvestigadorPrincipal,
        Correo = estudio.Correo,
        CoInvestigadores = estudio.CoInvestigadores,
        Coordinador = estudio.Coordinador,
        Patrocinador = estudio.Patrocinador,
        Paises = estudio.Paises,
        Objetivo = estudio.Objetivo,
        NumeroAprobacionCei = estudio.NumeroAprobacionCei,
        MensajeCorreccion = estudio.MensajeCorreccion,
        MensajeAval = estudio.MensajeAval,
        AvisoRecibidoEnviado = estudio.AvisoRecibidoEnviado,
        FechaCreacionUtc = estudio.FechaCreacionUtc,
        Formatos = AFormatos(estudio.Formatos),
        Archivos = estudio.Archivos
            .Where(item => !ClavesArchivoEstudio.EsFirma(item.Clave))
            .Select(item => new ArchivoEstudioDto
            {
                Id = item.Id,
                Clave = item.Clave,
                Nombre = item.Nombre,
                TipoContenido = item.TipoContenido
            })
            .ToList()
    };

    private static FormatosAcademicosDto AFormatos(FormatosAcademicos? formatos)
    {
        formatos ??= new FormatosAcademicos();
        return new FormatosAcademicosDto
        {
            CartaSolicitud = new CartaSolicitudEstudioDto
            {
                Protocolo = formatos.CartaSolicitud.Protocolo,
                VersionProtocolo = formatos.CartaSolicitud.VersionProtocolo,
                FechaProtocolo = formatos.CartaSolicitud.FechaProtocolo,
                IdiomaProtocolo = formatos.CartaSolicitud.IdiomaProtocolo,
                VersionConsentimiento = formatos.CartaSolicitud.VersionConsentimiento,
                FechaConsentimiento = formatos.CartaSolicitud.FechaConsentimiento,
                IdiomaConsentimiento = formatos.CartaSolicitud.IdiomaConsentimiento,
                VersionInstrumentos = formatos.CartaSolicitud.VersionInstrumentos,
                FechaInstrumentos = formatos.CartaSolicitud.FechaInstrumentos,
                Nombre = formatos.CartaSolicitud.Nombre
            },
            CartaIntencion = new CartaIntencionEstudioDto
            {
                TituloProtocolo = formatos.CartaIntencion.TituloProtocolo,
                Nombre = formatos.CartaIntencion.Nombre
            },
            Acuerdo = new AcuerdoConfidencialidadEstudioDto
            {
                Nombre = formatos.Acuerdo.Nombre,
                Rol = formatos.Acuerdo.Rol,
                Cedula = formatos.Acuerdo.Cedula,
                Expedicion = formatos.Acuerdo.Expedicion,
                Protocolo = formatos.Acuerdo.Protocolo,
                NombreFirma = formatos.Acuerdo.NombreFirma
            },
            Registro = new FormularioRegistroEstudioDto
            {
                Nombres = formatos.Registro.Nombres,
                Apellidos = formatos.Registro.Apellidos,
                Titulos = formatos.Registro.Titulos,
                Correo = formatos.Registro.Correo,
                Institucion = formatos.Registro.Institucion,
                Direccion = formatos.Registro.Direccion,
                Telefono = formatos.Registro.Telefono,
                Celular = formatos.Registro.Celular,
                Formacion = formatos.Registro.Formacion,
                Servicio = formatos.Registro.Servicio,
                Experiencia = formatos.Registro.Experiencia,
                Publicaciones = formatos.Registro.Publicaciones
            },
            HojaVida = new HojaVidaEstudioDto
            {
                Nombre = formatos.HojaVida.Nombre,
                Titulos = formatos.HojaVida.Titulos,
                Direccion = formatos.HojaVida.Direccion,
                Telefono = formatos.HojaVida.Telefono,
                Fax = formatos.HojaVida.Fax,
                Academica = formatos.HojaVida.Academica,
                Laboral = formatos.HojaVida.Laboral,
                Investigacion = formatos.HojaVida.Investigacion,
                Gcp = formatos.HojaVida.Gcp
            },
            Resumen = new ResumenAnaliticoEstudioDto
            {
                Titulo = formatos.Resumen.Titulo,
                Linea = formatos.Resumen.Linea,
                Grupo = formatos.Resumen.Grupo,
                Area = formatos.Resumen.Area,
                Institucion1 = formatos.Resumen.Institucion1,
                Institucion2 = formatos.Resumen.Institucion2,
                Institucion3 = formatos.Resumen.Institucion3,
                AnioResidencia = formatos.Resumen.AnioResidencia,
                Problema = formatos.Resumen.Problema,
                Justificacion = formatos.Resumen.Justificacion,
                ObjetivoGeneral = formatos.Resumen.ObjetivoGeneral,
                Objetivo1 = formatos.Resumen.Objetivo1,
                Objetivo2 = formatos.Resumen.Objetivo2,
                Objetivo3 = formatos.Resumen.Objetivo3,
                EstadoArte = formatos.Resumen.EstadoArte,
                TipoEstudio = formatos.Resumen.TipoEstudio,
                Poblacion = formatos.Resumen.Poblacion,
                Muestra = formatos.Resumen.Muestra,
                Inclusion = formatos.Resumen.Inclusion,
                Exclusion = formatos.Resumen.Exclusion,
                Recoleccion = formatos.Resumen.Recoleccion,
                Analisis = formatos.Resumen.Analisis,
                Divulgacion = formatos.Resumen.Divulgacion,
                FechaInicio = formatos.Resumen.FechaInicio,
                Riesgo = formatos.Resumen.Riesgo,
                Confidencialidad = formatos.Resumen.Confidencialidad,
                Seguridad = formatos.Resumen.Seguridad,
                Consentimiento = formatos.Resumen.Consentimiento,
                Investigadores = formatos.Resumen.Investigadores.Select(item => new PersonaRolEstudioDto { Nombre = item.Nombre, Rol = item.Rol }).ToList(),
                Presupuesto = formatos.Resumen.Presupuesto.Select(item => new PartidaPresupuestoEstudioDto
                {
                    Item = item.Item,
                    Horas = item.Horas,
                    Valor = item.Valor,
                    Responsable = item.Responsable,
                    Observaciones = item.Observaciones
                }).ToList(),
                Cronograma = formatos.Resumen.Cronograma.Select(item => new ActividadCronogramaEstudioDto
                {
                    Actividad = item.Actividad,
                    MesInicial = item.MesInicial,
                    MesFinal = item.MesFinal
                }).ToList()
            }
        };
    }
}
