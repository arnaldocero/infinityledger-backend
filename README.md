# CodeLedger – Backend

API REST del sistema contable CodeLedger. ASP.NET Core 8 + Entity Framework Core 8 con
Pomelo sobre MariaDB 10.4 (XAMPP).

## Requisitos

- .NET SDK 8 o superior
- MariaDB de XAMPP corriendo en el puerto 3306 con la base `codeledger` (ver `database/README.md`).
  Si el servicio de Windows **MySQL80** está activo, ocupa el puerto 3306 y la API no puede conectarse.

## Primera vez: clave de los tokens

La API firma los JWT con `Jwt:Clave`, que no está en el repositorio. En desarrollo se guarda en
*user-secrets*; sin ella la API no arranca. Genera una clave aleatoria de 64 bytes en base64 y guárdala:

```bash
dotnet user-secrets set "Jwt:Clave" "<clave en base64>" --project src/InfinityLedger.Api
```

En producción va en la variable de entorno `Jwt__Clave`; si falta, la API no arranca. En desarrollo,
si no la encuentra, usa una clave temporal y lo avisa en la consola ("No se encontró Jwt:Clave..."):
funciona, pero hay que volver a iniciar sesión cada vez que se reinicia la API.

## Ejecutar

```bash
dotnet run --project src/InfinityLedger.Api --launch-profile http
```

- API: http://localhost:5000 (el frontend en desarrollo apunta aquí)
- Swagger: http://localhost:5000/swagger (botón **Authorize** para probar con el token del login)
- Prueba de conexión: `GET /api/salud`

Con `--launch-profile https` la API queda en https://localhost:5001, pero antes hay que confiar en el
certificado de desarrollo (`dotnet dev-certs https --trust`). Fuera de desarrollo la API redirige a https.

## Autenticación

| Endpoint | Descripción |
|---|---|
| `POST /api/auth/login` | `{ email, password }` → `{ token, expiraEn, usuario }`. 401 con el mismo mensaje si el correo no existe, la contraseña no coincide o el usuario/empresa están inactivos. Máximo 10 intentos por minuto por IP (429). |
| `GET /api/auth/yo` | Datos del usuario del token. Requiere `Authorization: Bearer <token>`. |

- Contraseñas con bcrypt (`usuarios.password_hash`).
- El token dura `Jwt:ExpiracionMinutos` (480 por defecto) e incluye `sub`, `name`, `email`, `role` e `id_empresa`.
- Cada intento sobre un usuario existente queda en `auditoria_logs` (`LOGIN` o `LOGIN_FALLIDO`, con la IP).

## Configuración

`src/InfinityLedger.Api/appsettings.json` y `appsettings.Development.json`:

- `ConnectionStrings:CodeLedger`: conexión a MariaDB (root sin contraseña, valor por defecto de XAMPP).
- `Cors:Origenes`: orígenes permitidos; por defecto el frontend Angular en http://localhost:4200.
- `Jwt`: emisor, audiencia y duración del token. La clave va en user-secrets (ver arriba).

## Pruebas

```bash
dotnet test
```

Pruebas unitarias del servicio de login y pruebas de integración de los endpoints con
`WebApplicationFactory` y una base en memoria (no necesitan MariaDB).

## Entidades

`Data/Entities` y `Data/CodeLedgerContext.cs` se generaron desde la base con el scaffold de EF Core
(herramienta local en `dotnet-tools.json`). Para regenerarlas tras un cambio de esquema:

```bash
dotnet tool restore
```

```bash
dotnet tool run dotnet-ef dbcontext scaffold "Server=localhost;Port=3306;Database=codeledger;User=root;Password=;" Pomelo.EntityFrameworkCore.MySql --project src/InfinityLedger.Api --context CodeLedgerContext --context-dir Data --output-dir Data/Entities --namespace InfinityLedger.Api.Data.Entities --context-namespace InfinityLedger.Api.Data --no-onconfiguring --force
```

Después del scaffold hay que volver a aplicar a mano:

- Los nombres de clase en singular (`AsientoDetalle`, `AsientoEncabezado`, `CentroCosto`,
  `TipoComprobante`, `PeriodoFiscal`, `SaldoMensual`).
- La propiedad `Tercero.TipoTercero`: EF no mapea columnas `SET`. Su configuración está en
  `Data/CodeLedgerContext.Custom.cs`, que el scaffold no toca.
