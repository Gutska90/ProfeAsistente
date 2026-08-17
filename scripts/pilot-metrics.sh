#!/usr/bin/env bash
# Consulta métricas del piloto (API en :5180).
# Uso:
#   TOKEN=eyJ... ./scripts/pilot-metrics.sh
#   ./scripts/pilot-metrics.sh   # login demo admin si está disponible
set -euo pipefail

API="${PROFEASISTENTE_API:-http://127.0.0.1:5180}"

if [[ -z "${TOKEN:-}" ]]; then
  RAW=$(curl -sf -X POST "${API}/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"userName":"admin","password":"Admin!Pass123"}') || {
    echo "No se pudo hacer login. Exporte TOKEN=... o arranque la API." >&2
    exit 1
  }
  TOKEN=$(printf '%s' "$RAW" | python3 -c "import sys,json; print(json.load(sys.stdin).get('accessToken',''))")
fi

if [[ -z "${TOKEN}" ]]; then
  echo "TOKEN vacío." >&2
  exit 1
fi

echo "=== Pilot metrics (${API}) ==="
curl -sf -H "Authorization: Bearer ${TOKEN}" "${API}/api/pilot/metrics" | python3 -m json.tool
echo
echo "=== AI usage summary ==="
curl -sf -H "Authorization: Bearer ${TOKEN}" "${API}/api/ai-usage/summary" | python3 -m json.tool
