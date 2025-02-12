param(
    [switch]$hard
)

$installRoot = if ($global:MCLI_ROOT) {
    $global:MCLI_ROOT
} else {
    $PSScriptRoot
}

Push-Location $installRoot

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host ".NET is not installed. Installing .NET 8..."

    # URL for .NET 8 installer
    $dotnetInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/89a5ff62-7f4f-4931-896d-2c3e0b3db248/7a97ec4977e245b29d42db9de48c9db1/dotnet-sdk-8.0.100-win-x64.exe"
    $installerPath = "$env:TEMP\dotnet-sdk-8.0.100-win-x64.exe"

    # Download the .NET installer
    Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $installerPath
    Write-Host "Installing .NET 8 SDK..."

    # Install .NET silently
    Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait

    # Cleanup installer
    Remove-Item $installerPath

    # Verify installation
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Host "Failed to install .NET 8. Please install it manually and try again."
        exit 1
    }
} else {
    Write-Host ".NET is already installed."
}

# Ensure the installed version is .NET 8
$dotnetVersion = &dotnet --version
if ($dotnetVersion -notlike "8.*") {
    Write-Host ".NET version $dotnetVersion detected. Installing .NET 8..."

    # URL for .NET 8 installer
    $dotnetInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/89a5ff62-7f4f-4931-896d-2c3e0b3db248/7a97ec4977e245b29d42db9de48c9db1/dotnet-sdk-8.0.100-win-x64.exe"
    $installerPath = "$env:TEMP\dotnet-sdk-8.0.100-win-x64.exe"

    # Download the .NET installer
    Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $installerPath
    Write-Host "Installing .NET 8 SDK..."

    # Install .NET silently
    Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait

    # Cleanup installer
    Remove-Item $installerPath

    # Verify installation
    $dotnetVersion = &dotnet --version
    if ($dotnetVersion -notlike "8.*") {
        Write-Host "Failed to install .NET 8. Please install it manually and try again."
        exit 1
    }
}

Write-Host "Using .NET version $dotnetVersion"


if ($hard) {
    Write-Host "Stopping and removing Docker containers, networks, images, and volumes..."
    docker-compose down -v
} else {
    Write-Host "Stopping and removing Docker containers, networks, and images (volumes retained)..."
    docker-compose down
}

Write-Host "Starting API & Containers in detached mode..."
#Start-Process -FilePath ".\server\MaIN.exe" -WorkingDirectory ".\server" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project ./src/MaIN/MaIN.csproj" -NoNewWindow 
Start-Sleep -Seconds 10
docker-compose up -d
Write-Host "Listening on http://localhost:5001 - happy travels"