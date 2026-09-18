# SQL Server local

Requiere Docker Desktop iniciado con motor WSL 2.

Desde VS Code: **Terminal → Ejecutar tarea → SQL Server: iniciar**.
O desde la raíz del proyecto:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-db.ps1
```

El script genera `.env` si falta, espera que SQL Server responda y crea la base
`DispensAR` si no existe. No reemplaza credenciales existentes.

## Conexión

- Servidor: `localhost,1433`
- Base: `DispensAR`
- Autenticación: SQL Server; usuario: `sa`
- Contraseña: valor de `MSSQL_SA_PASSWORD` en `.env` (excluido de Git).
- Cifrado activado, confiar en el certificado del servidor en este entorno local.

SQL Server 2025 Developer es para desarrollo. El puerto solo se publica en
127.0.0.1. Los datos se conservan en el volumen `dispensar_sqlserver-data`.
La etiqueta `2025-latest` es móvil; no se actualiza automáticamente.
La API usa Entity Framework Core. Antes de iniciarla, aplicar las migraciones:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-db.ps1
```

Esto crea las tablas y los datos de ejemplo. Ver README.md para los endpoints
y la comprobación manual del aislamiento. Para crear una cuenta de login usá
`scripts/create-account.ps1` después de migrar. La contraseña de esa cuenta es
distinta de la credencial SQL de `.env`. Falta un usuario SQL propio de aplicación
antes de desplegar fuera de desarrollo.

```powershell
docker compose ps
docker compose logs --tail 50 sqlserver
docker compose stop sqlserver
docker compose start sqlserver
```

`docker compose down` conserva los datos; agregar `-v` elimina el volumen.
Cambiar `.env` no cambia la contraseña de una instancia ya inicializada.

Si WSL informa `REGDB_E_CLASSNOTREG`, hay que reparar o instalar WSL antes de
iniciar Docker. Si Windows solicita reinicio, guardá tu trabajo y reiniciá.
Luego abrí Docker Desktop, completá su configuración y ejecutá la tarea.

Referencia: https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17
