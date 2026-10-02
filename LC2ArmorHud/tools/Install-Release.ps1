param(
    [string]$GameRoot = 'D:\SteamLibrary\steamapps\common\Lost Castle 2',
    [switch]$WhatIf
)

$source = Join-Path $PSScriptRoot '..\bin\Release\net6.0\LC2ArmorHud.dll'
$targetDirectory = Join-Path $GameRoot 'BepInEx\plugins\LC2ArmorHud'
$target = Join-Path $targetDirectory 'LC2ArmorHud.dll'

if (Get-Process LostCastle2 -ErrorAction SilentlyContinue) {
    throw 'Close Lost Castle 2 before installing the HUD.'
}

if (-not (Test-Path -LiteralPath $source)) {
    throw "Release DLL not found: $source"
}

if ($WhatIf) {
    Write-Output "Would install $source to $target"
    exit 0
}

New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
if (Test-Path -LiteralPath $target) {
    $backupName = 'LC2ArmorHud.{0:yyyyMMdd-HHmmss}.dll.bak' -f (Get-Date)
    $backup = Join-Path $targetDirectory $backupName
    Move-Item -LiteralPath $target -Destination $backup
    Write-Output "Backed up $target to $backup"
}

Copy-Item -LiteralPath $source -Destination $target
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($sourceHash -ne $targetHash) {
    throw 'Installed DLL hash mismatch.'
}

Write-Output "Installed $target"
Write-Output "SHA256 $targetHash"
