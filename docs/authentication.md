# Autenticación (Prompt 9)

## Configuración JWT

Variables de entorno:

| Variable | Uso |
|----------|-----|
| `APPEDUCATIVA_JWT_KEY` | Clave de firma (≥32 caracteres). Obligatoria fuera de Development. |
| `APPEDUCATIVA_ADMIN_USERNAME` | Usuario del primer administrador |
| `APPEDUCATIVA_ADMIN_EMAIL` | Correo del primer administrador |
| `APPEDUCATIVA_ADMIN_PASSWORD` | Contraseña inicial (≥10, mayúscula, minúscula, número, especial) |

En `appsettings.json` (sección `Authentication`):

- `Issuer`: `AppEducativa.Api`
- `Audience`: `AppEducativa.Maui`
- `AccessTokenMinutes`: 30
- `RefreshTokenDays`: 7
- `MaximumFailedAttempts`: 5
- `LockoutMinutes`: 15
- `AllowDevelopmentSigningKey`: solo Development

La clave **no** se guarda en código, SQLite, MAUI ni logs.

## Primer administrador

Se crea solo si no existe ningún usuario (`IdentityBootstrap`).

```bash
export APPEDUCATIVA_JWT_KEY='su-clave-secreta-de-al-menos-32-chars'
export APPEDUCATIVA_ADMIN_USERNAME=admin
export APPEDUCATIVA_ADMIN_EMAIL=admin@local.test
export APPEDUCATIVA_ADMIN_PASSWORD='Admin!Pass123'
```

En Development, si faltan variables, se usa un admin de desarrollo con `MustChangePassword=true`.

## Login

`POST /api/auth/login`

```json
{
  "userNameOrEmail": "admin",
  "password": "Admin!Pass123",
  "rememberMe": true,
  "institutionId": null
}
```

Respuesta: `accessToken`, `refreshToken`, `roles`, `permissions`, `mustChangePassword`, `user`.

Mensaje genérico ante fallo: **Las credenciales no son válidas.**

## Refresh

`POST /api/auth/refresh` con `{ "refreshToken": "..." }`.

- El refresh se almacena solo como hash SHA-256.
- Rotación en cada uso.
- Reutilización de un token ya rotado revoca todas las sesiones del usuario.

## Swagger

1. Ejecutar login.
2. Copiar `accessToken`.
3. Authorize → `Bearer {token}`.
4. Probar endpoints protegidos.

## MAUI

Tokens en `SecureStorage` (`SecureTokenStorageService`).  
`AuthenticatedApiClientHandler` adjunta Bearer, envía `X-Institution-Id` y refresca una vez ante 401.

## Limitaciones MVP

- Recuperación de contraseña no envía correo; en Development expone `developmentResetToken`.
- Sin sincronización en nube ni 2FA real más allá de flags Identity.
