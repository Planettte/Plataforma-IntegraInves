# Guía de la plataforma

Plataforma Integral del Centro de Investigaciones (FOSCAL). Una sola institución. Las herramientas reemplazan el uso anterior de Google Apps Script. El código no es una traducción de esos scripts.

El diseño visual vive en un solo archivo: `CentroInvestigaciones.Client/wwwroot/css/plataforma.css`. Los componentes compartidos (`Boton`, `Modal`, `Alerta`, `EncabezadoPagina`, barras de herramienta) están en `CentroInvestigaciones.Client/Components`.

## Cómo está armado el código

| Proyecto | Para qué sirve |
| --- | --- |
| `CentroInvestigaciones.Client` | Pantallas Blazor WebAssembly. Solo llama a la API. No decide permisos. |
| `CentroInvestigaciones.API` | HTTP, cookie de sesión, Google, correo y los adaptadores de hoy (archivo local). |
| `CentroInvestigaciones.Application` | Casos de uso e interfaces de almacén y depósito. |
| `CentroInvestigaciones.Domain` | Entidades, estados y permisos. Sin ASP.NET ni base de datos. |
| `CentroInvestigaciones.Data` | Entity Framework. Hoy solo el esquema `nucleo` y la migración `NucleoInicial`. |
| `CentroInvestigaciones.Shared` | Contratos que comparten la API y el cliente. |

El cliente se publica desde la misma API. En desarrollo: `https://localhost:7029` con `dotnet run --launch-profile https` desde `CentroInvestigaciones.API`.

La sesión es una cookie HttpOnly (`CentroInvestigaciones.Sesion`), no un JWT en el navegador. Google identifica a la persona; la cuenta de la plataforma (estado, área, cargo, permisos) es aparte. Un ingreso de Google puede quedar pendiente de aprobación.

Cada escritura a `/api` (POST, PUT, PATCH, DELETE) exige el encabezado `X-Centro-Solicitud: 1`. El cliente lo manda en `ClienteSesion.EncabezadoSolicitud`. Las lecturas GET no lo necesitan.

Los permisos usan el código `area.modulo.accion`. La API los exige con `RequierePermiso`. La pantalla no es la barrera. El catálogo de herramientas está en `Domain/CatalogoHerramientas.cs` y la navegación del inicio en `Client/Navigation/CatalogoNavegacion.cs`.

## Plataforma general

| Qué | Dónde |
| --- | --- |
| Arranque, cookie, Google y adaptadores | `API/Program.cs` |
| Ingreso, perfil, tema y cierre | `API/Controllers/SesionController.cs`, `Client/Pages/Login.razor`, `Perfil.razor`, `PrimerIngreso.razor` |
| Inicio y áreas | `Client/Pages/Inicio.razor`, `EstudiosClinicos/Indice.razor`, `Udd/Indice.razor`, `Comite/Indice.razor` |
| Usuarios, roles y permisos | `API/Controllers/UsuariosController.cs`, `RolesController.cs`, `PermisosController.cs`, `AreasController.cs` |
| Auditoría y notas | `API/Controllers/AuditoriaController.cs`, `NotasController.cs` |
| Avisos de la campana | `API/Controllers/NotificacionesController.cs`, `API/Infraestructura/AlmacenNotificaciones.cs` |
| Diseño | `Client/wwwroot/css/plataforma.css` |
| Distribución de la pantalla | `Client/Layout/MainLayout.razor` |

Funciones del núcleo: ingresar con Google, completar perfil, aprobar o rechazar cuentas, asignar área, cargo y herramientas, consultar auditoría. Mientras SQL no responde, eso se guarda en `API/datos-locales/nucleo.json`.

## Cómo ejecutarla

El SDK puede estar solo en la carpeta de usuario. En PowerShell:

```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;" + $env:PATH
cd CentroInvestigaciones.API
dotnet run --launch-profile https
```

Si el navegador muestra que la conexión no es privada:

```powershell
dotnet dev-certs https --trust
```

Google y los secretos están en user-secrets del proyecto API, no en el código. La URI de redirección de desarrollo es `https://localhost:7029/api/sesion/google-respuesta`.

Si SQL Server no responde, la API no se cae: usa los JSON de `datos-locales`. Esa carpeta no se sube a git. El administrador inicial sale de `Autenticacion:AdministradorInicial`; si esa clave está vacía, se usa el respaldo de `API/Infraestructura/DesarrolloLocal.cs`. En el primer ingreso hay que completar nombres, apellidos, contacto, extensión, cargo y área.

## Herramientas

Retención documental no guarda datos: se llena y se descarga el PDF. El resto persiste en su propio JSON, separado de `nucleo.json`.

### Hojas de vida

| | |
| --- | --- |
| Ruta | `/estudios-clinicos/hojas-de-vida` |
| Permiso | `herramientas.estudios.hojas-de-vida` |
| Pantallas | `Pages/EstudiosClinicos/HojasDeVida/Personas.razor`, `Editor.razor`, `Soportes.razor`, `Historial.razor` |
| API | `HojasVidaController` |
| Datos | `datos-locales/hojas-de-vida.json` y archivos en `hojas-de-vida-archivos/` |
| PDF | `GeneradorPdfHojaVida` |

Se crea la persona, se edita la hoja, se adjuntan soportes (PDF o imagen) y se descarga el PDF. El PDF no se guarda. Cada guardado deja una nota en el historial.

### Plantillas de estudios

| | |
| --- | --- |
| Ruta | `/estudios-clinicos/plantillas` |
| Permiso | `herramientas.estudios.plantillas` |
| Pantallas | `Pages/EstudiosClinicos/Plantillas/Lista.razor`, `Checklist.razor`, `Carta.razor` |
| API | `PlantillasController` |
| Datos | `datos-locales/plantillas-estudios.json` |
| Firmas | `API/Recursos/firmas` |
| PDF | `GeneradorPdfCarta` sobre `Recursos/formato-plantilla.pdf` |

Catálogo de investigadores y coordinadores, estudios con plantillas revisadas, y cartas que se generan o se envían por correo. La firma se busca por el nombre del investigador.

### Inventario farmacia

| | |
| --- | --- |
| Ruta | `/estudios-clinicos/inventario-farmacia` |
| Permiso | `herramientas.estudios.inventario-farmacia` |
| Pantallas | `Pages/EstudiosClinicos/Farmacia/Lista.razor`, `Hoja.razor`, `Inventario.razor` |
| Barra | `Components/BarraFarmacia.razor` |
| API | `FarmaciaController` |
| Datos | `datos-locales/farmacia.json` |
| PDF | `GeneradorPdfFarmacia` |

Dos listas distintas. El formato del medicamento se cierra al primer guardado. El inventario sigue editable y se puede borrar. El borrado del formato es lógico.

### Agenda

| | |
| --- | --- |
| Rutas | `/estudios-clinicos/agenda`, `/udd/agenda`, `/agenda/general` |
| Permisos | `herramientas.estudios.agenda`, `herramientas.udd.agenda` |
| Pantallas | `Pages/Agenda/Calendario.razor`, `General.razor` |
| API | `AgendaController` |
| Datos | `datos-locales/agenda.json` |

Citas por médico, monitorías y festivos. La agenda general se consulta sin modificar. Quien tiene el permiso puede compartir el enlace; esa consulta pública muestra el título de la cita (estudio y paciente) y no el teléfono ni las observaciones.

### Retención documental

| | |
| --- | --- |
| Ruta | `/estudios-clinicos/retencion-documental` |
| Permiso | `herramientas.estudios.retencion-documental` |
| Pantalla | `Pages/EstudiosClinicos/Retencion.razor` |
| API | `RetencionController` (`POST api/retencion/pdf`) |
| PDF | `GeneradorPdfRetencion` |

Formulario de un solo uso. No hay lista ni archivo JSON.

### Dispositivos médicos

| | |
| --- | --- |
| Ruta | `/estudios-clinicos/inventario-dispositivos` |
| Permiso | `herramientas.estudios.dispositivos` |
| Pantalla | `Pages/EstudiosClinicos/Dispositivos/Inventario.razor` |
| API | `DispositivosController` |
| Datos | `datos-locales/dispositivos.json` |
| Avisos | `AvisadorDispositivos`, `VigilanciaDispositivos` |

Inventario con semáforo de mantenimiento, calibración y verificación. El correo del encargado se guarda en el mismo JSON.

### Estudios académicos y clínicos

| | |
| --- | --- |
| Rutas | `/comite/estudios`, `/comite/estudios/{id}`, `/comite/estudios/externo` y `/comite/estudios/externo/{token}` |
| Permiso interno | `herramientas.comite.estudios` |
| Pantallas | `Pages/Comite/Estudios.razor`, `EstudioDetalle.razor`, `ExternoEstudio.razor`, `FormatosAcademicos.razor` |
| API | `EstudiosController`, `EstudiosPublicosController` |
| Datos | `datos-locales/estudios.json` y archivos en `estudios-archivos/` |
| PDF | `GeneradorPdfEstudio` |
| Correo | `ServicioFlujoEstudios`, `EnviadorCorreoSmtp` |

El externo llena la fase 1 con un enlace de token. El comité corrige o da el aval. Luego el externo cierra la fase 2. Estados: recepción incompleta, aprobado por el comité, fase 2 en revisión, aprobado por el CEI.

### Formulario de protocolos

| | |
| --- | --- |
| Rutas | `/udd/protocolos`, `/udd/protocolos/{id}`, `/udd/protocolos/externo` y `/udd/protocolos/externo/{token}` |
| Permiso | `herramientas.udd.protocolos` |
| Pantallas | `Pages/Udd/Protocolos.razor`, `ProtocoloRevision.razor`, `ExternoProtocolo.razor` |
| Formulario | `Components/FormularioProtocolo.razor` |
| Barra | `Components/BarraProtocolos.razor` |
| API | `ProtocolosController`, `ProtocolosPublicosController` |
| Datos | `datos-locales/protocolos.json` |
| PDF | `GeneradorPdfProtocolos` |
| Instrucciones Foscal | `Recursos/como-escribir-un-protocolo.pdf` |

Formatos UIS, Foscal y UDES. El médico de apoyo es una cuenta activa con el permiso, área UDD y cargo UDD. Estados: En revisión, Corrección, Aprobado. El comentario es por campo. El botón Ver instrucciones descarga el PDF sin cambiar de página.

### Seguimiento de proyectos

| | |
| --- | --- |
| Ruta | `/udd/proyectos` |
| Permiso | `herramientas.udd.proyectos` |
| Pantalla | `Pages/Udd/Proyectos.razor` |
| API | `ProyectosController` |
| Datos | `datos-locales/proyectos.json` |
| PDF | `GeneradorPdfProyectos` |

El responsable es una cuenta con el permiso. Fases (pesos que suman 100), actividades, investigadores del catálogo de esta herramienta, asignaciones y prórrogas. Los porcentajes se calculan al leer: solo cuenta lo que está Completado.

## Archivos

El destino previsto de los archivos de usuario es un bucket de Google. Hoy el adaptador es una carpeta local. El cliente y `Domain` no conocen la ruta. Las interfaces están en `Application`: `IDepositoArchivos` (hojas de vida) e `IDepositoEstudios` (estudios). Logos, la plantilla de cartas y el PDF de instrucciones siguen en `API/Recursos` porque son parte de la aplicación, no documentos de un caso.

## Relación con el esquema inicial

El dibujo de Blazor WASM, API ASP.NET Core, Google y SQL Server con Entity Framework es el de esta solución. La sesión quedó en cookie del servidor, no en un JWT guardado por el cliente. SQL y el bucket están preparados en el código y todavía no son el almacén en uso. El recuadro de egresos de pacientes, SAP y LINCO no corresponde a esta plataforma.
