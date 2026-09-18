$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Test-Path .env)) {
    $bytes = New-Object byte[] 24
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $password = 'Ds!9' + [Convert]::ToBase64String($bytes)
    Set-Content -LiteralPath .env -Value "MSSQL_SA_PASSWORD=$password" -Encoding ascii
    Write-Host 'Credencial generada en .env (excluido de Git).'
}
$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    foreach ($candidate in @("$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin\docker.exe", 'C:\Program Files\Docker\Docker\resources\bin\docker.exe')) {
        if (Test-Path $candidate) { $docker = Get-Item $candidate; break }
    }
}
if (-not $docker) { throw 'Instalá y abrí Docker Desktop con el motor WSL 2. Luego ejecutá esta tarea nuevamente.' }
$dockerPath = if ($docker.Source) { $docker.Source } else { $docker.FullName }
& $dockerPath info --format '{{.OSType}}'
if ($LASTEXITCODE -ne 0) { throw 'Docker no está listo. Abrí Docker Desktop y esperá a que arranque el motor.' }
& $dockerPath compose up -d --wait --wait-timeout 240 sqlserver
if ($LASTEXITCODE -ne 0) { throw 'SQL Server no pudo iniciar. Revisá docker compose logs sqlserver.' }
# Send the shell script through stdin to preserve quotes in Windows PowerShell 5.1.
$initializeDatabase = @'
set -eu
export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"
exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b <<'SQL'
IF DB_ID(N'DispensAR') IS NULL
BEGIN
    CREATE DATABASE [DispensAR];
END;
GO
SELECT name, state_desc FROM sys.databases WHERE name = N'DispensAR';
GO
SQL
'@
# PowerShell may append CRLF to pipeline input; remove CR inside the container.
$initializeDatabase.Replace("`r", '') | & $dockerPath compose exec -T sqlserver sh -c 'tr -d \\015 | sh'
if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear o verificar la base DispensAR. Copia tambien el error anterior de sqlcmd para identificar la causa.' }
Write-Host 'SQL Server listo: localhost,1433 | Base: DispensAR | Usuario: sa | Contraseña: archivo .env'
