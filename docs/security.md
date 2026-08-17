# Seguridad

## Contraseñas

- Mínimo 10 caracteres, mayúscula, minúscula, número y especial.
- No igual a usuario/correo.
- Historial de últimas 5 (`PasswordHistory`).
- Hash vía ASP.NET Identity (nunca texto plano).
- Cambio/reset revoca todas las sesiones.

## Bloqueo

5 intentos fallidos → lockout 15 minutos.

## Rate limiting

Configuración `RateLimiting`:

- Login: 10/min
- Refresh: 30/min
- Password reset: 5/hora

## Encabezados HTTP

Producción: HSTS + HTTPS redirection.  
Siempre: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`.

## Data Protection

Usar ASP.NET Core Data Protection para tokens internos Identity. Persistir claves en ruta protegida en producción para no invalidar resets al reiniciar.

## DevelopmentAuthentication

```json
"DevelopmentAuthentication": { "Enabled": false, "DefaultUserName": "" }
```

Deshabilitado por defecto. Error si se habilita en Production.

## MAUI SecureStorage

Access/refresh tokens solo en SecureStorage. No Preferences/JSON/SQLite/logs.
