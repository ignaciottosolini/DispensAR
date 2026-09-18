$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$localDotnet = Join-Path $root '.dotnet/dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
$output = Join-Path $root 'artifacts/demo'

# Delete only this script's generated output; never project sources or uploaded logos.
if (Test-Path $output) {
    $resolved = (Resolve-Path -LiteralPath $output).Path
    $expected = [IO.Path]::GetFullPath((Join-Path $root 'artifacts/demo'))
    if ($resolved -ne $expected) { throw 'Ruta de salida inesperada.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Push-Location $root
try {
    & npm --prefix frontend ci
    if ($LASTEXITCODE -ne 0) { throw 'Fallo npm ci.' }
    & npm --prefix frontend run lint
    if ($LASTEXITCODE -ne 0) { throw 'Fallo lint.' }
    & npm --prefix frontend run build
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion de React.' }
    & $dotnet publish backend/DispensAR.Api -c Release -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion de .NET.' }
    $webroot = Join-Path $output 'wwwroot'
    New-Item -ItemType Directory -Force -Path $webroot | Out-Null
    Copy-Item -Path (Join-Path $root 'frontend/dist/*') -Destination $webroot -Recurse -Force
    Write-Host "Paquete generado en $output. No se aplicaron migraciones."
} finally {
    Pop-Location
}
