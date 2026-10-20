# Base de datos CodeLedger

MariaDB 10.4 (XAMPP), base `codeledger`, juego de caracteres `utf8mb4`.

## Archivos

| Archivo | Contenido |
|---|---|
| `schema.sql` | Estructura de las 13 tablas. No borra nada: usa `CREATE TABLE IF NOT EXISTS`. |
| `seed.sql` | Datos iniciales: empresa demo, usuario admin, PUC base (25 cuentas), centros de costo, tipos de comprobante, períodos ago–oct 2026 y un tercero demo. |
| `scripts/` | Cambios aplicados sobre la base existente, numerados en orden. Ya están incluidos en `schema.sql` y `seed.sql`. |

## Crear la base desde cero

Desde `C:\xampp\mysql\bin`, en orden:

```bash
./mysql.exe -u root --default-character-set=utf8mb4 < C:/CodeLedger/infinityledger-backend/database/schema.sql
```

```bash
./mysql.exe -u root --default-character-set=utf8mb4 < C:/CodeLedger/infinityledger-backend/database/seed.sql
```

`seed.sql` está pensado para una base vacía; si se ejecuta sobre la base actual falla por claves duplicadas.

## Contraseña del admin

`seed.sql` no incluye el hash real. El usuario `admin@codeledger.com` queda con
`password_hash = 'PENDIENTE_ASIGNAR_PASSWORD'`, así que no puede iniciar sesión
hasta que se le asigne un hash bcrypt válido (60 caracteres, prefijo `$2a$`).

La contraseña de desarrollo del admin está en `infinityledger-backend/.env`
(ignorado por git), en la variable `ADMIN_DEV_PASSWORD`.
