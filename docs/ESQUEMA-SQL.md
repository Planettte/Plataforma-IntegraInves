# Esquema SQL propuesto

Tres esquemas, los mismos que ya nombra `Domain/Esquemas.cs`: `nucleo`, `estudios`, `udd` y `comite`. La clave de cada fila es `uniqueidentifier`. Las fechas de calendario que la aplicación guarda como `aaaa-mm-dd` van en `date`. Los textos largos van en `nvarchar(max)`. Lo que hoy es un diccionario de claves variables va en una tabla hija de clave y valor.

`nucleo` ya está en Entity Framework (`Data/CentroDbContext.cs`, migración `NucleoInicial`). El resto todavía no tiene tablas: este archivo es el plano para crearlas.

## nucleo

| Tabla | Campo | Tipo | Notas |
| --- | --- | --- | --- |
| Areas | Id | uniqueidentifier | PK |
| | Codigo | nvarchar(40) | único |
| | Nombre | nvarchar(120) | |
| Usuarios | Id | uniqueidentifier | PK |
| | IdentificadorProveedor | nvarchar(128) | único, sujeto de Google |
| | CorreoElectronico | nvarchar(256) | |
| | Nombre | nvarchar(200) | |
| | Nombres | nvarchar(120) | |
| | Apellidos | nvarchar(120) | |
| | NumeroContacto | nvarchar(40) | |
| | Extension | nvarchar(20) | |
| | Cargo | nvarchar(120) | |
| | TemaPreferido | nvarchar(40) | |
| | Estado | nvarchar(40) | Activo, Pendiente, Rechazado |
| | AreaInteresId | uniqueidentifier | FK Areas, nulo |
| | FechaCreacionUtc | datetimeoffset | |
| | UltimoAccesoUtc | datetimeoffset | nulo |
| UsuariosAreas | UsuarioId | uniqueidentifier | PK compuesta, FK Usuarios |
| | AreaId | uniqueidentifier | PK compuesta, FK Areas |
| Roles | Id | uniqueidentifier | PK |
| | Codigo | nvarchar(80) | único |
| | Nombre | nvarchar(120) | |
| Permisos | Id | uniqueidentifier | PK |
| | Codigo | nvarchar(80) | único |
| | Nombre | nvarchar(160) | |
| RolesPermisos | RolId | uniqueidentifier | PK compuesta, FK Roles |
| | PermisoId | uniqueidentifier | PK compuesta, FK Permisos |
| AsignacionesRol | Id | uniqueidentifier | PK |
| | UsuarioId | uniqueidentifier | FK Usuarios |
| | RolId | uniqueidentifier | FK Roles |
| | AreaId | uniqueidentifier | FK Areas, nulo |
| AsignacionesPermiso | UsuarioId | uniqueidentifier | PK compuesta, FK Usuarios |
| | PermisoId | uniqueidentifier | PK compuesta, FK Permisos |
| EventosAuditoria | Id | uniqueidentifier | PK |
| | FechaUtc | datetimeoffset | índice |
| | UsuarioId | uniqueidentifier | nulo |
| | Accion | nvarchar(80) | |
| | Entidad | nvarchar(80) | |
| | EntidadId | nvarchar(64) | |
| | Resultado | nvarchar(40) | |
| | ValoresAnteriores | nvarchar(max) | nulo |
| | ValoresNuevos | nvarchar(max) | nulo |
| | DireccionRed | nvarchar(64) | |
| | AgenteUsuario | nvarchar(256) | |
| Notas | Id | uniqueidentifier | PK |
| | UsuarioId | uniqueidentifier | FK Usuarios, índice |
| | Titulo | nvarchar(160) | |
| | Contenido | nvarchar(4000) | |
| | FechaCreacionUtc | datetimeoffset | |
| | FechaActualizacionUtc | datetimeoffset | nulo |
| Notificaciones | Id | uniqueidentifier | PK |
| | UsuarioId | uniqueidentifier | FK Usuarios, índice |
| | Titulo | nvarchar(200) | |
| | Mensaje | nvarchar(max) | |
| | Enlace | nvarchar(300) | nulo |
| | FechaUtc | datetimeoffset | |
| | VistaUtc | datetimeoffset | nulo |

## estudios

### HojasVida

| Tabla | Campo | Tipo |
| --- | --- | --- |
| HojasVida | Id | uniqueidentifier |
| | NombreCompleto | nvarchar(200) |
| | TituloAcademico | nvarchar(200) |
| | DirInstitucion | nvarchar(200) |
| | DirCompleta | nvarchar(300) |
| | DirCiudad | nvarchar(120) |
| | DirEstado | nvarchar(120) |
| | DirProvincia | nvarchar(120) |
| | DirPais | nvarchar(120) |
| | DirCodigo | nvarchar(40) |
| | Telefono | nvarchar(40) |
| | Extension | nvarchar(20) |
| | VinculacionModalidad | nvarchar(80) |
| | VinculacionFechaInicio | date | nulo si viene vacío |
| | GcpSiNo | nvarchar(10) |
| | GcpDetalle | nvarchar(max) |
| | OtrosSiNo | nvarchar(10) |
| | OtrosDetalle | nvarchar(max) |
| | FirmaRuta | nvarchar(300) | clave en el bucket |
| | FirmaNombre | nvarchar(200) |
| | FirmaTipo | nvarchar(80) |
| | FirmaFecha | date | nulo |
| | Version | int |
| | FechaEdicionUtc | datetimeoffset |
| HojasVidaFormacion | Id | uniqueidentifier |
| | HojaVidaId | uniqueidentifier | FK |
| | Orden | int |
| | Institucion | nvarchar(200) |
| | Titulo | nvarchar(200) |
| | Anio | nvarchar(20) |
| | Area | nvarchar(120) |
| HojasVidaLaboral | Id | uniqueidentifier |
| | HojaVidaId | uniqueidentifier | FK |
| | Orden | int |
| | Posicion | nvarchar(200) |
| | Institucion | nvarchar(200) |
| | Locacion | nvarchar(200) |
| | FechaInicio | nvarchar(40) |
| | FechaFin | nvarchar(40) |
| HojasVidaInvestigacion | Id | uniqueidentifier |
| | HojaVidaId | uniqueidentifier | FK |
| | Orden | int |
| | Codigo | nvarchar(80) |
| | Indicacion | nvarchar(200) |
| | Fase | nvarchar(80) |
| | Papel | nvarchar(120) |
| | Anio | nvarchar(20) |
| HojasVidaSoportes | Id | uniqueidentifier |
| | HojaVidaId | uniqueidentifier | FK |
| | Nombre | nvarchar(200) |
| | Ruta | nvarchar(300) | clave en el bucket |
| | Tipo | nvarchar(80) |
| | Orden | int |
| HojasVidaHistorial | Id | uniqueidentifier |
| | HojaVidaId | uniqueidentifier | FK |
| | Version | int |
| | FechaUtc | datetimeoffset |
| | Usuario | nvarchar(200) |
| | Nota | nvarchar(500) |

### Farmacia

El formato y el inventario no se relacionan entre sí.

| Tabla | Campo | Tipo |
| --- | --- | --- |
| FarmaciaFormatos | Id | uniqueidentifier |
| | NumeroEstudio | nvarchar(80) |
| | Patrocinador | nvarchar(200) |
| | Coordinador | nvarchar(200) |
| | NumeroCentro | nvarchar(80) |
| | Titulo | nvarchar(300) |
| | InvestigadorPrincipal | nvarchar(200) |
| | CondicionesAlmacenamiento | nvarchar(max) |
| | DescripcionMedicamento | nvarchar(max) |
| | Dosis | nvarchar(200) |
| | Observaciones | nvarchar(max) |
| | ResponsableFarmacia | nvarchar(200) |
| | FirmaCierre | nvarchar(max) | imagen en base64, o luego una clave de bucket |
| | CantidadFinal | nvarchar(40) |
| | GeneradoEl | datetimeoffset | nulo |
| | Activo | bit |
| | EliminadoEl | datetimeoffset | nulo |
| | EliminadoPorId | uniqueidentifier | nulo |
| | EliminadoPorNombre | nvarchar(200) |
| FarmaciaInventario | Id | uniqueidentifier |
| | NumeroEnvio | nvarchar(40) |
| | FechaRecibido | date | nulo |
| | Lote | nvarchar(80) |
| | FechaVencimiento | date | nulo |
| | CodigoMedicamento | nvarchar(80) |
| | Conforme | nvarchar(20) |
| | SujetoCodigo | nvarchar(80) |
| | FechaEntrega | date | nulo |
| | Entrega | nvarchar(120) |
| | Recibe | nvarchar(120) |
| | FechaRetorno | date | nulo |
| | Empaque | nvarchar(80) |
| | Unidades | nvarchar(40) |
| | EntregaRetorno | nvarchar(120) |
| | RecibeRetorno | nvarchar(120) |
| | ControlMonitoria | nvarchar(120) |
| | FechaDisposicion | date | nulo |
| | RetornoPatrocinador | nvarchar(80) |
| | DestruccionActa | nvarchar(80) |

### Dispositivos

| Tabla | Campo | Tipo |
| --- | --- | --- |
| Dispositivos | Id | uniqueidentifier |
| | Dm | nvarchar(40) |
| | Marca | nvarchar(120) |
| | Modelo | nvarchar(120) |
| | Riesgo | nvarchar(40) |
| | Serie | nvarchar(80) |
| | CodigoActivo | nvarchar(80) |
| | InventarioBiomedica | nvarchar(80) |
| | RegistroInvima | nvarchar(80) |
| | PeriodicidadMantenimiento | nvarchar(40) |
| | UltimoMantenimiento | date | nulo |
| | MesCalificacion | nvarchar(40) |
| | MesVerificacionMetrologica | nvarchar(40) |
| | PeriodicidadCalibracion | nvarchar(40) |
| | UltimaCalibracion | date | nulo |
| | Ubicacion | nvarchar(200) |
| | Area | nvarchar(120) |
| | Observaciones | nvarchar(max) |
| | CalificacionVentana1 | nvarchar(40) |
| | CalificacionVentana2 | nvarchar(40) |
| DispositivosAviso | Id | uniqueidentifier | una sola fila de configuración |
| | CorreoEncargado | nvarchar(256) |
| | UltimoCorreoUtc | datetimeoffset | nulo |
| DispositivosAvisosEnviados | Id | uniqueidentifier |
| | Clave | nvarchar(200) | el texto que hoy evita repetir el mismo aviso |

### Plantillas

| Tabla | Campo | Tipo |
| --- | --- | --- |
| PlantillaInvestigadores | Id | uniqueidentifier |
| | Nombre | nvarchar(200) |
| | Titulo | nvarchar(200) |
| PlantillaCoordinadores | Id | uniqueidentifier |
| | Nombre | nvarchar(200) |
| | Correo | nvarchar(256) |
| PlantillaEstudios | Id | uniqueidentifier |
| | Nombre | nvarchar(200) |
| | Codigo | nvarchar(80) |
| | Patrocinador | nvarchar(200) |
| | InvestigadorPrincipalId | uniqueidentifier | nulo, FK PlantillaInvestigadores |
| | SubinvestigadorId | uniqueidentifier | nulo |
| | CoordinadorPrincipalId | uniqueidentifier | nulo, FK PlantillaCoordinadores |
| | CoordinadorApoyoId | uniqueidentifier | nulo |
| | FechaCreacionUtc | datetimeoffset |
| | FechaEdicionUtc | datetimeoffset |
| PlantillaRevisadas | EstudioId | uniqueidentifier | PK compuesta |
| | Codigo | nvarchar(80) | PK compuesta |

### Agenda

Misma tabla para estudios y UDD. `Agenda` distingue `medicos`, `coordinacion` y `diseno`.

| Tabla | Campo | Tipo |
| --- | --- | --- |
| AgendaCitas | Id | uniqueidentifier |
| | Agenda | nvarchar(40) |
| | Fecha | date |
| | Persona | nvarchar(200) |
| | UsuarioId | uniqueidentifier | nulo, FK nucleo.Usuarios |
| | HoraInicio | nvarchar(5) |
| | HoraFin | nvarchar(5) |
| | Tipo | nvarchar(20) | citas u otros |
| | Importante | bit |
| | CreadoPorPersona | nvarchar(200) |
| | CreadoPorCorreo | nvarchar(256) |
| AgendaCitaCampos | CitaId | uniqueidentifier | PK compuesta, FK |
| | Clave | nvarchar(40) | PK compuesta |
| | Valor | nvarchar(500) |
| AgendaMonitorias | Id | uniqueidentifier |
| | Fecha | date |
| | Descripcion | nvarchar(300) |
| | CreadoPorPersona | nvarchar(200) |
| AgendaFestivos | Fecha | date | PK |

Retención documental no tiene tabla.

## udd

### Proyectos

| Tabla | Campo | Tipo |
| --- | --- | --- |
| Proyectos | Id | uniqueidentifier |
| | Nombre | nvarchar(200) |
| | Estado | nvarchar(40) | se crea en Activo |
| | CreadoEl | datetimeoffset |
| | ResponsableId | uniqueidentifier | FK nucleo.Usuarios |
| | Semillero | nvarchar(200) |
| ProyectoFases | Id | uniqueidentifier |
| | ProyectoId | uniqueidentifier | FK |
| | Nombre | nvarchar(200) |
| | Orden | int |
| | Peso | int |
| ProyectoActividades | Id | uniqueidentifier |
| | FaseId | uniqueidentifier | FK |
| | Descripcion | nvarchar(500) |
| | FechaInicio | date |
| | FechaLimite | date |
| | Peso | int |
| | Estado | nvarchar(40) | En progreso, Completado, Pausado |
| ProyectoInvestigadores | Id | uniqueidentifier |
| | Nombre | nvarchar(200) | catálogo de la herramienta, no es una cuenta |
| ProyectoAsignaciones | Id | uniqueidentifier |
| | ActividadId | uniqueidentifier | FK |
| | InvestigadorId | uniqueidentifier | FK |
| | Peso | int |
| | Estado | nvarchar(40) |
| ProyectoProrrogas | Id | uniqueidentifier |
| | ActividadId | uniqueidentifier | FK |
| | InvestigadorId | uniqueidentifier | nulo |
| | FechaAnterior | date |

Los porcentajes no se guardan. Se calculan al leer.

### Protocolos

| Tabla | Campo | Tipo |
| --- | --- | --- |
| Protocolos | Id | uniqueidentifier |
| | Token | nvarchar(64) | único |
| | Estado | nvarchar(40) | En revisión, Corrección, Aprobado |
| | Formato | nvarchar(20) | UIS, FOSCAL, UDES |
| | Nombre | nvarchar(200) |
| | Correo | nvarchar(256) |
| | RevisorId | uniqueidentifier | FK nucleo.Usuarios |
| | CreadoEl | datetimeoffset |
| | EditadoEl | datetimeoffset |
| ProtocoloRespuestas | ProtocoloId | uniqueidentifier | PK compuesta |
| | Clave | nvarchar(80) | PK compuesta |
| | Valor | nvarchar(max) |
| ProtocoloComentarios | ProtocoloId | uniqueidentifier | PK compuesta |
| | Clave | nvarchar(80) | PK compuesta |
| | Valor | nvarchar(4000) |
| ProtocoloConocimiento | Id | uniqueidentifier |
| | ProtocoloId | uniqueidentifier | FK |
| | Orden | int |
| | Resultado | nvarchar(500) |
| | Indicador | nvarchar(500) |
| | Beneficiario | nvarchar(500) |
| ProtocoloProductos | Id | uniqueidentifier |
| | ProtocoloId | uniqueidentifier | FK |
| | Tipo | nvarchar(20) | tecnologico o capacidad |
| | Orden | int |
| | Objetivo | nvarchar(500) |
| | Resultado | nvarchar(500) |
| | Indicador | nvarchar(500) |
| | Beneficiario | nvarchar(500) |
| ProtocoloCronograma | Id | uniqueidentifier |
| | ProtocoloId | uniqueidentifier | FK |
| | Origen | nvarchar(20) | foscal o udes |
| | Orden | int |
| | Actividad | nvarchar(500) |
| | Responsable | nvarchar(200) |
| | Meses | nvarchar(40) | doce bits, por ejemplo `010010000000` |
| ProtocoloPresupuesto | Id | uniqueidentifier |
| | ProtocoloId | uniqueidentifier | FK |
| | Orden | int |
| | Rubro | nvarchar(200) |
| | Valores | nvarchar(400) | ocho importes separados, el texto tal como se escribe |
| ProtocoloEtica | ProtocoloId | uniqueidentifier | PK compuesta |
| | Grupo | nvarchar(20) | poblacion, intervencion, instrumentos |
| | Orden | int | PK compuesta |
| | Marcado | bit |

## comite

| Tabla | Campo | Tipo |
| --- | --- | --- |
| Estudios | Id | uniqueidentifier |
| | Conteo | int |
| | TipoRegistro | nvarchar(20) | academico o clinico |
| | Token | nvarchar(64) | único |
| | Estado | nvarchar(160) |
| | Anio | int |
| | Titulo | nvarchar(300) |
| | Especialidad | nvarchar(120) |
| | Tipo | nvarchar(80) |
| | Acronimo | nvarchar(80) |
| | TipoInvestigacion | nvarchar(120) |
| | Institucion | nvarchar(200) |
| | InvestigadorPrincipal | nvarchar(200) |
| | Correo | nvarchar(256) |
| | CoInvestigadores | nvarchar(max) |
| | Coordinador | nvarchar(200) |
| | Patrocinador | nvarchar(200) |
| | Paises | nvarchar(300) |
| | Objetivo | nvarchar(max) |
| | NumeroAprobacionCei | nvarchar(80) |
| | MensajeCorreccion | nvarchar(max) |
| | MensajeAval | nvarchar(max) |
| | AvisoRecibidoEnviado | bit | nulo |
| | FechaCreacionUtc | datetimeoffset |
| | FechaEdicionUtc | datetimeoffset |
| EstudioArchivos | Id | uniqueidentifier |
| | EstudioId | uniqueidentifier | FK |
| | Clave | nvarchar(40) |
| | Nombre | nvarchar(200) |
| | TipoContenido | nvarchar(80) |
| | Ruta | nvarchar(300) | clave en el bucket |
| | FechaUtc | datetimeoffset |
| EstudioCartaSolicitud | EstudioId | uniqueidentifier | PK, FK |
| | Protocolo | nvarchar(200) |
| | VersionProtocolo | nvarchar(40) |
| | FechaProtocolo | date | nulo |
| | IdiomaProtocolo | nvarchar(40) |
| | VersionConsentimiento | nvarchar(40) |
| | FechaConsentimiento | date | nulo |
| | IdiomaConsentimiento | nvarchar(40) |
| | VersionInstrumentos | nvarchar(40) |
| | FechaInstrumentos | date | nulo |
| | Nombre | nvarchar(200) |
| EstudioCartaIntencion | EstudioId | uniqueidentifier | PK, FK |
| | TituloProtocolo | nvarchar(300) |
| | Nombre | nvarchar(200) |
| EstudioAcuerdo | EstudioId | uniqueidentifier | PK, FK |
| | Nombre | nvarchar(200) |
| | Rol | nvarchar(120) |
| | Cedula | nvarchar(40) |
| | Expedicion | nvarchar(120) |
| | Protocolo | nvarchar(200) |
| | NombreFirma | nvarchar(200) |
| EstudioRegistro | EstudioId | uniqueidentifier | PK, FK |
| | Nombres | nvarchar(120) |
| | Apellidos | nvarchar(120) |
| | Titulos | nvarchar(200) |
| | Correo | nvarchar(256) |
| | Institucion | nvarchar(200) |
| | Direccion | nvarchar(300) |
| | Telefono | nvarchar(40) |
| | Celular | nvarchar(40) |
| | Formacion | nvarchar(max) |
| | Servicio | nvarchar(max) |
| | Experiencia | nvarchar(max) |
| | Publicaciones | nvarchar(max) |
| EstudioHojaVida | EstudioId | uniqueidentifier | PK, FK |
| | Nombre | nvarchar(200) |
| | Titulos | nvarchar(300) |
| | Direccion | nvarchar(300) |
| | Telefono | nvarchar(40) |
| | Fax | nvarchar(40) |
| | Academica | nvarchar(max) |
| | Laboral | nvarchar(max) |
| | Investigacion | nvarchar(max) |
| | Gcp | nvarchar(max) |
| EstudioResumen | EstudioId | uniqueidentifier | PK, FK |
| | Titulo | nvarchar(300) |
| | Linea | nvarchar(200) |
| | Grupo | nvarchar(200) |
| | Area | nvarchar(120) |
| | Institucion1 | nvarchar(200) |
| | Institucion2 | nvarchar(200) |
| | Institucion3 | nvarchar(200) |
| | AnioResidencia | nvarchar(20) |
| | Problema | nvarchar(max) |
| | Justificacion | nvarchar(max) |
| | ObjetivoGeneral | nvarchar(max) |
| | Objetivo1 | nvarchar(max) |
| | Objetivo2 | nvarchar(max) |
| | Objetivo3 | nvarchar(max) |
| | EstadoArte | nvarchar(max) |
| | TipoEstudio | nvarchar(120) |
| | Poblacion | nvarchar(max) |
| | Muestra | nvarchar(max) |
| | Inclusion | nvarchar(max) |
| | Exclusion | nvarchar(max) |
| | Recoleccion | nvarchar(max) |
| | Analisis | nvarchar(max) |
| | Divulgacion | nvarchar(max) |
| | FechaInicio | date | nulo |
| | Riesgo | nvarchar(max) |
| | Confidencialidad | nvarchar(max) |
| | Seguridad | nvarchar(max) |
| | Consentimiento | nvarchar(max) |
| EstudioResumenPersonas | Id | uniqueidentifier |
| | EstudioId | uniqueidentifier | FK |
| | Orden | int |
| | Nombre | nvarchar(200) |
| | Rol | nvarchar(120) |
| EstudioPresupuesto | Id | uniqueidentifier |
| | EstudioId | uniqueidentifier | FK |
| | Orden | int |
| | Item | nvarchar(200) |
| | Horas | nvarchar(40) |
| | Valor | nvarchar(40) |
| | Responsable | nvarchar(200) |
| | Observaciones | nvarchar(300) |
| EstudioCronograma | Id | uniqueidentifier |
| | EstudioId | uniqueidentifier | FK |
| | Orden | int |
| | Actividad | nvarchar(300) |
| | MesInicial | nvarchar(20) |
| | MesFinal | nvarchar(20) |

Las firmas dibujadas del estudio se guardan como filas de `EstudioArchivos` con clave `firma-...` y la ruta del bucket. No hace falta una columna de imagen en la tabla del estudio.
