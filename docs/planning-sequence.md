# Secuencia curricular de clases

## Generación determinista

`POST /api/planificaciones/{id}/secuencia/propuestas`

No usa Gemini para asignar OA/indicadores/Bloom.

Algoritmo:

1. Cuenta sesiones disponibles.
2. Reserva diagnóstico / repaso / evaluación.
3. Asigna mínimos por OA (prioridad).
4. Distribuye indicadores.
5. Sugiere progresión Bloom.
6. Detecta déficit y propone alternativas.

## Confirmación

`POST /api/secuencia/propuestas/{id}/confirmar`

Crea/actualiza clases, asocia OA e indicadores, marca propuesta `Confirmed`.  
No genera estructuras Gemini automáticamente.

## Bloom

Configurable con `InitialLevel`, `TargetLevel`, `MaximumLevelJump`, `AllowRegression`.  
Saltos excesivos generan advertencias, no errores definitivos.

## Edición

`PUT /api/secuencia/propuestas/{id}/items/{itemId}` marca `WasManuallyModified`.
