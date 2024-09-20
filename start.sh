#!/bin/bash

hard=false
models=()
noInfra=false
infraOnly=false
noImageGen=false  # New variable for --no-image-gen

# Manually parse the command-line arguments for double-dash parameters
for arg in "$@"; do
    case $arg in
        --hard)
            hard=true
            ;;
        --models=*)
            modelsString="${arg#*=}"
            # Split the models string into an array, assuming comma-separated models
            IFS=',' read -ra models <<< "$modelsString"
            ;;
        --no-infra)
            noInfra=true
            ;;
        --infra-only)
            infraOnly=true
            ;;
        --no-image-gen)
            noImageGen=true
            ;;
    esac
done

# Run infrastructure-related tasks only if --no-infra is not provided or if --infra-only is provided
if [[ $noInfra == false || $infraOnly == true ]]; then
    # Determine models to pull: from parameter if provided, otherwise from file
    if [[ ${#models[@]} -gt 0 ]]; then
        echo "Using provided models list..."
    else
        echo "No models provided as parameter, reading from .models file..."
        # Read models from the .models file, ignoring comments and blank lines
        models=($(grep -v '^#' .models | grep -v '^$'))
    fi

    # Pull each model, ignoring comments if reading from file
    for model in "${models[@]}"; do
        echo "Pulling model: $model"
        ollama pull "$model"
    done

    sleep 10
    echo "Running the Ollama serve."
    sleep 5

    # Install Python and run Image Gen API if --no-image-gen is not provided
    if [[ $noImageGen == false ]]; then
pythonVersion="3.9.13"
pythonInstallerUrl="https://www.python.org/ftp/python/$pythonVersion/Python-$pythonVersion.tgz"

# Check if Python 3.9 is already installed
if ! command -v python3.9 &> /dev/null || [[ $(python3.9 --version | awk '{print $2}') != $pythonVersion ]]; then
    echo "Downloading Python $pythonVersion..."
    wget "$pythonInstallerUrl" -O /tmp/Python-$pythonVersion.tgz
    tar -xzf /tmp/Python-$pythonVersion.tgz -C /tmp

    cd /tmp/Python-$pythonVersion

    # Ensure necessary build tools are installed
    sudo apt update
    sudo apt install -y build-essential zlib1g-dev libffi-dev libssl-dev libreadline-dev libbz2-dev libsqlite3-dev wget curl llvm libncurses5-dev libncursesw5-dev xz-utils tk-dev libxml2-dev libxmlsec1-dev liblzma-dev

    # Configure and install
    ./configure --enable-optimizations
    make -j$(nproc)
    sudo make altinstall

    cd -

    # Clean up
    rm -rf /tmp/Python-$pythonVersion /tmp/Python-$pythonVersion.tgz

    # Manually check for and add Python3.9 to PATH if it's not already there
    pythonPath=$(command -v python3.9)
    if [[ -z "$pythonPath" ]]; then
        echo "Python installation failed. Exiting."
        exit 1
    fi

    # Adding Python to PATH if not already in it
    currentPath=$(echo "$PATH")
    pythonBinDir=$(dirname "$pythonPath")

    if [[ ! "$currentPath" =~ "$pythonBinDir" ]]; then
        echo "Adding Python 3.9 to the PATH..."
        echo "export PATH=\"$pythonBinDir:\$PATH\"" >> ~/.bashrc
        export PATH="$pythonBinDir:$PATH"
    fi
else
    echo "Python is already installed."
fi

# Verify Python and pip installation
echo "Verifying Python installation..."
python3.9 --version
if ! command -v pip3.9 &> /dev/null; then
    echo "Installing pip for Python 3.9..."
    curl https://bootstrap.pypa.io/get-pip.py -o /tmp/get-pip.py
    python3.9 /tmp/get-pip.py
    rm /tmp/get-pip.py
fi

echo "Verifying pip installation..."
pip3.9 --version

# Install packages from requirements.txt
echo "Installing dependencies from requirements.txt..."
pip3.9 install --user --default-timeout=900 -r "./ImageGen/requirements.txt"

        sleep 5

        # Conditionally run the image generation API based on --no-image-gen
        echo "Running image gen API"
        nohup python3.9 ./ImageGen/main.py &
        sleep 100
    else
        echo "--no-image-gen flag provided, skipping image generation API..."
    fi
fi

# Run Docker-related tasks only if --infra-only is not provided
if [[ $infraOnly == false ]]; then
    # Stop and remove Docker containers, networks, images (and volumes if --hard is provided)
    if [[ $hard == true ]]; then
        echo "Stopping and removing Docker containers, networks, images, and volumes..."
        docker compose down -v
    else
        echo "Stopping and removing Docker containers, networks, and images (volumes retained)..."
        docker compose down
    fi

    # Start Docker containers in detached mode
    echo "Starting Docker containers in detached mode..."
    docker compose up -d

    # Wait for 5 seconds to ensure the containers are up and running
    echo "Waiting for 5 seconds to ensure the containers are up and running..."
    sleep 5

    echo "
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
    echo "Listening on http://localhost:5001 - happy travels"
fi
