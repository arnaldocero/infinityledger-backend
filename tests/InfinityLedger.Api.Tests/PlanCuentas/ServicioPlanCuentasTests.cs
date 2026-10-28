using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.PlanCuentas;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.PlanCuentas;

public class ServicioPlanCuentasTests
{
    private static readonly UsuarioActual AdminEmpresa1 = new(1, 1, "127.0.0.1");

    [Fact]
    public async Task Listar_devuelve_solo_las_cuentas_de_la_empresa_ordenadas_por_codigo()
    {
        await using var db = CrearBase();

        var cuentas = await new ServicioPlanCuentas(db).ListarAsync(1, CancellationToken.None);

        Assert.Equal(new[] { "1", "11", "1105", "110505", "2" }, cuentas.Select(c => c.Codigo));
    }

    [Fact]
    public async Task Crear_subcuenta_calcula_nivel_y_padre_y_audita()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva("110510", "Cajas Menores"), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal(4, resultado.Valor!.Nivel);
        Assert.Equal(DatosDePrueba.Caja, resultado.Valor.IdCuentaPadre);
        var log = Assert.Single(db.AuditoriaLogs);
        Assert.Equal(("CREAR", "plan_cuentas"), (log.Accion, log.TablaAfectada));
    }

    [Theory]
    [InlineData("123", "1, 2, 4 o 6 dígitos")]
    [InlineData("140505", "Primero cree la cuenta 1405")]
    public async Task Crear_con_codigo_invalido_o_sin_padre_falla(string codigo, string mensaje)
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva(codigo, "Cuenta"), CancellationToken.None);

        Assert.Equal(TipoError.Validacion, resultado.Error);
        Assert.Contains(mensaje, resultado.Mensaje);
        Assert.Empty(db.AuditoriaLogs);
    }

    [Fact]
    public async Task Crear_codigo_repetido_es_conflicto()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva("110505", "Otra caja"), CancellationToken.None);

        Assert.Equal(TipoError.Conflicto, resultado.Error);
    }

    [Fact]
    public async Task El_mismo_codigo_se_puede_usar_en_otra_empresa()
    {
        await using var db = CrearBase();
        var adminEmpresa2 = new UsuarioActual(3, 2, null);

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(adminEmpresa2, Nueva("11", "DISPONIBLE"), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal(DatosDePrueba.ActivoEmpresa2, resultado.Valor!.IdCuentaPadre);
    }

    [Fact]
    public async Task Crear_bajo_una_cuenta_auxiliar_falla()
    {
        await using var db = CrearBase();
        var caja = await db.PlanCuentas.FindAsync(DatosDePrueba.Caja);
        caja!.EsMovimiento = true;
        await db.SaveChangesAsync();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva("110510", "Cajas Menores"), CancellationToken.None);

        Assert.Contains("es auxiliar", resultado.Mensaje);
    }

    [Fact]
    public async Task Crear_con_tipo_distinto_al_del_padre_falla()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva("1110", "BANCOS", tipo: "PASIVO"), CancellationToken.None);

        Assert.Contains("mismo de la cuenta 11", resultado.Mensaje);
    }

    [Fact]
    public async Task Editar_cambia_los_datos_y_audita()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).EditarAsync(AdminEmpresa1, DatosDePrueba.CajaGeneral,
            Edicion("Caja Principal", movimiento: true, pideTercero: true), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal("Caja Principal", resultado.Valor!.NombreCuenta);
        Assert.True(resultado.Valor.PideTercero);
        Assert.Equal("EDITAR", Assert.Single(db.AuditoriaLogs).Accion);
    }

    [Fact]
    public async Task Editar_cuenta_de_otra_empresa_no_la_encuentra()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).EditarAsync(AdminEmpresa1, DatosDePrueba.ActivoEmpresa2,
            Edicion("Intento"), CancellationToken.None);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
        Assert.Equal("ACTIVO EMPRESA DOS", (await db.PlanCuentas.FindAsync(DatosDePrueba.ActivoEmpresa2))!.NombreCuenta);
    }

    [Fact]
    public async Task Una_cuenta_con_subcuentas_no_puede_ser_auxiliar()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).EditarAsync(AdminEmpresa1, DatosDePrueba.Caja,
            Edicion("CAJA", movimiento: true), CancellationToken.None);

        Assert.Contains("no puede ser auxiliar", resultado.Mensaje);
    }

    [Fact]
    public async Task No_se_desactiva_una_cuenta_con_subcuentas_activas()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).EditarAsync(AdminEmpresa1, DatosDePrueba.Caja,
            Edicion("CAJA", activo: false), CancellationToken.None);

        Assert.Contains("desactive sus subcuentas", resultado.Mensaje);
    }

    [Fact]
    public async Task No_se_cambia_el_tipo_de_una_subcuenta()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).EditarAsync(AdminEmpresa1, DatosDePrueba.CajaGeneral,
            Edicion("Caja General", movimiento: true, tipo: "GASTO"), CancellationToken.None);

        Assert.Contains("mismo de la cuenta 1105", resultado.Mensaje);
    }

    [Fact]
    public async Task Tipo_no_valido_falla()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPlanCuentas(db).CrearAsync(AdminEmpresa1, Nueva("3", "PATRIMONIO", tipo: "OTRO"), CancellationToken.None);

        Assert.Contains("Tipo de cuenta no válido", resultado.Mensaje);
    }

    private static CrearCuentaRequest Nueva(string codigo, string nombre, string tipo = "ACTIVO") => new()
    {
        Codigo = codigo,
        NombreCuenta = nombre,
        TipoCuenta = tipo,
        Naturaleza = "DEBITO",
        EsMovimiento = codigo.Length == 6,
        Activo = true,
    };

    private static EditarCuentaRequest Edicion(
        string nombre, bool movimiento = false, bool activo = true, bool pideTercero = false, string tipo = "ACTIVO") => new()
    {
        NombreCuenta = nombre,
        TipoCuenta = tipo,
        Naturaleza = "DEBITO",
        EsMovimiento = movimiento,
        PideTercero = pideTercero,
        Activo = activo,
    };

    private static CodeLedgerContext CrearBase()
    {
        var opciones = new DbContextOptionsBuilder<CodeLedgerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CodeLedgerContext(opciones);
        DatosDePrueba.Cargar(db);
        return db;
    }
}
