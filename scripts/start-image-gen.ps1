# Image Generation API Setup Script
$installRoot = if ($global:MCLI_ROOT) {
    $global:MCLI_ROOT
} else {
    $PSScriptRoot
}

Push-Location $installRoot
# Python configuration
$pythonVersion = "3.9.13"
$pythonInstallerUrl = "https://www.python.org/ftp/python/$pythonVersion/python-$pythonVersion-amd64.exe"
$installerPath = "$env:TEMP\python-$pythonVersion-installer.exe"

# Check if Python 3.9 is already installed
$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    Write-Host "Downloading Python $pythonVersion..."
    try {
        Invoke-WebRequest $pythonInstallerUrl -OutFile $installerPath
    }
    catch {
        Write-Host "Failed to download Python installer. Error: $_"
        exit 1
    }

    Write-Host "Installing Python $pythonVersion..."
    $installProcess = Start-Process $installerPath -ArgumentList '/quiet InstallAllUsers=1 PrependPath=1 Include_pip=1' -PassThru -Wait
    Remove-Item $installerPath

    if ($installProcess.ExitCode -ne 0) {
        Write-Host "Python installation failed with exit code $($installProcess.ExitCode)"
        exit 1
    }

    # Refresh environment variables
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
}
else {
    Write-Host "Python is already installed."
}

# Verify Python installation
Write-Host "Verifying Python installation..."
python --version
pip --version

# Install dependencies
Write-Host "Installing dependencies from requirements.txt..."
try {
    pip install --default-timeout=900 -r "./ImageGen/requirements.txt"
}
catch {
    Write-Host "Failed to install dependencies. Error: $_"
    exit 1
}

# Start the API
Write-Host "Starting Image Generation API..."
Start-Process -FilePath "python" -ArgumentList "./ImageGen/main.py" -NoNewWindow -PassThru

Write-Host "Image Generation API is running. Press Ctrl+C to stop."
try {
    # Keep the script running until interrupted
    while ($true) {
        Start-Sleep -Seconds 60
    }
}
finally {
    Write-Host "Stopping Image Generation API..."
}