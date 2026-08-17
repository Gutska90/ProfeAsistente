# P11 — Observabilidad IA

Cerrada: 2026-08-16.

## Objetivo

Saber cuánto cuesta (estimado) y cuánto tarda cada generación, con qué prompt, para el piloto.

## Qué quedó

| Pieza | Detalle |
|-------|---------|
| `AiUsageRecord` ampliado | UserId, InstitutionId, Purpose, PromptId/Version, LatencyMs, EstimatedCostUsd, GenerationId |
| `AiCostEstimator` | USD estimado por modelo (precios en `AiUsage:ModelPricing`) |
| `PromptCatalog` | IDs/versiones y propósitos (ClassPlan, Guide, Assessment, …) |
| Persistencia | Clase + documento + regeneración de ítem completan usage con tokens/latencia/costo |
| API | `GET /api/ai-usage/summary?fromUtc=&toUtc=` · `GET /api/ai-usage/recent?take=` |
| Tests | `AiCostEstimatorTests` + `PromptCatalogTests` |

## Ejemplo

```bash
# Tras login JWT
curl -s -H "Authorization: Bearer $TOKEN" \
  http://127.0.0.1:5180/api/ai-usage/summary
```

Respuesta típica: totales de generaciones, tokens in/out, costo USD estimado, latencia media, breakdown por `Purpose`.

## Precios

Configurables en `appsettings.json` → `AiUsage.ModelPricing`. No son facturación real de Google; sirven para control de piloto.

## Fuera de P11

- Dashboard MAUI de costos
- Alertas de presupuesto
- Facturación multi-tenant real
- Tracing distribuido (OpenTelemetry)

## Verificación

```bash
dotnet test tests/ProfeAsistente.Api.Tests --filter "FullyQualifiedName~AiCostEstimator|FullyQualifiedName~PromptCatalog"
dotnet test tests/ProfeAsistente.Api.Tests
```
