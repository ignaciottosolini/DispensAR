$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$dotnet = Join-Path (Get-Location) '.dotnet\dotnet.exe'
& $dotnet build backend/DispensAR.Api
if ($LASTEXITCODE -ne 0) { throw 'La compilacion fallo.' }
& $dotnet run --no-build --project backend/DispensAR.Api --launch-profile http -- --assign-role
if ($LASTEXITCODE -ne 0) { throw 'No se pudo asignar el rol. Revisa el error anterior.' }
