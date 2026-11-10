using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;

namespace InfinityLedger.Api.Tests.Infraestructura;

/// <summary>
/// Dos empresas para comprobar que nadie ve datos de otra empresa.
///   Empresa 1: admin, auxiliar, auditor, un PUC pequeño (1 → 11 → 1105 → 110505, y 2 PASIVO),
///              dos terceros y un centro de costo.
///   Empresa 2: admin, la cuenta 1 y un tercero.
/// </summary>
public static class DatosDePrueba
{
    public const string Password = "clave-de-prueba";

    public const string AdminEmpresa1 = "admin@empresa1.com";
    public const string AuxiliarEmpresa1 = "auxiliar@empresa1.com";
    public const string AuditorEmpresa1 = "auditor@empresa1.com";
    public const string AdminEmpresa2 = "admin@empresa2.com";

    // Ids de cuentas sembradas
    public const int Activo = 1;
    public const int Disponible = 2;
    public const int Caja = 3;
    public const int CajaGeneral = 4;
    public const int Pasivo = 5;
    public const int ActivoEmpresa2 = 100;

    // Terceros y centros de costo sembrados
    public const int ClienteDemo = 1;
    public const int ProveedorDemo = 2;
    public const int TerceroEmpresa2 = 100;
    public const int CentroAdministracion = 1;

    public static void Cargar(CodeLedgerContext db)
    {
        db.Empresas.AddRange(
            new Empresa { IdEmpresa = 1, NitRut = "900000001-1", RazonSocial = "Empresa uno", Estado = "ACTIVA" },
            new Empresa { IdEmpresa = 2, NitRut = "900000002-2", RazonSocial = "Empresa dos", Estado = "ACTIVA" });

        // Costo 4 para que las pruebas sean rápidas
        var hash = BCrypt.Net.BCrypt.HashPassword(Password, 4);
        db.Usuarios.AddRange(
            Usuario(1, 1, AdminEmpresa1, "ADMIN", hash),
            Usuario(2, 1, AuxiliarEmpresa1, "AUXILIAR", hash),
            Usuario(4, 1, AuditorEmpresa1, "AUDITOR", hash),
            Usuario(3, 2, AdminEmpresa2, "ADMIN", hash));

        db.PlanCuentas.AddRange(
            Cuenta(Activo, 1, "1", "ACTIVO", null, 1, "ACTIVO", "DEBITO", movimiento: false),
            Cuenta(Disponible, 1, "11", "DISPONIBLE", Activo, 2, "ACTIVO", "DEBITO", movimiento: false),
            Cuenta(Caja, 1, "1105", "CAJA", Disponible, 3, "ACTIVO", "DEBITO", movimiento: false),
            Cuenta(CajaGeneral, 1, "110505", "Caja General", Caja, 4, "ACTIVO", "DEBITO", movimiento: true),
            Cuenta(Pasivo, 1, "2", "PASIVO", null, 1, "PASIVO", "CREDITO", movimiento: false),
            Cuenta(ActivoEmpresa2, 2, "1", "ACTIVO EMPRESA DOS", null, 1, "ACTIVO", "DEBITO", movimiento: false));

        db.Terceros.AddRange(
            Tercero(ClienteDemo, 1, "NIT", "900123456", 8, "Cliente Demo S.A.S.", "CLIENTE"),
            Tercero(ProveedorDemo, 1, "CC", "1234567", null, "Proveedor Persona", "PROVEEDOR"),
            Tercero(TerceroEmpresa2, 2, "NIT", "900123456", 8, "Cliente de la empresa dos", "CLIENTE"));

        db.CentrosCostos.Add(new CentroCosto { IdCentroCosto = CentroAdministracion, IdEmpresa = 1, Codigo = "CC-100", Nombre = "Administración", Activo = true });

        db.SaveChanges();
    }

    private static Usuario Usuario(int id, int idEmpresa, string email, string rol, string hash) => new()
    {
        IdUsuario = id,
        IdEmpresa = idEmpresa,
        Nombre = email,
        Email = email,
        PasswordHash = hash,
        Rol = rol,
        Estado = "ACTIVO",
    };

    private static Tercero Tercero(int id, int idEmpresa, string tipoDoc, string numero, int? dv, string nombre, string tipos) => new()
    {
        IdTercero = id,
        IdEmpresa = idEmpresa,
        TipoDocumento = tipoDoc,
        NumeroDocumento = numero,
        DigitoVerificacion = dv,
        NombreRazonSocial = nombre,
        TipoTercero = tipos,
        Estado = true,
    };

    private static PlanCuenta Cuenta(
        int id, int idEmpresa, string codigo, string nombre, int? idPadre, int nivel,
        string tipo, string naturaleza, bool movimiento) => new()
    {
        IdCuenta = id,
        IdEmpresa = idEmpresa,
        Codigo = codigo,
        NombreCuenta = nombre,
        IdCuentaPadre = idPadre,
        Nivel = nivel,
        TipoCuenta = tipo,
        Naturaleza = naturaleza,
        EsMovimiento = movimiento,
        PideTercero = false,
        PideCentroCosto = false,
        Activo = true,
    };
}
