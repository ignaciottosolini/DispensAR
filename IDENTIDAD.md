# Identidad visual por asociación

Cada asociación puede configurar sus logos y colores. Todos los usuarios de la
asociación ven el mismo tema; solo el rol Administrador lo edita. Los componentes
son compartidos: la identidad se aplica mediante variables CSS, nunca con hojas
distintas por tenant.

## Activación inicial

La migración `IdentidadVisualPorOrganizacion` está generada pero **todavía no se
ejecutó contra la base**. Con SQL Server iniciado y la API detenida, desde
C:\DispensAR (crear backup previo, como en las entregas anteriores):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
```

La migración crea únicamente la tabla `IdentidadesVisuales` (clave `TenantId`,
FK con `ON DELETE RESTRICT` hacia `Organizaciones` y columna `Version` rowversion).
No modifica datos existentes. Para regenerarla en otra máquina, sin aplicarla:

```powershell
$env:DOTNET_ROOT = "$PWD\.dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
.\.dotnet\dotnet.exe tool restore
.\.dotnet\dotnet.exe ef migrations add IdentidadVisualPorOrganizacion --project backend/DispensAR.Api --output-dir Data/Migrations
```

Reiniciar la API y React. Los tenants existentes siguen funcionando sin fila en la
tabla: sin configuración se aplica el tema predeterminado de DispensAR.

Las subidas crean `backend/DispensAR.Api/storage/logos/{tenantId}/` con nombres
generados por el servidor; la carpeta está excluida del control de versiones.

## Estructura de estilos compartidos

`frontend/src/styles/` reemplaza a `App.css` e `index.css` (eliminados):

| Archivo | Contenido |
| --- | --- |
| `tokens.css` | Todas las variables CSS: marca, fondos, superficies, textos, bordes, estados éxito/advertencia/error, tipografía, espaciados, tamaños, radios, sombras e interacción. Define el tema predeterminado de DispensAR en `:root`. |
| `base.css` | Estilos de elemento (`button`, `input`, `section`, foco, `:hover` con mouse, disabled) usando solo variables. |
| `components.css` | Componentes compartidos: marca, barra superior, barra lateral y navegación, encabezado de página, alertas, tarjetas de widget, etiquetas de estado, diálogo de confirmación y acciones de formulario, con sus media queries. |
| `login.css`, `dashboard.css`, `catalogos.css`, `identidad.css` | Estilos específicos de cada módulo, acotados a la clase de su pantalla (`.login`, `.dashboard`, `.catalog-page`, `.identidad`). |

No hay ningún color literal fuera de `tokens.css`. Cambiar un token actualiza todas
las pantallas que lo usan. `frontend/src/components/` extrae lo que estaba
duplicado: `Confirmacion` (antes repetida cuatro veces), `Alerta`/`Aviso`,
`EncabezadoPagina`, `Campo`/`Selector`/`EtiquetaCheck`, `EtiquetaEstado` y `Marca`.
Se conservaron el responsive (tablas→tarjetas, menú deslizable, área táctil 44px),
la accesibilidad y el resto del marcado. Dos excepciones voluntarias al color
original: `#246b4f` del interruptor y `#dae5df` de `section` se unificaron en
`--color-primario` y `--color-linea`.

## Resolución del tema

`frontend/src/theme/tema.ts` es el único lugar que resuelve el tema:

- `variablesTema(identidad)` deriva los estados de interacción coherentes: foco,
  superficies de marca y ribetes/seleccionado de la barra lateral, a partir de los
  colores configurados. Los colores nulos dejan intacta la variable predeterminada.
- `aplicarTema`/`limpiarTema` escriben y revierten las variables sobre `:root`
  (incluido el `theme-color` del celular).
- `theme/colores.ts` replica en el navegador las mismas reglas de contraste WCAG
  que el backend, para advertir antes de guardar.

Flujo: iniciar sesión → el backend identifica el tenant desde la cookie → React
consulta `GET /api/identidad` → logos y colores de esa asociación. Si la carga de
la configuración falla, la aplicación funciona con el tema predeterminado. Al cerrar
sesión o expirar la sesión se limpia el tema (y se descartan respuestas tardías).
El login mantiene la identidad genérica de DispensAR: antes de autenticar no se
conoce el tenant y no se intenta descubrir por el email.

## Configuración y permisos

Tabla `IdentidadesVisuales`: una fila por tenant (`TenantId` como clave primaria),
con `LogoPrincipalRuta`, `LogoPrincipalContenido`, `LogoCompactoRuta`,
`LogoCompactoContenido`, `ColorPrimario`, `ColorSecundario`, `ColorSidebarFondo`,
`ColorSidebarTexto` y `Version` rowversion. Aplica el filtro EF por tenant y la
validación de escrituras de `AppDbContext`; una fila que solo repite valores
predeterminados se guarda como "sin configuración".

| Acción | Administrador | Administrativo | UsuarioTenant | Plataforma |
| --- | --- | --- | --- | --- |
| Ver y usar el tema de su asociación | Sí | Sí | Sí | — |
| Editar identidad visual de su asociación | Sí | No | No | No (por ahora) |
| Editar identidad de otra asociación | No | No | No | No |

La edición por el administrador de plataforma quedó explícitamente fuera de esta
entrega. Para habilitarla: grupos `/api/plataforma/organizaciones/{tenantId}/identidad`
bajo la política `PlatformAdministration`, selección explícita de asociación,
`TenantContext.Select(tenantId)` validado en el endpoint y almacenamiento con la
misma convención de rutas `{tenantId}/...`.

## Endpoints

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | /api/identidad | Sesión (todos los roles). Devuelve `descripcion` e `identidad` (null si no configuró). |
| GET | /api/identidad/logos/{tenantId}/{tipo}/{archivo} | Sesión. Solo sirve archivos del tenant de la sesión; el `tenantId` del path debe coincidir, si no 404. |
| PUT | /api/identidad/ | Administrador + CSRF. Reemplaza la configuración completa; 409 si `Version` cambió. |
| DELETE | /api/identidad/ | Administrador + CSRF + `If-Match`. Restaura predeterminados y borra los archivos. |
| POST | /api/identidad/logos/principal o /compacto | Administrador + CSRF, multipart (campo `archivo`). Subida preparada para previsualizar; se persiste al guardar. |

Las rutas se muestran sin el prefijo `/api` que agrega el grupo, salvo donde ya
aparece explícito. La barra final en `/api/identidad/` es la ruta registrada por el
subgrupo; coincide con la llamada del frontend.

La configuración visual está separada de la habilitación de widgets
(`WidgetsDashboardOrganizaciones`) y del catálogo global (`WidgetsDashboard`).

### Validaciones (backend, además del frontend)

- Colores: únicamente `#rgb` o `#rrggbb`, normalizados a seis minúsculas; no se
  acepta ningún otro texto, por lo que no puede inyectarse CSS arbitrario.
- Contraste WCAG sobre la combinación efectiva (configurado + predeterminados):
  primario vs blanco ≥ 4.5:1 (texto de botones), secundario ≥ 3:1 sobre blanco y
  sobre el fondo de la aplicación, y texto de la barra lateral vs su fondo ≥ 4.5:1.
  Éxito, advertencia y error no se configuran: conservan su significado.
- Logos: PNG, JPEG o WebP verificados por firma y dimensiones leídas del archivo
  (no del `Content-Type` del navegador). Máximo 1 MB, 4096 px por lado y 12 MP.
  Nombres generados por el servidor `{tenantId}/{tipo}/{guid}.ext`, en carpetas por
  tenant bajo `storage/logos` (configurable con `Storage:LogosPath`). No se guarda
  base64 en la base.

## Pantalla Configuración → Identidad visual

Aparece en el menú izquierdo (grupo CONFIGURACIÓN) solo para Administrador. Muestra
la asociación que se está configurando. Permite subir, previsualizar, reemplazar y
quitar ambos logos, elegir colores con selector o hexadecimal, y ver una
previsualización construida con los mismos componentes de la aplicación (barra
superior, barra lateral, botones, tarjetas del dashboard y etiquetas de estado).
Las variables del borrador se aplican solo al contenedor de la previsualización.
Guardar envía `Version`; Cancelar edición recupera el estado del servidor;
Restaurar predeterminados pide confirmación.

## Comprobaciones manuales pendientes

No se ejecutaron migraciones ni pruebas contra la base de datos.

1. Migrar y entrar sin configurar: todo se ve igual que antes (tema predeterminado)
   y el login no cambia.
2. Entrar como Administrador: aparece "Identidad visual" en CONFIGURACIÓN; con
   Administrativo o UsuarioTenant no aparece y un PUT directo devuelve 403.
3. Cargar un logo de Demo y guardar: el encabezado muestra el logo; recargar la
   aplicación lo conserva.
4. Configurar ACME con otros logos y colores en otra ventana privada: ambas
   asociaciones muestran identidades distintas usando los mismos componentes.
5. Entrar a Demo con una cuenta sin permisos: ve los colores de su asociación y no
   puede guardar (403 incluso llamando a la API directamente).
6. Subir un `.png` con bytes falsificados, un GIF, un archivo de más de 1 MB o de
   más de 4096 px: rechazado con 400 por validar el contenido real.
7. Escribir `red`, `body{...}` o un hexadecimal sin contraste (p. ej. primario
   claro con texto blanco): rechazado en la pantalla y también en la API.
8. Reemplazar y quitar logos: la previsualización cambia solo dentro de su panel;
   al guardar, el encabezado real refleja el estado guardado.
9. Cancelar edición y Restaurar predeterminados: el tema de la aplicación no
   cambió en ningún momento; tras restaurar se ve el verde de DispensAR.
10. Editar en dos ventanas la misma configuración: la segunda escritura recibe 409
    y debe recargar.
11. Cerrar sesión y abrir el login: sin colores ni logo de la asociación anterior;
    al expirar la cookie (401 en cualquier llamada) ocurre lo mismo.
12. Pedir el logo con la sesión de otra asociación: 404. Un `TenantId` enviado desde
    el navegador no afecta la selección.
13. Detener el backend antes de `/api/identidad`: el login, y el resto con sesión
    ya cargada, siguen funcionando con el tema predeterminado.
14. Cambiar un valor en `tokens.css` (p. ej. `--color-advertencia-fondo`): la
    tarjeta de widget con módulo pendiente, la pantalla de elementos y el formulario
    de identidad lo reflejan a la vez sin tocar componentes.
