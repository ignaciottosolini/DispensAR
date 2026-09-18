# DispensAR

.NET 10 + EF Core + SQL Server + React/TypeScript. Login con ASP.NET Core Identity:
cada cuenta tiene exactamente un tenant y un email único en toda la plataforma.

## Arranque y primera cuenta

Con Docker Desktop abierto, detené la API antes de compilar o migrar. Desde C:\DispensAR:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-db.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\create-account.ps1
```

El último comando pregunta tenant (`1` para Demo o `2` para ACME), email y contraseña oculta
con confirmación. Requiere 12 caracteres, mayúscula, minúscula, número y símbolo.
No reemplaza cuentas existentes. No hay credenciales predeterminadas ni registro
público. Repetí el comando con OTRO email para crear una cuenta del otro tenant.
No uses la contraseña de SQL Server como contraseña de usuario.

Después ejecutá **Terminal → Ejecutar tarea → Iniciar entorno** y abrí
http://localhost:5173. Ingresá con la cuenta recién creada.

También podés iniciar API y React en dos terminales:

```powershell
.\.dotnet\dotnet.exe watch --project backend/DispensAR.Api
npm.cmd --prefix frontend run dev
```

Si la migración falla por conexión, revisar `docker compose ps` y
`Test-NetConnection 127.0.0.1 -Port 1433`. La API utiliza `.env` solo en Development;
`ConnectionStrings__DispensAR` permite reemplazar la conexión. No publiques secretos.
La contraseña de `.env` corresponde al servidor SQL, no al login de la app.

## Cuentas y tenants

- Organizations: tenants existentes, con Id int y Descripcion. Los nuevos numeros se generan con la secuencia TenantNumbers.
- AspNetUsers: cuentas Identity, TenantId obligatorio con FK a Organizations,
  email normalizado único global y contraseña hasheada por Identity.
- AspNetUserClaims / AspNetUserLogins / AspNetUserTokens: tablas auxiliares de Identity.
- Users: perfiles previos por organización; se conservan, no son cuentas de login.
  Crear un perfil con `/api/users` no otorga acceso ni crea credenciales.
- Branches: sucursales por tenant.

La migración AddSingleTenantAccounts agrega tablas de autenticación sin borrar
los perfiles o sucursales previos. No convierte automáticamente los perfiles demo
en cuentas. Las migraciones se aplican con el script, nunca al arrancar la API.

La API autentica la cookie, recupera la cuenta desde la base y selecciona su
TenantId por solicitud. Ignora X-Tenant-Id. No existe selector de tenant en React,
ni endpoint de cambio de organización. El contexto rechaza cambiar el TenantId
de una cuenta cargada y modificada con EF. No hay tabla de membresías múltiples.

Los filtros EF y las claves compuestas (TenantId, Id) protegen las consultas y
escrituras de sucursales y perfiles. SaveChanges valida su tenant. Las tablas de
Identity se consultan globalmente para autenticar por email y no se exponen en
endpoints genéricos. No usar SQL directo ni IgnoreQueryFilters para saltar estos
controles. Esto no implementa seguridad por filas en SQL Server.

## Sesión y protección de solicitudes

Cookie HttpOnly, SameSite Strict, duración máxima de 8 horas sin extensión
por actividad. La cookie no se guarda en localStorage. En Development funciona
por HTTP local; fuera de desarrollo exige HTTPS. Se valida el security stamp
contra la base en cada solicitud autenticada. Logout elimina la cookie del navegador;
no revoca automáticamente otras sesiones abiertas en otros dispositivos.

Las escrituras requieren X-CSRF-TOKEN: obtenerlo mediante GET /api/auth/csrf
con la misma cookie/sesión. React lo hace antes de login y logout, y lo renueva
tras los cambios de identidad. Cualquier nuevo formulario que escriba debe hacer
lo mismo. El token CSRF no reemplaza la autenticación.

Identity bloquea la cuenta durante 15 minutos después de 5 intentos fallidos.
El login admite hasta 10 solicitudes por minuto por IP (contador local al proceso).
No hay recuperación de contraseña, verificación por email ni MFA todavía. Los roles y el dashboard están documentados en DASHBOARD.md.
Las operaciones dependen del rol Administrador, Administrativo o UsuarioTenant.
Los endpoints de negocio siguen habilitados solo en Development; falta configurar
el despliegue, permisos de negocio y operación de producción.

## Endpoints

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | /api/auth/csrf | Público, devuelve token CSRF |
| POST | /api/auth/login | Email/contraseña + CSRF |
| GET | /api/auth/me | Sesión requerida |
| POST | /api/auth/logout | Sesión + CSRF |
| GET | /api/workspace | Organización, sucursales y perfiles de la sesión |
| GET / POST | /api/branches | Listar / crear sucursales |
| GET / PUT / DELETE | /api/branches/{id} | Consultar / modificar / borrar |
| GET / POST | /api/users | Listar / crear perfiles, NO cuentas |
| GET | /api/users/{id} | Consultar perfil |
| GET / POST | /api/pacientes | Listar / crear pacientes |
| GET / PUT / DELETE | /api/pacientes/{id} | Consultar / modificar / dar de baja lógica |
| GET / POST | /api/productos | Listar / crear productos de la asociación |
| GET / PUT / DELETE | /api/productos/{id} | Consultar / modificar / dar de baja lógica |
| GET / POST | /api/lotes | Listar / registrar lotes por producto |
| GET / PUT / DELETE | /api/lotes/{id} | Consultar / modificar / dar de baja lógica |
| GET | /api/identidad | Identidad visual de la asociación de la sesión |
| GET | /api/identidad/logos/{tenantId}/{tipo}/{archivo} | Logo de la asociación de la sesión |
| PUT / DELETE | /api/identidad/ | Guardar identidad / restaurar predeterminados, solo Administrador |
| POST | /api/identidad/logos/{tipo} | Subir logo principal o compacto, solo Administrador |

Los endpoints de negocio requieren sesión, y CSRF en las escrituras. Sin sesión:
401; ID de otra organización: 404; email de perfil repetido dentro del tenant: 409.
GET /health verifica el proceso, no la base.

## Verificación manual (no ejecutada automáticamente)

1. Aplicar migraciones y crear una cuenta demo y otra acme, con emails diferentes.
2. Abrir la app sin sesión: debe mostrar login, y /api/workspace debe devolver 401.
3. Probar una contraseña incorrecta: mensaje genérico, sin entrar a la organización.
4. Entrar con demo: solo sucursales/perfiles demo. Recargar: mantiene la sesión.
5. En las herramientas del navegador, enviar X-Tenant-Id: acme con sesión demo:
   no debe cambiar de organización. Consultar ID de sucursal acme: 404.
6. Enviar una escritura sin X-CSRF-TOKEN: debe ser rechazada con 400.
7. Cerrar sesión: vuelve al login; una consulta posterior sin cookie devuelve 401.
8. Entrar con acme: solo datos acme. Usar ventanas privadas separadas para dos sesiones.
9. Intentar crear desde terminal otra cuenta con el mismo email, incluso con otra
   combinación de mayúsculas: debe ser rechazada sin cambiar la cuenta original.

## Herramientas

Usar el SDK local `.dotnet` (10.0.400). Para generar migraciones, sin aplicarlas:

```powershell
$env:DOTNET_ROOT = "$PWD\.dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
.\.dotnet\dotnet.exe tool restore
.\.dotnet\dotnet.exe ef migrations add NombreDelCambio --project backend/DispensAR.Api --output-dir Data/Migrations
```

Aplicar con scripts/migrate-db.ps1. La factory de diseño no contiene credenciales.

```powershell
docker compose stop sqlserver
docker compose start sqlserver
```

El volumen dispensar_sqlserver-data conserva los datos. `docker compose down -v`
los elimina. DBeaver con sa ve todos los tenants; no aplica filtros de la API.
La conexión sa es solo local; falta un login SQL de aplicación con permisos limitados.

Documentación: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0

## Cambio a tenants numericos

La migracion NumericTenantIds transforma Organizations.Id y los TenantId de
Branches, Users y AspNetUsers a int. Renombra Organizations.Name a Descripcion.
Demo conserva sus datos bajo el numero 1, ACME bajo el 2. Otras organizaciones
se numeran desde 3 en orden del identificador anterior. Los identificadores de
cuentas, perfiles y sucursales no cambian, ni sus contrasenas.

Detener la API y crear un backup antes de ejecutar scripts/migrate-db.ps1.
La migracion no tiene rollback automatico: para volver al esquema anterior,
restaurar el backup y la version anterior de la aplicacion. No editar las
migraciones antiguas ni borrar tablas. El script aplica tambien las migraciones
previas si la base es nueva. No se ejecuto contra SQL Server durante este cambio.

El contrato de /api/workspace ahora devuelve id numerico y descripcion;
/api/auth/me devuelve tenantId numerico. React ya usa estos campos.
Para crear cuentas nuevas, ingresar 1 o 2, no demo/acme. Las cuentas existentes
siguen accediendo con su email y contrasena habituales.

## Nombres de tablas en español

La migración TablasEnEspanol renombra las tablas existentes, conservando filas,
contraseñas, identificadores y relaciones:

| Nombre anterior | Nombre nuevo |
| --- | --- |
| Organizations | Organizaciones |
| Branches | Sucursales |
| Users | PerfilesUsuarios |
| AspNetUsers | Usuarios |
| AspNetUserClaims | AtributosUsuarios |
| AspNetUserLogins | AccesosExternosUsuarios |
| AspNetUserTokens | TokensUsuarios |

Las referencias a nombres anteriores en las secciones históricas de este documento
corresponden al esquema previo. Usuarios contiene las cuentas de acceso;
PerfilesUsuarios contiene los perfiles, sin credenciales.

Se mantiene __EFMigrationsHistory, la tabla técnica de control de migraciones de EF.
Las columnas y los contratos HTTP conservan sus nombres. Las migraciones anteriores
no se modifican: una instalación nueva las aplica en orden hasta los nombres finales.

Con la API detenida, ejecutar:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
```

Después reiniciar la API y actualizar el listado de tablas en DBeaver. Actualizar
consultas SQL propias que usen los nombres antiguos. La migración de nombres incluye
Down para revertir este cambio; la migración numérica anterior conserva su limitación
de rollback. No se ejecutó esta migración sobre la base durante su preparación.


## Dashboard configurable y roles

Ver [DASHBOARD.md](DASHBOARD.md) para aplicar la migracion y asignar un administrador.
Cada tenant tiene su configuracion independiente de widgets. Las cuentas existentes
reciben UsuarioTenant (solo consulta) hasta que se asigne un rol con scripts/assign-role.ps1.


## ABM del catálogo global

Ver [ELEMENTOS.md](ELEMENTOS.md) para crear, modificar y dar de baja elementos
 desde el menú izquierdo. Requiere el permiso de administrador de plataforma,
independiente del rol del tenant. Incluye el comando para asignarlo localmente.

## Pacientes, productos y lotes

Con SQL Server iniciado y la API detenida, aplicar la migración
`GestionPacientesProductos` (crea solo las tablas Pacientes, Productos y Lotes;
no modifica datos existentes):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
```

La migración está generada pero todavía no se ejecutó contra la base. Requiere
revisión y aplicación manual.

Tablas nuevas, todas con `(TenantId, Id)` como clave, filtro EF por tenant y
`Version` rowversion:

- Pacientes: Nombres, Apellidos, Dni (6 a 10 dígitos, único por asociación,
  no se modifica tras el alta), FechaNacimiento, Estado Activo/Inactivo,
  AutorizacionHasta (fecha de vencimiento de la documentación habilitante) y
  Observaciones. La baja lógica pasa a Inactivo: la fila y su DNI quedan
  reservados, igual que un elemento dado de baja en el catálogo global.
- Productos: catálogo propio de cada asociación. Codigo único por tenant,
  minúsculas con guion bajo y permanente; Descripcion; Unidad en gramos,
  mililitros o unidades. Las cantidades nunca se suman entre unidades
  distintas. La unidad no cambia si el producto ya tiene lotes.
- Lotes: por producto dentro del mismo tenant (FK compuesta
  TenantId + ProductoId). Codigo único por producto y permanente,
  FechaVencimiento opcional. Un lote no cambia de producto.

Escrituras (POST/PUT/DELETE) requieren rol Administrador o Administrativo;
UsuarioTenant solo consulta. Borrar es siempre baja lógica con `If-Match`;
una edición con `Version` obsoleta devuelve 409 y obliga a recargar. DNI o
código repetidos devuelven 409. El frontend muestra Pacientes, Productos y
Lotes en el menú "OPERACIÓN" y oculta las acciones de escritura para
UsuarioTenant, pero el control real lo hace la API.

El widget "Pacientes activos" ahora cuenta solo pacientes con estado Activo
de tu asociación, con unidad "pacientes". Cantidades dispensadas, stock y
alertas siguen en "Sin datos": su fuente llega con stock, movimientos y
dispensaciones. Los lotes no tienen cantidades todavía.

Verificación manual pendiente (no ejecutada):

1. Crear un paciente como Administrador y repetir el DNI: 409.
2. Entrar con UsuarioTenant: las páginas cargan y no ofrecen acciones de escritura;
   un POST directo devuelve 403.
3. Editar en dos ventanas el mismo producto y guardar dos veces con la misma
   versión: la segunda recibe 409.
4. Dar de baja un paciente y reactivarlo desde Editar cambiando el estado.
5. Crear un lote para un producto dado de baja: rechazado; el lote de un producto
   existente se conserva al dar de baja el producto.
6. Consultar desde otra asociación el id de un paciente, producto o lote ajeno: 404.
7. "Pacientes activos" coincide con la lista filtrada por estado Activo.

## Estilos compartidos e identidad visual

Los estilos dejaron de vivir en `App.css`/`index.css`: ahora hay tokens y variables
en `frontend/src/styles/` (tokens, base, componentes y uno por módulo) y componentes
compartidos en `frontend/src/components/`. Cada asociación puede configurar logos y
colores desde Configuración → Identidad visual (solo Administrador); el
ThemeProvider aplica el tema del tenant autenticado y lo limpia al cerrar sesión.
Ver [IDENTIDAD.md](IDENTIDAD.md) para la migración pendiente, los endpoints, las
validaciones de imágenes y contraste, y la verificación manual.

## Interfaz en el celular

Mobile primero en todo el front: viewport con `viewport-fit=cover`, `theme-color`
y `color-scheme: light`. El menú lateral pasa a barra deslizable horizontal en
pantallas chicas (cinco ítems sin scroll vertical). Las tablas de Elementos,
Pacientes, Productos y Lotes se convierten en tarjetas hasta 640px: cada celda
muestra su `data-etiqueta` y su valor, sin scroll horizontal. Los filtros, el
encabezado y las acciones del formulario se apilan a ancho completo. En puntero
burdo los botones de fila, navegación y orden tienen 44px mínimos y el switch
amplía su área táctil sin cambiar de tamaño. Se desactivó el zoom por foco de
iOS (los controles ya usan 16px) y el estado `:hover` solo aplica con mouse.
El diseño responsive no se verificó en un dispositivo real: probá en Chrome
DevTools (360px) y en el celular el menú, una tabla, un formulario y el login.
# Demo en Azure

La preparacion del despliegue gratuito, las variables y los pasos pendientes estan en [AZURE-DEMO.md](AZURE-DEMO.md). El workflow es manual y no aplica migraciones automaticamente.
