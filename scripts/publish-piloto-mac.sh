#!/usr/bin/env bash
# Publica un paquete de piloto local (Mac Catalyst arm64 + API).
# Salida: artifacts/piloto-mac/
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${ROOT}/artifacts/piloto-mac"
API_OUT="${OUT}/api"
APP_TFM="net8.0-maccatalyst"
RID="maccatalyst-arm64"

echo "==> Limpiando ${OUT}"
rm -rf "${OUT}"
mkdir -p "${API_OUT}"

echo "==> Tests API"
dotnet test "${ROOT}/tests/ProfeAsistente.Api.Tests/ProfeAsistente.Api.Tests.csproj" -c Release --nologo

echo "==> Publish API (framework-dependent)"
dotnet publish "${ROOT}/src/ProfeAsistente.Api/ProfeAsistente.Api.csproj" \
  -c Release \
  -o "${API_OUT}" \
  --nologo

echo "==> Publish MAUI Mac Catalyst (${RID})"
dotnet publish "${ROOT}/src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj" \
  -f "${APP_TFM}" \
  -c Release \
  -r "${RID}" \
  -p:CreatePackage=false \
  --nologo

# Localizar el .app generado
APP_SRC="$(find "${ROOT}/src/ProfeAsistente.Maui/bin/Release/${APP_TFM}/${RID}" -maxdepth 2 -name 'ProfeAsistente.Maui.app' -type d | head -1)"
if [[ -z "${APP_SRC}" ]]; then
  APP_SRC="$(find "${ROOT}/src/ProfeAsistente.Maui/bin/Release" -name 'ProfeAsistente.Maui.app' -type d | head -1)"
fi
if [[ -z "${APP_SRC}" || ! -d "${APP_SRC}" ]]; then
  echo "ERROR: no se encontró ProfeAsistente.Maui.app tras publish." >&2
  exit 1
fi

echo "==> Copiando app → ${OUT}/ProfeAsistente.app"
rm -rf "${OUT}/ProfeAsistente.app"
cp -R "${APP_SRC}" "${OUT}/ProfeAsistente.app"

cp "${ROOT}/scripts/start-piloto.sh" "${OUT}/start-piloto.sh"
chmod +x "${OUT}/start-piloto.sh"

cat > "${OUT}/LEEME.txt" <<'EOF'
ProfeAsistente — paquete piloto Mac (local)

1. En Terminal, desde esta carpeta:
     ./start-piloto.sh

2. Login demo (si Demo:Enabled / Development):
     admin / Admin!Pass123

3. La API escucha en http://127.0.0.1:5180
   Health: http://127.0.0.1:5180/health/live

Notas
- No es distribución App Store ni notarizada.
- Requiere .NET 8 runtime en el Mac del piloto (API framework-dependent).
- Para datos reales, configure PROFEASISTENTE_JWT_KEY y desactive Demo.
EOF

echo "==> Listo: ${OUT}"
ls -la "${OUT}"
du -sh "${OUT}" "${OUT}/ProfeAsistente.app" "${API_OUT}" 2>/dev/null || true
