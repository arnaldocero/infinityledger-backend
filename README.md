# CodeLedger – Backend

API REST del sistema contable CodeLedger. ASP.NET Core 8 + Entity Framework Core 8 con
Pomelo sobre MariaDB 10.4 (XAMPP).

## Requisitos

- .NET SDK 8 o superior
- MariaDB de XAMPP corriendo en el puerto 3306 con la base `codeledger` (ver `database/README.md`).
  Si el servicio de Windows **MySQL80** está activo, ocupa el puerto 3306 y la API no puede conectarse.

## Ejecutar

```bash
dotnet run --project src/InfinityLedger.Api --launch-profile https
```

- API: https://localhost:5001 (http://localhost:5000 con `--launch-profile http`)
- Swagger: https://localhost:5001/swagger
- Prueba de conexión: `GET /api/salud`

La primera vez con https puede ser necesario confiar en el certificado de desarrollo: `dotnet dev-certs https --trust`.

## Configuración

`src/InfinityLedger.Api/appsettings.Development.json`:

- `ConnectionStrings:CodeLedger`: conexión a MariaDB (root sin contraseña, valor por defecto de XAMPP).
- `Cors:Origenes`: orígenes permitidos; por defecto el frontend Angular en http://localhost:4200.

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
