# Initialize variables
$hard = $false
$models = @()
$noInfra = $false
$infraOnly = $false
$noImageGen = $false  # New variable for --no-image-gen

# Manually parse the command-line arguments for double-dash parameters
foreach ($arg in $args) {
    if ($arg -eq '--hard') {
        $hard = $true
    } elseif ($arg -like '--models=*') {
        # Extract the models from the argument
        $modelsString = $arg -replace '--models=', ''
        # Split the models string into an array, assuming comma-separated models
        $models = $modelsString -split ','
    } elseif ($arg -eq '--no-infra') {
        $noInfra = $true
    } elseif ($arg -eq '--infra-only') {
        $infraOnly = $true
    } elseif ($arg -eq '--no-image-gen') {
        $noImageGen = $true  # Set the flag for --no-image-gen
    }
}

# Run infrastructure-related tasks only if --no-infra is not provided or if --infra-only is provided
if (-not $noInfra -or $infraOnly) {
    # Determine models to pull: from parameter if provided, otherwise from file
    if ($models.Count -gt 0) {
        Write-Host "Using provided models list..."
    } else {
        Write-Host "No models provided as parameter, reading from .models file..."
        $models = Get-Content ".models"
    }

    # Pull each model, ignoring comments if reading from file
    foreach ($model in $models) {
        # Ignore lines that are empty or start with '#' if reading from file
        if ($model.Trim() -eq "" -or $model.Trim().StartsWith("#")) {
            continue
        }

        Write-Host "Pulling model: $model"
        ollama pull $model
    }

    Start-Sleep -Seconds 10
    Write-Host "Running the Ollama serve."
    Start-Sleep -Seconds 5

    # Install Python and run Image Gen API if --no-image-gen is not provided
    if (-not $noImageGen) {
        $pythonVersion = "3.9.13"
        $pythonInstallerUrl = "https://www.python.org/ftp/python/$pythonVersion/python-$pythonVersion-amd64.exe"
        $installerPath = "$env:TEMP\python-$pythonVersion-installer.exe"

        # Check if Python 3.9 is already installed
        $python = Get-Command python -ErrorAction SilentlyContinue
        if (-not $python) {
            Write-Host "Downloading Python $pythonVersion..."

            # Download the Python installer
            Invoke-WebRequest $pythonInstallerUrl -OutFile $installerPath

            # Install Python 3.9 silently, add to PATH, and ensure pip is installed
            Write-Host "Installing Python $pythonVersion..."
            Start-Process $installerPath -ArgumentList '/quiet InstallAllUsers=1 PrependPath=1 Include_pip=1' -Wait

            # Clean up the installer
            Remove-Item $installerPath

            # Manually add Python to PATH if not automatically set
            $pythonPath = [System.IO.Path]::Combine("C:\Program Files\Python39", "python.exe")
            if (-not (Test-Path $pythonPath)) {
                $pythonPath = [System.IO.Path]::Combine("C:\Program Files (x86)\Python39", "python.exe")
            }

            if (-not (Test-Path $pythonPath)) {
                Write-Host "Python installation path not found. Please check the installation."
                exit 1
            }

            # Check if the path is already in the PATH environment variable
            $currentPath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
            if ($currentPath -notlike "*Python39*") {
                Write-Host "Adding Python to the PATH manually..."
                [System.Environment]::SetEnvironmentVariable("Path", "$currentPath;$($pythonPath -replace 'python.exe', '')", "Machine")
            }

            # Refresh PATH environment variable for the current session
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
        } else {
            Write-Host "Python is already installed."
        }

        # Verify Python and pip installation
        Write-Host "Verifying Python installation..."
        python --version
        pip --version

        # Install packages from requirements.txt
        Write-Host "Installing dependencies from requirements.txt..."
        pip install --default-timeout=900 -r "./ImageGen/requirements.txt"

        Start-Sleep -Seconds 5

        # Conditionally run the image generation API based on --no-image-gen
        Write-Host "Running image gen API"
        Start-Process -FilePath "python" -ArgumentList "./ImageGen/main.py" -NoNewWindow -PassThru
        Start-Sleep -Seconds 100
    } else {
        Write-Host "--no-image-gen flag provided, skipping image generation API..."
    }
}

# Run Docker-related tasks only if --infra-only is not provided
if (-not $infraOnly) {
    # Stop and remove Docker containers, networks, images (and volumes if --hard is provided)
    if ($hard) {
        Write-Host "Stopping and removing Docker containers, networks, images, and volumes..."
        docker-compose down -v
    } else {
        Write-Host "Stopping and removing Docker containers, networks, and images (volumes retained)..."
        docker-compose down
    }

    # Start Docker containers in detached mode
    Write-Host "Starting Docker containers in detached mode..."
    docker-compose up -d

    # Wait for 5 seconds to ensure the containers are up and running
    Write-Host "Waiting for 5 seconds to ensure the containers are up and running..."
    Start-Sleep -Seconds 5

    Write-Host "
MMMMMMMM               MMMMMMMM                       AAA                       IIIIIIIIII        NNNNNNNN        NNNNNNNN
M:::::::M             M:::::::M                      A:::A                      I::::::::I        N:::::::N       N::::::N
M::::::::M           M::::::::M                     A:::::A                     I::::::::I        N::::::::N      N::::::N
M:::::::::M         M:::::::::M                    A:::::::A                    II::::::II        N:::::::::N     N::::::N
M::::::::::M       M::::::::::M                   A:::::::::A                     I::::I          N::::::::::N    N::::::N
M:::::::::::M     M:::::::::::M                  A:::::A:::::A                    I::::I          N:::::::::::N   N::::::N
M:::::::M::::M   M::::M:::::::M                 A:::::A A:::::A                   I::::I          N:::::::N::::N  N::::::N
M::::::M M::::M M::::M M::::::M                A:::::A   A:::::A                  I::::I          N::::::N N::::N N::::::N
M::::::M  M::::M::::M  M::::::M               A:::::A     A:::::A                 I::::I          N::::::N  N::::N:::::::N
M::::::M   M:::::::M   M::::::M              A:::::AAAAAAAAA:::::A                I::::I          N::::::N   N:::::::::::N
M::::::M    M:::::M    M::::::M             A:::::::::::::::::::::A               I::::I          N::::::N    N::::::::::N
M::::::M     MMMMM     M::::::M            A:::::AAAAAAAAAAAAA:::::A              I::::I          N::::::N     N:::::::::N
M::::::M               M::::::M           A:::::A             A:::::A           II::::::II        N::::::N      N::::::::N
M::::::M               M::::::M ......   A:::::A               A:::::A   ...... I::::::::I ...... N::::::N       N:::::::N
M::::::M               M::::::M .::::.  A:::::A                 A:::::A  .::::. I::::::::I .::::. N::::::N        N::::::N
MMMMMMMM               MMMMMMMM ...... AAAAAAA                   AAAAAAA ...... IIIIIIIIII ...... NNNNNNNN         NNNNNNN
"

    # Wait for all background jobs to complete
    Write-Host "Listening on http://localhost:5001 - happy travels"
}
