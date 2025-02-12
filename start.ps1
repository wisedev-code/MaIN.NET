# Initialize variables
$hard = $false
$models = @()
$noApi = $false
$apiOnly = $false
$noImageGen = $false
$noModels = $false

# Parse command-line arguments
foreach ($arg in $args) {
    if ($arg -eq '--hard') {
        $hard = $true
    } elseif ($arg -like '--models=*') {
        $modelsString = $arg -replace '--models=', ''
        $models = $modelsString -split ','
    } elseif ($arg -eq '--no-api') {
        $noApi = $true
    } elseif ($arg -eq '--api-only') {
        $apiOnly = $true
    } elseif ($arg -eq '--no-image-gen') {
        $noImageGen = $true
    } elseif ($arg -eq '--no-models') {
        $noModels = $true
    }
}

# Run setup tasks unless --api-only is provided
if (-not $apiOnly) {
    # Handle model downloads unless --no-models is specified
    if (-not $noModels) {
        Write-Host "Starting model downloads..."
        & "$PSScriptRoot\download-models.ps1" -models $models
    }

    # Handle Image Generation API unless --no-image-gen is specified
    if (-not $noImageGen) {
        Write-Host "Starting Image Generation API as a background job..."
        Start-Process -FilePath "powershell.exe" -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSScriptRoot\start-image-gen.ps1`"" -NoNewWindow
    }
}

# Start API unless --no-api is specified
if (-not $noApi) {
    Write-Host "Starting main API..."
    & "$PSScriptRoot\start-api.ps1" -hard:$hard
}