# Initialize variables
$hard = $false
$models = @()
$noInfra = $false
$infraOnly = $false
$noImageGen = $false  # New variable for --no-image-gen

# Check for the MODELS_PATH environment variable
$modelsPath = $env:MODELS_PATH
if (-not $modelsPath) {
    Write-Host "MODELS_PATH environment variable is not set."
    $modelsPath = Read-Host "Please provide the local path where models will be stored"
    if (-not (Test-Path $modelsPath)) {
        Write-Host "The provided path does not exist. Creating the directory..."
        New-Item -ItemType Directory -Path $modelsPath | Out-Null
    }
    [System.Environment]::SetEnvironmentVariable("MODELS_PATH", $modelsPath, "Process")
    Write-Host "Using provided path: $modelsPath"
}

# Ensure the models path exists
if (-not (Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath | Out-Null
    Write-Host "Created models directory at $modelsPath"
}

# Check if .NET is installed
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

# Parse command-line arguments
foreach ($arg in $args) {
    if ($arg -eq '--hard') {
        $hard = $true
    } elseif ($arg -like '--models=*') {
        $modelsString = $arg -replace '--models=', ''
        $models = $modelsString -split ','
    } elseif ($arg -eq '--no-infra') {
        $noInfra = $true
    } elseif ($arg -eq '--infra-only') {
        $infraOnly = $true
    } elseif ($arg -eq '--no-image-gen') {
        $noImageGen = $true
    }
}

# Run infrastructure-related tasks only if --no-infra is not provided or if --infra-only is provided
if (-not $noInfra -or $infraOnly) {
    # Determine models to download: from parameter if provided, otherwise from file
    if ($models.Count -gt 0) {
        Write-Host "Using provided models list..."
    } else {
        Write-Host "No models provided as parameter, reading from .models file..."
        $models = Get-Content ".models"
    }

    # Download each model if not already present
    foreach ($model in $models) {
        # Ignore lines that are empty or start with '#' if reading from file
        if ($model.Trim() -eq "" -or $model.Trim().StartsWith("#")) {
            continue
        }

        $model = $model.Trim()
        $modelFileName = "$model.gguf"
        $modelFilePath = Join-Path $modelsPath $modelFileName

        if (-not $modelsMap.ContainsKey($model)) {
            Write-Host "Model '$model' not found in models map. Skipping..."
            continue
        }

        if (Test-Path (Join-Path $modelsPath $modelFileName)) {
            Write-Host "Model '$model' already exists at $modelsPath. Skipping download..."
            continue
        }

        $modelUrl = $modelsMap[$model]

        Write-Host "Downloading model: $model from $modelUrl"
        Invoke-WebRequest -Uri $modelUrl -OutFile $modelFilePath
        Write-Host "Downloaded and saved to $modelFilePath"
    }

    # Continue with other infrastructure tasks
    if (-not $noImageGen) {
        $pythonVersion = "3.9.13"
        $pythonInstallerUrl = "https://www.python.org/ftp/python/$pythonVersion/python-$pythonVersion-amd64.exe"
        $installerPath = "$env:TEMP\python-$pythonVersion-installer.exe"

        # Check if Python 3.9 is already installed
        $python = Get-Command python -ErrorAction SilentlyContinue
        if (-not $python) {
            Write-Host "Downloading Python $pythonVersion..."
            Invoke-WebRequest $pythonInstallerUrl -OutFile $installerPath

            Write-Host "Installing Python $pythonVersion..."
            Start-Process $installerPath -ArgumentList '/quiet InstallAllUsers=1 PrependPath=1 Include_pip=1' -Wait
            Remove-Item $installerPath

            $pythonPath = [System.IO.Path]::Combine("C:\Program Files\Python39", "python.exe")
            if (-not (Test-Path $pythonPath)) {
                $pythonPath = [System.IO.Path]::Combine("C:\Program Files (x86)\Python39", "python.exe")
            }

            if (-not (Test-Path $pythonPath)) {
                Write-Host "Python installation path not found. Please check the installation."
                exit 1
            }

            $currentPath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
            if ($currentPath -notlike "*Python39*") {
                [System.Environment]::SetEnvironmentVariable("Path", "$currentPath;$($pythonPath -replace 'python.exe', '')", "Machine")
            }

            $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
        } else {
            Write-Host "Python is already installed."
        }

        Write-Host "Verifying Python installation..."
        python --version
        pip --version

        Write-Host "Installing dependencies from requirements.txt..."
        pip install --default-timeout=900 -r "./ImageGen/requirements.txt"

        Start-Sleep -Seconds 5

        Write-Host "Running image gen API"
        Start-Process -FilePath "python" -ArgumentList "./ImageGen/main.py" -NoNewWindow -PassThru
        Start-Sleep -Seconds 100
    } else {
        Write-Host "--no-image-gen flag provided, skipping image generation API..."
    }
}

if (-not $infraOnly) {
    if ($hard) {
        Write-Host "Stopping and removing Docker containers, networks, images, and volumes..."
        docker-compose down -v
    } else {
        Write-Host "Stopping and removing Docker containers, networks, and images (volumes retained)..."
        docker-compose down
    }

    Write-Host "Starting Infra & Containers in detached mode..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project ./src/MaIN/MaIN.csproj" -NoNewWindow 
    docker-compose up -d
    Start-Sleep -Seconds 5
    Write-Host "Listening on http://localhost:5001 - happy travels"
}
