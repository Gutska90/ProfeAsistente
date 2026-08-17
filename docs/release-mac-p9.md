# P9 — Empaquetado Mac Catalyst + checklist release

Cerrada: 2026-08-16.

## Objetivo

Dejar un **paquete de piloto local** en Mac (Apple Silicon) reproducible, sin App Store ni notarización.

## Artefactos

```bash
./scripts/smoke-release.sh          # tests + build Release
./scripts/publish-piloto-mac.sh     # genera artifacts/piloto-mac/
./scripts/start-piloto.sh           # API + abre la app (desde el paquete o el repo)
```

Contenido de `artifacts/piloto-mac/`:

| Ítem | Uso |
|------|-----|
| `ProfeAsistente.app` | Cliente MAUI Release (Mac Catalyst arm64) |
| `api/` | API publicada (`ProfeAsistente.Api.dll`) |
| `start-piloto.sh` | Arranca API en `:5180` y abre la app |
| `LEEME.txt` | Instrucciones cortas |

Versión app: `ApplicationDisplayVersion` **1.0.0** / `ApplicationVersion` **9** (ver `.csproj`).

## Checklist antes de un piloto

- [ ] `./scripts/smoke-release.sh` en verde
- [ ] `./scripts/publish-piloto-mac.sh` genera el paquete
- [ ] En el Mac piloto: .NET 8 runtime instalado
- [ ] `./start-piloto.sh` → login `admin` / `Admin!Pass123` (solo si Demo/Development)
- [ ] Flujo: Hoy → clase → material → evaluar → evidencia
- [ ] Offline: editar sin API un momento y «Sincronizar ahora»
- [ ] Sin Gemini: generación degrada con mensaje claro (o key configurada)
- [ ] Bump de versión en el `.csproj` si es una entrega numerada
- [ ] (Opcional) `PROFEASISTENTE_JWT_KEY` + `ASPNETCORE_ENVIRONMENT=Production` + Demo off

## Info.plist / identidad

- Bundle id: `cl.profeasistente.app`
- Categoría: educación
- `ITSAppUsesNonExemptEncryption` = false
- Entitlements: App Sandbox + `network.client` (la API se arranca **fuera** del sandbox vía script)

## Fuera de P9

- Notarización Apple / Developer ID / Mac App Store
- Instaler `.pkg` firmado
- Android / iOS / Windows empaquetados
- CDN, updates automáticos, telemetría
