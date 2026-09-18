# Dashboard por asociación y permisos

## Activación inicial

Detener la API y mantener SQL Server iniciado. Desde C:\DispensAR:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\assign-role.ps1
```

El segundo comando pregunta el email de una cuenta existente, muestra su tenant
para identificarla y pide el rol: 0 UsuarioTenant, 1 Administrativo, 2 Administrador.
Para configurar widgets, asignar 2 a tu cuenta. El comando se ejecuta localmente,
no está expuesto por HTTP. Cambiar el rol invalida las sesiones anteriores.

Reiniciar la API y React e iniciar sesión de nuevo en http://localhost:5173.
Las cuentas existentes reciben UsuarioTenant (0) al migrar: no se promueven
administradores automáticamente. El alta de nuevas cuentas también pregunta el rol.

## Privilegios iniciales

| Acción | Administrador | Administrativo | UsuarioTenant |
| --- | --- | --- | --- |
| Consultar dashboard y datos de su tenant | Sí | Sí | Sí |
| Activar/desactivar y ordenar widgets de su tenant | Sí | No | No |
| Crear/modificar/borrar sucursales | Sí | Sí | No |
| Crear perfiles | Sí | Sí | No |
| Asignar roles desde la web | No | No | No |
| Acceder a datos de otro tenant | No | No | No |

Estos permisos cubren los endpoints actuales. No hay roles globales, membresías
múltiples ni permisos particulares por widget todavía. Administrativo y UsuarioTenant
ven los mismos indicadores habilitados; se diferencian por sus operaciones permitidas.

## Catálogo y datos

- WidgetsDashboard: catálogo global con código estable, descripción y tamaño.
- WidgetsDashboardOrganizaciones: TenantId + WidgetId como clave, Habilitado,
  Orden y Version (rowversion) para detectar cambios concurrentes.
- Usuarios.Rol: rol de la cuenta dentro de su único tenant.

Widgets iniciales: pacientes activos, cantidad dispensada, stock disponible,
sucursales, alertas y últimas dispensaciones. Demo inicia con los primeros cuatro;
ACME con pacientes, dispensado y sucursales. Alertas y últimas dispensaciones
comienzan apagadas. Una organización o widget sin configuración queda apagado.

Solo sucursales y pacientes activos tienen datos operativos: sucursales cuenta
TODAS las sucursales registradas, porque aún no existe un estado activa/inactiva,
y pacientes activos cuenta únicamente los Pacientes con estado Activo de la
asociación, con unidad "pacientes". Los otros widgets muestran Sin datos
y explican que el módulo está pendiente. No cuentan perfiles como pacientes,
no mezclan gramos/mililitros/unidades y no inventan dispensaciones o alertas.
Por ahora la vista es general: no hay filtros de fecha o sucursal.

Cada widget consulta su propio endpoint, tiene carga/error/reintento independiente
y solo se solicita si está habilitado. La pantalla se adapta al ancho disponible.
El administrador cambia interruptores y orden y debe pulsar Guardar cambios.
Cancelar descarta el borrador; apagar todo muestra un estado vacío.
Los demás operadores ven los cambios al recargar o pulsar Actualizar.
Apagar un widget no elimina datos ni deshabilita el módulo de negocio.

## API

- GET /api/dashboard: organización y widgets habilitados, según sesión.
- GET /api/dashboard/widgets/{codigo}: datos de un widget habilitado; apagado: 404.
- GET /api/dashboard/configuracion: catálogo y ajustes, solo Administrador.
- PUT /api/dashboard/configuracion: lista completa con id, habilitado, orden y
  version recibida. Orden 0..N-1 sin duplicados; solo Administrador y con CSRF.

La API nunca toma TenantId del cuerpo ni de X-Tenant-Id. EF filtra la configuración
por tenant y SaveChanges aplica la misma validación que a los demás datos tenant.
Las versiones se comparan y EF valida rowversion al escribir. Una configuración
concurrente devuelve 409 en lugar de sobrescribir cambios; el usuario debe recargar.
Todos los endpoints de negocio siguen siendo de Development, como hasta ahora.

## Comprobaciones manuales pendientes

No se ejecutaron migraciones ni pruebas de integración sobre tu base.

1. Migrar, asignar rol 2 y entrar: aparece Configurar widgets.
2. Apagar stock, guardar y recargar: desaparece y no se solicita su endpoint.
3. Invocar manualmente el endpoint apagado: debe devolver 404.
4. Cambiar el orden y recargar: debe persistir para toda la asociación.
5. Apagar todos: aparece el dashboard vacío; se puede volver a configurar.
6. Entrar con una cuenta ACME: los cambios de Demo no deben afectarla.
7. Con Administrativo/UsuarioTenant, configuración GET/PUT devuelve 403.
8. UsuarioTenant no puede escribir sucursales ni perfiles; sin cookie devuelve 401.
9. Agregar X-Tenant-Id de otra organización: no cambia los datos de la sesión.
10. Abrir configuración en dos pestañas, cambiar el mismo widget en ambas:
    la segunda escritura obsoleta debe recibir 409 y pedir recarga.
11. Una escritura sin CSRF debe ser rechazada. Cambiar rol con el script exige
    nuevo login. Interrumpir la solicitud de un widget no impide mostrar los demás.

## Catálogo global y roles

Se agregó un ABM del catálogo: ver ELEMENTOS.md. El permiso global
Usuarios.EsAdministradorPlataforma es independiente de los roles del tenant.
El administrador de tenant sigue configurando solo los elementos activos de su
asociación. La baja global conserva sus preferencias, pero bloquea su visualización
y su endpoint de datos hasta que el administrador de plataforma lo reactive.
