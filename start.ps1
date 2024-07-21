# Stop and remove Docker containers, networks, images, and volumes
Write-Host "Stopping and removing Docker containers, networks, images, and volumes..."
docker-compose down -v
# Start Docker containers in detached mode
Write-Host "Starting Docker containers in detached mode..."
docker-compose up -d

# Wait for 5 seconds to ensure the containers are up and running
Write-Host "Waiting for 5 seconds to ensure the containers are up and running..."
Start-Sleep -Seconds 5


Write-Host "Running the Ollama serve."
Start-Job -ScriptBlock { ollama serve }

Start-Sleep -Seconds 15

# Read the .models file and pull each model, ignoring comments
Write-Host "Reading .models file and pulling models..."
$models = Get-Content ".models"
foreach ($model in $models) {
    # Ignore lines that are empty or start with '#'
    if ($model.Trim() -eq "" -or $model.Trim().StartsWith("#")) {
        continue
    }
    Write-Host "Pulling model: $model"
    ollama pull $model
}

Start-Sleep -Seconds 5
# Wait for all background jobs to complete
Write-Host "Listening on http://localhost:5001 - happy travels"
Get-Job | Wait-Job
