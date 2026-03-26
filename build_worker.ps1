param(
    [string]$PythonExe = "python"
)

Set-Location -Path $PSScriptRoot

$cmd = @(
    $PythonExe,
    "-m",
    "PyInstaller",
    "--clean",
    "--onefile",
    "--console",
    "--name",
    "XianyuWorker",
    "--add-data",
    "prompts;prompts",
    "--add-data",
    "chrome;chrome",
    "--add-data",
    "chromedriver;chromedriver",
    "--collect-all",
    "playwright",
    "worker_cli.py"
)

Write-Host "Running: $($cmd -join ' ')"
& $cmd[0] $cmd[1..($cmd.Length-1)]
