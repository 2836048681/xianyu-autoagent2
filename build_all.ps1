param(
    [string]$PythonExe = "python",
    [string]$InnoExe = "D:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe"
)

Set-Location -Path $PSScriptRoot

if (Test-Path $InnoExe -PathType Container) {
    $InnoExe = Join-Path $InnoExe "ISCC.exe"
}
if (-not (Test-Path $InnoExe)) {
    throw "ISCC.exe not found: $InnoExe"
}

Write-Host "Step 1/2: Build desktop app and worker"
& $PSScriptRoot\build_desktop.ps1 -PythonExe $PythonExe
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Step 2/2: Build installer via Inno Setup"
& $InnoExe "installer\\inno\\setup.iss"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done."

