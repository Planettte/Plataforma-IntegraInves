# Paso a SQL Server y al bucket de Google

Hoy, en desarrollo, la API arranca, prueba la cadena `ConnectionStrings:Centro` y, si SQL no responde, usa `datos-locales/*.json`. Fuera de desarrollo, si SQL no responde, la API no inicia. Los archivos de usuario van a carpetas locales detrás de `IDepositoArchivos` e `IDepositoEstudios`. Este archivo dice qué hay que pedir y en qué orden enlazarlos. No hace falta reescribir las pantallas.

## Qué pedir para SQL Server

1. Una instancia de SQL Server accesible desde el servidor donde va a correr la API.
2. Una base llamada `CentroInvestigaciones`.
3. Un inicio de sesión de la API con permiso para crear esquemas y tablas (`db_owner` en esa base durante la migración; después se puede bajar a lectura y escritura de datos).
4. La cadena de conexión, con este nombre exacto en la configuración de la API:

```text
ConnectionStrings:Centro
```

Ejemplo de forma, sin credenciales reales:

```text
Server=SERVIDOR;Database=CentroInvestigaciones;User Id=USUARIO;Password=CLAVE;TrustServerCertificate=True
```

En desarrollo puede seguir siendo `(localdb)\mssqllocaldb`. En el servidor institucional va en variables de entorno o en el almacén de secretos, no en git.

5. Confirmar que el puerto de SQL (normalmente 1433) está abierto solo hacia el servidor de la API.

## Qué pasa al conectar SQL

`API/Program.cs` ya hace esto cuando la sonda responde:

- Registra Entity Framework (`Data/DependenciasDatos.cs`).
- Aplica la migración `NucleoInicial`.
- Siembra áreas, roles y permisos con `SemillaNucleo`.
- Usa `AlmacenNucleo` en lugar de `nucleo.json`.

Eso cubre cuentas, roles, permisos, auditoría y notas. Las herramientas (hojas de vida, farmacia, agenda, dispositivos, plantillas, estudios, proyectos y protocolos) siguen en JSON hasta que exista su migración. El esquema propuesto está en `docs/ESQUEMA-SQL.md`.

Las cuentas que hoy están solo en `nucleo.json` no pasan solas. Hay que volver a entrar con Google o copiarlas a propósito en un paso aparte.

## Qué pedir para el bucket

1. Un proyecto de Google Cloud de la institución.
2. Un bucket, por ejemplo `centro-investigaciones-archivos`, en la región que defina sistemas.
3. Una cuenta de servicio con permiso de crear, leer y borrar objetos en ese bucket. El JSON de la cuenta se guarda como secreto del servidor, igual que el secreto de Google OAuth. No va al repositorio.
4. El nombre del bucket y el correo de la cuenta de servicio.

Los objetos conviene guardarlos con una clave opaca (`estudios/{id}/{archivo}`, `hojas-de-vida/{id}/soportes/{archivo}`), no con la ruta del disco. El cliente sigue pidiendo el archivo a la API; la API lo lee del bucket.

Quedan en el programa, no en el bucket: el logo, `formato-plantilla.pdf` y `como-escribir-un-protocolo.pdf`. Esos están en `API/Recursos`.

## Orden recomendado

1. Recibir instancia, base, usuario y cadena `ConnectionStrings:Centro`.
2. Arrancar la API una vez contra esa base para aplicar `NucleoInicial` y comprobar el ingreso con Google.
3. Definir la URI de producción `https://HOST/api/sesion/google-respuesta` en Google Cloud, junto al cliente OAuth que ya se usa.
4. Pedir el bucket y la cuenta de servicio.
5. Agregar las migraciones de `docs/ESQUEMA-SQL.md` para los esquemas `estudios`, `udd` y `comite`.
6. Cambiar en `Program.cs` cada `Almacen*Local` por un almacén SQL, y `DepositoCarpetaLocal` / `DepositoEstudiosLocal` por un depósito del bucket. Las pantallas no cambian porque dependen de las interfaces.
7. Copiar los JSON que sí sean datos reales (agenda, dispositivos y las cuentas) en un paso controlado. No copiar registros de prueba.

## Qué no pedir todavía

- Un JWT para el navegador. La sesión sigue siendo la cookie.
- Acceso del cliente Blazor al bucket. El navegador no debe tener la clave.
- Mezclar los JSON de herramientas dentro de `nucleo.json`.
