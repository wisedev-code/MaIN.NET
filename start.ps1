# Initialize variables
$hard = $false
$models = @()
$noInfra = $false
$infraOnly = $false

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

    Write-Host "Running image gen API"
    Start-Process -FilePath "python" -ArgumentList "./ImageGen/main.py" -NoNewWindow -PassThru
    Start-Sleep -Seconds 100
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
