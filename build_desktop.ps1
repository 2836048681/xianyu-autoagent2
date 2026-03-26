param(
    [string]$PythonExe = "python"
)

Set-Location -Path $PSScriptRoot

$desktopProject = Join-Path $PSScriptRoot "desktop\XianyuDesktopApp\XianyuDesktopApp.csproj"
$workerOutDir = Join-Path $PSScriptRoot "desktop\publish\worker"
$appOutDir = Join-Path $PSScriptRoot "desktop\publish\app"
$desktopPublishDir = Join-Path $appOutDir "XianyuDesktopApp"

Write-Host "Step 1/4: Build Python worker"
& $PSScriptRoot\build_worker.ps1 -PythonExe $PythonExe
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path $workerOutDir | Out-Null
Copy-Item -Force (Join-Path $PSScriptRoot "dist\XianyuWorker.exe") (Join-Path $workerOutDir "XianyuWorker.exe")

Write-Host "Step 2/4: Publish WinUI desktop app"
$env:PATH = "C:\Program Files\dotnet;$env:PATH"
dotnet publish $desktopProject -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:CodeAnalysisVSSku=Community -o $desktopPublishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Step 3/4: Copy worker and offline assets"

$compiledXamlRoot = Get-ChildItem -Path (Join-Path $PSScriptRoot "desktop\XianyuDesktopApp\bin\x64\Release") -Recurse -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "MainWindow.xbf") } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $compiledXamlRoot) {
    throw "Could not locate compiled WinUI XAML resources (MainWindow.xbf)."
}

$xamlResourceDirs = @("Controls", "Services", "Styles")
foreach ($dirName in $xamlResourceDirs) {
    $sourceDir = Join-Path $compiledXamlRoot.FullName $dirName
    $targetDir = Join-Path $desktopPublishDir $dirName
    if (Test-Path $sourceDir) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
        Copy-Item -Recurse -Force (Join-Path $sourceDir "*") $targetDir
    }
}
foreach ($resourceName in @("App.xbf", "MainWindow.xbf", "XianyuDesktopApp.pri")) {
    $sourceResource = Join-Path $compiledXamlRoot.FullName $resourceName
    if (Test-Path $sourceResource) {
        Copy-Item -Force $sourceResource (Join-Path $desktopPublishDir $resourceName)
    }
}
New-Item -ItemType Directory -Force -Path (Join-Path $desktopPublishDir "worker") | Out-Null
Copy-Item -Force (Join-Path $workerOutDir "XianyuWorker.exe") (Join-Path $desktopPublishDir "worker\XianyuWorker.exe")
Copy-Item -Recurse -Force (Join-Path $PSScriptRoot "chrome") (Join-Path $desktopPublishDir "chrome")
Copy-Item -Recurse -Force (Join-Path $PSScriptRoot "chromedriver") (Join-Path $desktopPublishDir "chromedriver")
Copy-Item -Recurse -Force (Join-Path $PSScriptRoot "prompts") (Join-Path $desktopPublishDir "prompts")

Write-Host "Step 4/4: Desktop publish complete"
Write-Host "Output: $desktopPublishDir"
