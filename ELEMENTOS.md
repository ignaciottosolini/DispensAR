# ABM global de elementos

## Activarlo

Con SQL Server iniciado y la API detenida, desde C:\DispensAR:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\assign-platform-admin.ps1
```

El segundo comando solicita el email de la cuenta y muestra su tenant. Elegir
1 para otorgar administración de plataforma o 0 para revocarla. No cambia el rol
del tenant. Rota el security stamp, así que después es necesario iniciar sesión
nuevamente. El permiso no se puede otorgar desde una API pública.

Reiniciar el entorno e ingresar: aparece **Elementos** en el menú del extremo
izquierdo, debajo de Administración. En pantallas pequeñas el menú se acomoda
arriba del contenido. El dashboard sigue accesible desde el mismo menú.

## Dos ámbitos independientes

| Función | Permiso |
| --- | --- |
| ABM del catálogo global | Usuarios.EsAdministradorPlataforma = true |
| Activar y ordenar widgets para su asociación | Rol Administrador del tenant |
| Consultar widgets habilitados de su asociación | Cualquier cuenta autenticada |

Ser Administrador de un tenant no concede permiso global. Tener permiso de
plataforma tampoco habilita consultar datos clínicos u operativos de otras
asociaciones, ni permite configurar el dashboard del tenant sin el rol correspondiente.
Si necesitás ambos permisos para tu cuenta, también usar scripts/assign-role.ps1
para asignar el rol 2 (Administrador). Todas las cuentas existentes comienzan con
EsAdministradorPlataforma = false al aplicar esta migración.

## Operaciones

- Alta: código único y permanente, descripción, tamaño, indicador y disponibilidad.
- Modificación: descripción, tamaño, indicador y disponibilidad. Código inmutable.
- Baja lógica: oculta el elemento para todas las asociaciones. No elimina la fila
  ni las preferencias de cada tenant. La pantalla pide confirmar antes de la baja.
- Reactivación: Editar → Disponible para las asociaciones. Recupera las preferencias
  anteriores de los tenants; puede volver a mostrarse donde antes estaba habilitado.
- Listado: búsqueda por código/descripción y filtro activos/bajas/todos.

Un elemento nuevo queda apagado en cada tenant hasta que su administrador lo active
mediante Configurar widgets. El estado global y la preferencia del tenant se validan
juntos en el servidor antes de devolver datos de un widget.

El indicador se selecciona de una lista conocida: sucursales tiene datos reales;
pacientes, dispensaciones, stock y alertas siguen pendientes. Crear un elemento
no implementa una fuente nueva de datos. Puede reutilizar un indicador existente
con otro título/tamaño. Agregar un cálculo nuevo requiere desarrollar su proveedor.
No se admite SQL, JavaScript, HTML ni URLs ejecutables como fuente.

## API y persistencia

Todos los endpoints requieren sesión y el permiso PlatformAdministration:

- GET /api/plataforma/elementos/: lista global y opciones de indicadores.
- POST /api/plataforma/elementos/: alta.
- PUT /api/plataforma/elementos/{id}: edición; requiere Version del elemento.
- DELETE /api/plataforma/elementos/{id}: baja lógica; requiere If-Match con Version.

Las escrituras también exigen X-CSRF-TOKEN. La interfaz obtiene el token usando
la sesión. Códigos duplicados devuelven 409. Version es rowversion en SQL Server;
una edición o baja con versión obsoleta devuelve 409 y requiere recargar.

La migración AbmCatalogoElementos agrega Activo, TipoIndicador y Version a
WidgetsDashboard, y EsAdministradorPlataforma a Usuarios. Conserva el catálogo
existente y los ajustes por tenant. No se aplicó a la base durante este desarrollo.

## Comprobación manual pendiente

1. Migrar, otorgar permiso e ingresar: el menú Elementos debe aparecer a la izquierda.
2. Crear un elemento con código único asociado a Sucursales: aparece en el catálogo,
   pero no se habilita automáticamente en ningún tenant.
3. Con rol Administrador del tenant, habilitarlo y comprobar que muestra sus sucursales.
4. Editar descripción y tamaño: al recargar se reflejan donde esté habilitado.
5. Cancelar una baja no cambia datos. Confirmarla oculta el widget en todas las
   asociaciones y su endpoint de datos devuelve 404.
6. Editar y reactivar: se restauran las preferencias guardadas por tenant.
7. Repetir código debe devolver 409; código inválido o indicador desconocido: 400.
8. Dos pestañas editando el mismo elemento: la segunda versión obsoleta debe fallar
   con 409. No se debe sobrescribir un cambio concurrente.
9. Cuenta sin permiso de plataforma: menú oculto y endpoints de catálogo en 403,
   incluso si es Administrador del tenant. Sin sesión: 401. Sin CSRF: 400.
10. Revocar permiso con el script: exige nuevo login y deja de permitir el ABM.

No se ejecutaron pruebas contra SQL Server ni se modificaron datos durante la implementación.
