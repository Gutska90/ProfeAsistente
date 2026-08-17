#!/usr/bin/env bash
# Smoke de release: restore + tests + build Release API/MAUI (sin empaquetar completo).
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "${ROOT}"

echo "==> Restore"
dotnet restore ProfeAsistente.sln --nologo

echo "==> Test API"
dotnet test tests/ProfeAsistente.Api.Tests/ProfeAsistente.Api.Tests.csproj -c Release --nologo

echo "==> Test CurriculumImporter"
dotnet test tests/ProfeAsistente.CurriculumImporter.Tests/ProfeAsistente.CurriculumImporter.Tests.csproj -c Release --nologo

echo "==> Build API Release"
dotnet build src/ProfeAsistente.Api/ProfeAsistente.Api.csproj -c Release --nologo

echo "==> Build MAUI Release (Mac Catalyst)"
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst -c Release --nologo

echo "Smoke OK."
