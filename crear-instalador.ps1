# Genera el instalador de GazeStream con Velopack.
#
# Uso (desde la carpeta del proyecto):
#   .\crear-instalador.ps1
# La versión se toma de <Version> en GazeStream.csproj: para una versión nueva, cambiarla ahí.
#
# Requisitos (una sola vez en la PC que arma el instalador):
#   - .NET 8 SDK
#   - vpk, la herramienta de Velopack, en la MISMA versión que el paquete NuGet Velopack del .csproj (1.0.1):
#       dotnet tool install -g vpk --version 1.0.1
#
# Resultado en .\Releases\:
#   - GazeStream-win-Setup.exe        instalador para distribuir (instala por usuario, sin pedir administrador)
#   - GazeStream-win-Portable.zip     versión portable
#   - GazeStream-<versión>-full.nupkg + releases.win.json + RELEASES   (los genera Velopack; no se usan)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# Tiene que ser mayor que la versión instalada en los equipos (la anterior fue 1.0.20) para que la reemplace.
[xml]$csproj = Get-Content (Join-Path $PSScriptRoot 'GazeStream.csproj')
$Version = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $Version) { throw "No se encontró <Version> en GazeStream.csproj." }

$publishDir = Join-Path $PSScriptRoot 'publish'
$releasesDir = Join-Path $PSScriptRoot 'Releases'
$icon = Join-Path $PSScriptRoot 'Resources\Icons\tray.ico'

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw "No se encontró vpk. Instalalo con: dotnet tool install -g vpk --version 1.0.1"
}

Write-Host "1/2 Publicando GazeStream (Release, win-x64, self-contained)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
# Self-contained: el equipo destino no necesita tener .NET instalado. Los SDK nativos son x64.
dotnet publish GazeStream.csproj -c Release -r win-x64 --self-contained -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet publish." }

# Controles mínimos: sin estos archivos los eyetrackers no conectan.
$required = @(
    'GazeStream.exe',
    'sdk\joaco\pid_29D0\aSeeX.dll',
    'sdk\joaco\pid_2CD1\aSeeX.dll',
    'skylelib.dll',
    'tobii_stream_engine.dll',
    'EtApi.dll',
    'config\Configs.dat'
)
foreach ($file in $required) {
    if (-not (Test-Path (Join-Path $publishDir $file))) { throw "Falta en la publicación: $file" }
}

Write-Host "2/2 Empaquetando versión $Version con Velopack..." -ForegroundColor Cyan
$packArgs = @(
    'pack',
    '--packId', 'GazeStream',            # Mismo Id que las instalaciones existentes: así se actualizan en lugar de duplicarse.
    '--packVersion', $Version,
    '--packDir', $publishDir,
    '--mainExe', 'GazeStream.exe',
    '--packTitle', 'GazeStream',
    '--shortcuts', 'Desktop,Startup,StartMenuRoot',   # Los mismos accesos directos que la versión 1.0.20.
    '--framework', 'vcredist143-x64',    # Runtime de Visual C++ 2015-2022 x64, que necesitan los SDK nativos.
    '--outputDir', $releasesDir
)
if (Test-Path $icon) { $packArgs += @('--icon', $icon) }

vpk @packArgs
if ($LASTEXITCODE -ne 0) { throw "Falló vpk pack." }

Write-Host "Listo: $(Join-Path $releasesDir 'GazeStream-win-Setup.exe')" -ForegroundColor Green
