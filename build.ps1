param(
    [string]$BeatSaberDir
)

$ErrorActionPreference = "Stop"

# If BeatSaberDir was not specified, try the default Steam installation path.
if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    $DefaultSteamDir = "C:\Program Files (x86)\Steam\steamapps\common\Beat Saber"

    if (Test-Path $DefaultSteamDir) {
        $BeatSaberDir = $DefaultSteamDir
    }
    else {
        Write-Host "Beat Saber installation directory was not specified." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Usage:"
        Write-Host '  .\build.ps1 -BeatSaberDir "D:\Path\To\Beat Saber"'
        Write-Host ""
        Write-Host "The default Steam installation path was also not found:"
        Write-Host "  $DefaultSteamDir"
        exit 1
    }
}

Write-Host "ToyanBomb v1.0.0 for Beat Saber 1.40.8 Stable build"
Write-Host "Beat Saber: $BeatSaberDir"
Write-Host ""

$MainDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\Main.dll"
$NewtonsoftJsonDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\Newtonsoft.Json.dll"
$BeatmapCoreDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\BeatmapCore.dll"

$UnityCoreDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.CoreModule.dll"
$UnityPhysicsDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.PhysicsModule.dll"
$UnityParticlesDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.ParticleSystemModule.dll"
$TmpDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\Unity.TextMeshPro.dll"
$UnityUIDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.UI.dll"
$UnityWebRequestDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.UnityWebRequestModule.dll"
$UnityWebRequestTextureDll = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\UnityEngine.UnityWebRequestTextureModule.dll"
$ChatPlexDll = Join-Path $BeatSaberDir "Plugins\ChatPlexSDK_BS.dll"
$BSMLDll = Join-Path $BeatSaberDir "Plugins\BSML.dll"

$IPA1 = Join-Path $BeatSaberDir "Beat Saber_Data\Managed\IPA.Loader.dll"
$IPA2 = Join-Path $BeatSaberDir "IPA\Data\Managed\IPA.Loader.dll"

$Harmony1 = Join-Path $BeatSaberDir "Libs\0Harmony.dll"
$Harmony2 = Join-Path $BeatSaberDir "IPA\Libs\0Harmony.dll"

$IPA = if (Test-Path $IPA1) { $IPA1 } elseif (Test-Path $IPA2) { $IPA2 } else { $null }
$Harmony = if (Test-Path $Harmony1) { $Harmony1 } elseif (Test-Path $Harmony2) { $Harmony2 } else { $null }

$missing = @()

if (-not (Test-Path $MainDll)) { $missing += $MainDll }
if (-not (Test-Path $NewtonsoftJsonDll)) { $missing += $NewtonsoftJsonDll }
if (-not (Test-Path $BeatmapCoreDll)) { $missing += $BeatmapCoreDll }
if (-not (Test-Path $UnityCoreDll)) { $missing += $UnityCoreDll }
if (-not (Test-Path $UnityPhysicsDll)) { $missing += $UnityPhysicsDll }
if (-not (Test-Path $UnityParticlesDll)) { $missing += $UnityParticlesDll }
if (-not (Test-Path $TmpDll)) { $missing += $TmpDll }
if (-not (Test-Path $UnityUIDll)) { $missing += $UnityUIDll }
if (-not (Test-Path $UnityWebRequestDll)) { $missing += $UnityWebRequestDll }
if (-not (Test-Path $UnityWebRequestTextureDll)) { $missing += $UnityWebRequestTextureDll }
if (-not (Test-Path $ChatPlexDll)) { $missing += $ChatPlexDll }
if (-not (Test-Path $BSMLDll)) { $missing += $BSMLDll }
if (-not $IPA) { $missing += "IPA.Loader.dll (Managed or IPA\Data\Managed)" }
if (-not $Harmony) { $missing += "0Harmony.dll (Libs or IPA\Libs)" }

if ($missing.Count -gt 0) {
    Write-Host "Missing required files:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Build stopped."
    exit 1
}

Write-Host "Found dependencies:" -ForegroundColor Cyan
Write-Host "  Main.dll          : $MainDll"
Write-Host "  Newtonsoft.Json   : $NewtonsoftJsonDll"
Write-Host "  BeatmapCore       : $BeatmapCoreDll"
Write-Host "  Unity Physics     : $UnityPhysicsDll"
Write-Host "  Unity Particles   : $UnityParticlesDll"
Write-Host "  TextMeshPro       : $TmpDll"
Write-Host "  Unity UI          : $UnityUIDll"
Write-Host "  Unity WebRequest  : $UnityWebRequestDll"
Write-Host "  WebRequest Texture: $UnityWebRequestTextureDll"
Write-Host "  IPA.Loader.dll    : $IPA"
Write-Host "  0Harmony.dll      : $Harmony"
Write-Host "  ChatPlexSDK_BS.dll: $ChatPlexDll"
Write-Host "  BSML              : $BSMLDll"
Write-Host ""

$ProjectPath = Join-Path $PSScriptRoot "ToyanBomb\ToyanBomb.csproj"

dotnet build $ProjectPath `
    -c Release `
    -p:BeatSaberDir="$BeatSaberDir" `
    -p:BeatmapCorePath="$BeatmapCoreDll"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$dll = Join-Path $PSScriptRoot "ToyanBomb\bin\Release\net472\ToyanBomb.dll"

Write-Host ""
Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host "DLL: $dll"
Write-Host ""
Write-Host "Next:"
Write-Host "  Copy ONLY ToyanBomb.dll to $BeatSaberDir\Plugins\"