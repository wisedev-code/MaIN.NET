#!/bin/bash
set -e

# Start Ollama in the background, binding to all interfaces so port 11434
# is reachable from the host for model management (ollama pull, etc.)
OLLAMA_HOST=0.0.0.0:11434 ollama serve &
OLLAMA_PID=$!

# Forward SIGTERM/SIGINT to both child processes
_term() {
  kill "$OLLAMA_PID" 2>/dev/null
  kill "$INFERPAGE_PID" 2>/dev/null
}
trap _term SIGTERM SIGINT

# Wait until Ollama's HTTP API is accepting connections
echo "[entrypoint] Waiting for Ollama to be ready..."
until bash -c 'printf "" 2>/dev/null >> /dev/tcp/localhost/11434' 2>/dev/null; do
  sleep 1
done
echo "[entrypoint] Ollama is ready."

# Start InferPage in the background so we can wait on both PIDs
dotnet MaIN.InferPage.dll &
INFERPAGE_PID=$!

# Block until either process exits, then shut down the other
wait -n "$OLLAMA_PID" "$INFERPAGE_PID"
EXIT_CODE=$?
kill "$OLLAMA_PID" "$INFERPAGE_PID" 2>/dev/null
exit $EXIT_CODE
