# Demo gratuita en Azure

Recursos de demo configurados: App Service `dispensar-demo` (Linux F1, West US 3) y base `free-sql-db-1539727` en `dispensar-sql-ignaciottosolini` (oferta gratuita, AutoPause al agotar el cupo). Las migraciones y el alta del administrador fueron ejecutadas por el operador.

GitHub utiliza la identidad `dispensar-github-demo`, con Website Contributor limitado al App Service. Las variables del repositorio y el environment `demo` estan configurados. Para publicar una nueva version: Actions > Publicar demo en Azure > Run workflow, rama main. El workflow no modifica el esquema ni crea cuentas.

## Arquitectura

React compilado se publica dentro de `wwwroot` de la API .NET 10. El navegador utiliza el mismo origen para `/api`, cookies y CSRF. No se ejecuta Vite en Azure. Las rutas de negocio conservan autenticacion, permisos y filtros por tenant en Production.

Destino previsto: App Service Linux, publicacion de codigo .NET 10, plan Free F1 y Azure SQL Database con la oferta gratuita. Confirmar disponibilidad en la suscripcion y region antes de crear. No aceptar una sustitucion automatica por Basic ni activar cargos adicionales de SQL.

## Referencia para crear los recursos

1. Crear una cuenta en https://azure.microsoft.com/free/ y completar personalmente la verificacion requerida.
2. Comprobar la suscripcion y elegir una region que admita ambos recursos gratuitos.
3. Crear un grupo de recursos para la demo y un App Service **Free F1**. Desactivar Always On, activar HTTPS Only y usar el dominio proporcionado por Azure.
4. Crear una **Azure SQL Database** mediante su oferta gratuita; comprobar que el costo estimado sea cero y elegir **Auto-pause the database until next month**. No confundir con SQL Managed Instance o una VM SQL.
5. Configurar el firewall SQL para las IP salientes de la aplicacion y, temporalmente, la IP del operador que aplicara las migraciones. Evitar abrir a cualquier origen.

F1 tiene un cupo diario de CPU y SQL un cupo mensual. No se promete disponibilidad continua. No crear recursos adicionales de pago (registro de contenedores, planes superiores, almacenamiento o telemetria facturable) para completar esta guia. Revisar el costo de cada recurso en el portal.

## Variables de App Service (Linux)

Configurar en el portal, nunca en Git:

| Nombre | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DispensAR` | Conexion a la base Azure SQL; `Encrypt=True;TrustServerCertificate=False` |
| `Storage__LogosPath` | `/home/dispensar/logos` |
| `DataProtection__KeysPath` | `/home/dispensar/keys` |

Usar el almacenamiento persistente de `/home` del App Service, dentro de su cuota, fuera de `wwwroot`. Los logos y las claves no deben borrarse durante publicaciones. Verificar la persistencia tras reiniciar y desplegar antes de dar por operativa la demo. Las claves son sensibles: restringir el acceso al sitio/almacenamiento; no publicarlas ni incluirlas en artefactos. El HTTPS lo exige App Service; no configurar confianza indiscriminada en cabeceras de proxies.

No usar la cuenta administradora SQL como credencial permanente de la aplicacion. Separar la credencial de migraciones (DDL) de la de ejecucion (lectura/escritura de datos).

## Compilar localmente sin tocar la base

Desde la raiz:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-demo.ps1
```

Instala dependencias con el lockfile, ejecuta lint, compila React y publica .NET en `artifacts/demo`. No arranca la API ni conecta con SQL. No incluye la base local, `.env`, logos subidos ni claves.

## GitHub Actions y Azure

El workflow `Publicar demo en Azure` se ejecuta manualmente desde Actions. Sin `AZURE_WEBAPP_NAME`, solo compila y genera artefactos; no despliega.

Una vez creado App Service, configurar una identidad de despliegue con OIDC y permiso limitado a esa aplicacion. La credencial federada debe confiar en:

```text
issuer: https://token.actions.githubusercontent.com
subject: repo:ignaciottosolini@96127714/DispensAR@1376259756:environment:demo
audience: api://AzureADTokenExchange
```

El subject anterior fue verificado contra el token presentado por GitHub en este repositorio; contiene los identificadores estables del propietario y del repositorio. Para otros repositorios verificar el subject real en el paso azure/login y no copiarlo literalmente.

Crear el environment `demo` en GitHub y estas **variables del repositorio**:

- `AZURE_WEBAPP_NAME`
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID` (directorio Microsoft Entra; no es el TenantId de DispensAR)
- `AZURE_SUBSCRIPTION_ID`

No almacenar perfiles de publicacion, contrasenas SQL ni tokens en archivos del repositorio. El workflow no aplica migraciones ni crea cuentas.

## Base y primer administrador

El workflow entrega `migraciones-demo` con un SQL idempotente generado sin acceso a bases. Revisarlo y aplicarlo explicitamente a la **base nueva de demo** antes de usar la app. No copiar datos reales de pacientes para una presentacion.

Para dar de alta la cuenta inicial, un operador puede usar desde su terminal local `scripts/create-account.ps1` con `ConnectionStrings__DispensAR` apuntando temporalmente a la base remota. Ingresar la conexion mediante un mecanismo seguro, sin pegarla en el historial. El script solicita tenant, email, rol y contrasena oculta. Seleccionar Administrador. Quitar esa variable al terminar para no dirigir operaciones locales posteriores a Azure y cerrar la regla temporal del firewall. `--create-account` sigue restringido a Development: no existe un endpoint publico de provisionamiento ni se cambia el entorno de la web publicada.

## Verificacion manual tras publicar

- `/health` responde; `/` carga React sobre HTTPS.
- Login y logout funcionan; `/api/ruta-inexistente` devuelve 404, no HTML.
- Dos cuentas de tenants distintos mantienen separados datos y configuraciones visuales.
- Un usuario sin privilegios no puede editar configuraciones administrativas.
- Los logos sobreviven a un reinicio y a un nuevo despliegue.
- Las claves de proteccion persisten; la sesion se limpia correctamente al salir.
- El costo estimado conserva los planes gratuitos y SQL pausa al agotar el cupo.

Referencias: https://learn.microsoft.com/azure/app-service/quickstart-dotnetcore
https://learn.microsoft.com/azure/app-service/deploy-github-actions
https://learn.microsoft.com/azure/azure-sql/database/free-offer
