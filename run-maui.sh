#!/usr/bin/env bash
set -euo pipefail
export PATH="$HOME/.dotnet:$PATH"
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
ROOT="$(cd "$(dirname "$0")" && pwd)"

# API si no está arriba
if ! curl -sf http://127.0.0.1:5180/health >/dev/null && ! curl -sf http://127.0.0.1:5047/health >/dev/null; then
  echo "Iniciando API..."
  (cd "$ROOT/src/ProfeAsistente.Api" && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5180) &
  for i in $(seq 1 40); do
    curl -sf http://127.0.0.1:5180/health >/dev/null && break
    sleep 0.5
  done
fi

cd "$ROOT/src/ProfeAsistente.Maui"
dotnet build -f net8.0-maccatalyst -c Debug
open "bin/Debug/net8.0-maccatalyst/maccatalyst-arm64/ProfeAsistente.Maui.app"
echo "ProfeAsistente abierto."
