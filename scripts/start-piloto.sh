#!/usr/bin/env bash
# Arranca la API publicada y abre la app de piloto.
# Uso (desde artifacts/piloto-mac o desde el repo):
#   ./start-piloto.sh
#   ./scripts/start-piloto.sh
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"

HERE="$(cd "$(dirname "$0")" && pwd)"

# Detectar carpeta del paquete (artifacts/piloto-mac) o repo
if [[ -d "${HERE}/api" && -d "${HERE}/ProfeAsistente.app" ]]; then
  PKG="${HERE}"
elif [[ -d "${HERE}/../artifacts/piloto-mac/api" ]]; then
  PKG="$(cd "${HERE}/../artifacts/piloto-mac" && pwd)"
else
  echo "No se encontró el paquete. Ejecute antes: ./scripts/publish-piloto-mac.sh" >&2
  exit 1
fi

API_DIR="${PKG}/api"
APP="${PKG}/ProfeAsistente.app"
URL="${PROFEASISTENTE_URL:-http://127.0.0.1:5180}"
ENV_NAME="${ASPNETCORE_ENVIRONMENT:-Development}"

if [[ ! -f "${API_DIR}/ProfeAsistente.Api.dll" ]]; then
  echo "Falta ${API_DIR}/ProfeAsistente.Api.dll" >&2
  exit 1
fi

is_up() {
  curl -sf "${URL}/health/live" >/dev/null 2>&1 || curl -sf "${URL}/health" >/dev/null 2>&1
}

if ! is_up; then
  echo "Iniciando API (${ENV_NAME}) en ${URL}..."
  (
    cd "${API_DIR}"
    export ASPNETCORE_ENVIRONMENT="${ENV_NAME}"
    # Demo útil en piloto local; en producción real use Production + PROFEASISTENTE_* 
    exec dotnet ProfeAsistente.Api.dll --urls "${URL}"
  ) &
  API_PID=$!
  trap 'kill ${API_PID} 2>/dev/null || true' EXIT

  for _ in $(seq 1 60); do
    is_up && break
    sleep 0.5
  done
  if ! is_up; then
    echo "La API no respondió a tiempo." >&2
    exit 1
  fi
  echo "API OK (pid ${API_PID})."
else
  echo "API ya estaba en ${URL}."
fi

echo "Abriendo ProfeAsistente..."
open "${APP}"

echo "Deje esta terminal abierta mientras usa la app (Ctrl+C detiene la API si la inició este script)."
# Mantener vivo si nosotros lanzamos la API
if [[ -n "${API_PID:-}" ]]; then
  wait "${API_PID}" || true
fi
