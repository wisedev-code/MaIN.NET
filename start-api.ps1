param(
    [switch]$hard
)

$installRoot = if ($global:MCLI_ROOT) {
    $global:MCLI_ROOT
} else {
    $PSScriptRoot
}

Push-Location $installRoot

function Get-DotNetVersion {
    try {
        $sdkVersions = dotnet --list-sdks
        if ($sdkVersions) {
            # Get the highest installed version
            $highestVersion = ($sdkVersions | ForEach-Object { 
                if ($_ -match "(\d+\.\d+\.\d+)") { 
                    [version]$matches[1] 
                } 
            } | Sort-Object -Descending | Select-Object -First 1)
            return $highestVersion
        }
    } catch {
        return $null
    }
    return $null
}

function Install-DotNet8 {
    Write-Host "Installing .NET 8 SDK..."
    $dotnetInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/89a5ff62-7f4f-4931-896d-2c3e0b3db248/7a97ec4977e245b29d42db9de48c9db1/dotnet-sdk-8.0.100-win-x64.exe"
    $installerPath = "$env:TEMP\dotnet-sdk-8.0.100-win-x64.exe"
    
    try {
        # Download the .NET installer
        Write-Host "Downloading .NET 8 SDK installer..."
        Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $installerPath
        
        # Install .NET silently
        Write-Host "Running installer..."
        Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait
        
        # Cleanup installer
        Remove-Item $installerPath -ErrorAction SilentlyContinue
        
        # Verify installation
        $newVersion = Get-DotNetVersion
        if ($newVersion -and $newVersion.Major -ge 8) {
            Write-Host "Successfully installed .NET SDK version $newVersion"
            return $true
        } else {
            Write-Host "Installation completed but version verification failed."
            return $false
        }
    } catch {
        Write-Host "Error during installation: $_"
        return $false
    }
}

# Check if dotnet is installed and get version
$dotnetVersion = Get-DotNetVersion

if ($null -eq $dotnetVersion) {
    Write-Host "No .NET SDK installation detected."
    if (-not (Install-DotNet8)) {
        Write-Host "Failed to install .NET 8 SDK. Please install it manually and try again."
        exit 1
    }
} else {
    Write-Host "Detected .NET SDK version $dotnetVersion"
    if ($dotnetVersion.Major -lt 8) {
        Write-Host ".NET SDK version $dotnetVersion is below required version 8.0"
        if (-not (Install-DotNet8)) {
            Write-Host "Failed to install .NET 8 SDK. Please install it manually and try again."
            exit 1
        }
    } else {
        Write-Host ".NET SDK version $dotnetVersion meets requirements."
    }
}

# Verify final version
$finalVersion = Get-DotNetVersion
Write-Host "Using .NET SDK version $finalVersion"

# Docker operations
if ($hard) {
    Write-Host "Stopping and removing Docker containers, networks, images, and volumes..."
    docker-compose down -v
} else {
    Write-Host "Stopping and removing Docker containers, networks, and images (volumes retained)..."
    docker-compose down
}

Start-Sleep -Seconds 10

Write-Host "Starting API & Containers in detached mode..."
#Start-Process -FilePath ".\server\MaIN.exe" -WorkingDirectory ".\server" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project ./src/MaIN/MaIN.csproj" -NoNewWindow
Start-Sleep -Seconds 10
docker-compose up -d
Write-Host "Listening on http://localhost:5001 - happy travels"

