$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$dotnet = Join-Path (Get-Location) '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { throw 'No se encontro el SDK local en .dotnet.' }
& $dotnet build backend/DispensAR.Api
if ($LASTEXITCODE -ne 0) { throw 'La compilacion fallo. No se aplicaron migraciones.' }
& $dotnet run --no-build --project backend/DispensAR.Api --launch-profile http -- --migrate
if ($LASTEXITCODE -ne 0) { throw 'La migracion fallo. Revisa el mensaje anterior.' }
