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
    # Set the MODELS_PATH environment variable for the current session
    [System.Environment]::SetEnvironmentVariable("MODELS_PATH", $modelsPath, "Process")
    Write-Host "Using provided path: $modelsPath"
}

# Ensure the models path exists
if (-not (Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath | Out-Null
    Write-Host "Created models directory at $modelsPath"
}

# Read the models map file for name-to-URL mapping
$modelsMapFile = "$PSScriptRoot\models_map.txt"
if (-not (Test-Path $modelsMapFile)) {
    Write-Host "Models map file not found at $modelsMapFile. Please provide a valid file."
    exit 1
}

# Load models map as a hashtable
$modelsMap = @{}
Get-Content $modelsMapFile | ForEach-Object {
    # Match key=value pairs and trim spaces
    if ($_ -match '^\s*(\S+)\s*=\s*(\S+)\s*$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        $modelsMap[$key] = $value
    }
}

# Initialize variables
$hard = $false
$models = @()
$noInfra = $false
$infraOnly = $false
$noImageGen = $false

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

if (-not (Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath | Out-Null
    Write-Host "Created models directory at $modelsPath"
}

# Read models map file
$modelsMapFile = "$PSScriptRoot\models_map.txt"
if (-not (Test-Path $modelsMapFile)) {
    Write-Host "Models map file not found at $modelsMapFile. Please provide a valid file."
    exit 1
}

# Load models map as a hashtable (simple CSV parsing)
$modelsMap = @{}
Get-Content $modelsMapFile | ForEach-Object {
    # Skip empty lines or comments
    if ($_ -match '^\s*$' -or $_ -match '^\s*#') {
        return
    }

    # Split by comma and map key-value pair
    $parts = $_ -split ','
    if ($parts.Length -eq 2) {
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        $modelsMap[$key] = $value
    } else {
        Write-Host "Skipping invalid entry in models_map.txt: $_"
    }
}

# DEBUG: Print loaded models map for verification
Write-Host "Loaded models map:"
$modelsMap.GetEnumerator() | ForEach-Object {
    Write-Host "$($_.Key) = $($_.Value)"
}
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
