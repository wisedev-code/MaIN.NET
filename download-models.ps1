param(
    [Parameter(Position=0)]
    [string]$singleModel,
    [string[]]$models = @(),
    [string]$modelsPath = $env:MaIN_ModelsPath
)

# Initialize models path if not set
if (-not $modelsPath) {
    Write-Host "Models path environment variable is not set."
    $modelsPath = Read-Host "Please provide the local path where models will be stored"

    if (-not (Test-Path $modelsPath)) {
        Write-Host "The provided path does not exist. Creating the directory..."
        New-Item -ItemType Directory -Path $modelsPath | Out-Null
    }

    # Set environment variable for the user (persist)
    [System.Environment]::SetEnvironmentVariable("MaIN_ModelsPath", $modelsPath, "User")

    # Reload the environment variable in the current session
    $env:MaIN_ModelsPath = [System.Environment]::GetEnvironmentVariable("MaIN_ModelsPath", "User")

    Write-Host "MODELS_PATH set to: $env:MaIN_ModelsPath"
} else {
    Write-Host "MODELS_PATH is already set: $modelsPath"
}

# Ensure the models path exists
if (-not (Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath | Out-Null
    Write-Host "Created models directory at $modelsPath"
}

# Function to download a single model
function Download-Model {
    param(
        [string]$model,
        [string]$modelsPath
    )

    $model = $model.Trim()
    $modelFileName = "$model.gguf"
    $modelFilePath = Join-Path $modelsPath $modelFileName

    if (Test-Path (Join-Path $modelsPath $modelFileName)) {
        Write-Host "Model '$model' already exists at $modelsPath. Skipping download..."
        return
    }

    $modelUrl = $modelsMap[$model]
    if (-not $modelUrl) {
        Write-Host "Error: Model '$model' not found in models map. Skipping..."
        return
    }

    Write-Host "Downloading model: $model from $modelUrl"
    Invoke-WebRequest -Uri $modelUrl -OutFile $modelFilePath
    Write-Host "Downloaded and saved to $modelFilePath"
}

# Handle single model download if specified
if ($singleModel) {
    Write-Host "Downloading single model: $singleModel"
    Download-Model -model $singleModel -modelsPath $modelsPath
    return
}

# Otherwise, process multiple models from parameter or file
if ($models.Count -gt 0) {
    Write-Host "Using provided models list..."
} else {
    Write-Host "No models provided as parameter, reading from .models file..."
    if (Test-Path ".models") {
        $models = Get-Content ".models"
    } else {
        Write-Host "Error: .models file not found and no models specified."
        return
    }
}

# Download each model in the list
foreach ($model in $models) {
    # Ignore lines that are empty or start with '#' if reading from file
    if ($model.Trim() -eq "" -or $model.Trim().StartsWith("#")) {
        continue
    }

    Download-Model -model $model -modelsPath $modelsPath
}