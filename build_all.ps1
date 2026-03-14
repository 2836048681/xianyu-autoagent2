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

Write-Host "Step 1/2: Build v1.exe via PyInstaller"
& $PythonExe -m PyInstaller --onefile --noconsole --name v1 --add-data "prompts;prompts" --add-data "chrome;chrome" --add-data "chromedriver;chromedriver" --hidden-import tkinter --hidden-import tkinter.ttk --hidden-import selenium gui_app.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Step 2/2: Build installer via Inno Setup"
& $InnoExe "installer\\inno\\setup.iss"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done."
