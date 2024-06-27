docker compose up -d
sleep 5
docker compose exec ollama sh /root/.ollama/scripts/pull_gemma_2b.sh