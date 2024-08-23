#!/bin/bash

# Initialize variables
HARD=false
MODELS=()

# Function to parse command-line arguments
parse_args() {
  while [[ "$#" -gt 0 ]]; do
    case $1 in
      --hard) HARD=true ;;
      --models=*) IFS=',' read -r -a MODELS <<< "${1#*=}" ;;
      *) echo "Unknown parameter passed: $1"; exit 1 ;;
    esac
    shift
  done
}

# Parse the command-line arguments
parse_args "$@"

# Stop and remove Docker containers, networks, images (and volumes if --hard is provided)
if [ "$HARD" = true ]; then
  echo "Stopping and removing Docker containers, networks, images, and volumes..."
  docker-compose down -v
else
  echo "Stopping and removing Docker containers, networks, and images (volumes retained)..."
  docker-compose down
fi

# Start Docker containers in detached mode
echo "Starting Docker containers in detached mode..."
docker-compose up -d

# Wait for 5 seconds to ensure the containers are up and running
echo "Waiting for 5 seconds to ensure the containers are up and running..."
sleep 5

echo "Running the Ollama serve..."
# You may need to uncomment and adjust the following line depending on your setup
# nohup ollama serve &

sleep 15

# Determine models to pull: from parameter if provided, otherwise from file
if [ ${#MODELS[@]} -gt 0 ]; then
  echo "Using provided models list..."
else
  echo "No models provided as parameter, reading from .models file..."
  if [ -f ".models" ]; then
    # Read the .models file into an array, ignoring comments and empty lines
    while IFS= read -r line || [ -n "$line" ]; do
      [[ "$line" =~ ^#.*$ ]] || [[ -z "$line" ]] && continue
      MODELS+=("$line")
    done < ".models"
  else
    echo ".models file not found."
    exit 1
  fi
fi

# Pull each model
for model in "${MODELS[@]}"; do
  echo "Pulling model: $model"
  ollama pull "$model"
done

# Wait for all background jobs to complete
echo "Listening on http://localhost:5001 - happy travels"
wait