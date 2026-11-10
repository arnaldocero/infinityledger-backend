using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Periodos;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.Periodos;

public class ServicioPeriodosTests
{
    private static readonly UsuarioActual AdminEmpresa1 = new(1, 1, null);

    [Fact]
    public async Task Listar_devuelve_los_periodos_de_la_empresa_del_mas_reciente_al_mas_antiguo()
    {
        await using var db = CrearBase();

        var periodos = await new ServicioPeriodos(db).ListarAsync(1, CancellationToken.None);

        Assert.Equal(new[] { 10, 9, 8 }, periodos.Select(p => p.Mes));
    }

    [Fact]
    public async Task Crear_calcula_las_fechas_del_mes()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CrearAsync(AdminEmpresa1, 2026, 11, CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), "ABIERTO"),
            (resultado.Valor!.FechaInicio, resultado.Valor.FechaFin, resultado.Valor.Estado));
    }

    [Fact]
    public async Task Crear_un_periodo_existente_es_conflicto()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CrearAsync(AdminEmpresa1, 2026, 10, CancellationToken.None);

        Assert.Equal(TipoError.Conflicto, resultado.Error);
        Assert.Contains("octubre 2026", resultado.Mensaje);
    }

    [Fact]
    public async Task Generar_anio_crea_solo_los_meses_que_faltan()
    {
        await using var db = CrearBase();
        var servicio = new ServicioPeriodos(db);

        var primera = await servicio.GenerarAnioAsync(AdminEmpresa1, 2026, CancellationToken.None);
        var segunda = await servicio.GenerarAnioAsync(AdminEmpresa1, 2026, CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 11, 12 }, primera.Valor!.Select(p => p.Mes));
        Assert.Equal(12, db.PeriodosFiscales.Count(p => p.IdEmpresa == 1 && p.Anio == 2026));
        Assert.Equal(TipoError.Conflicto, segunda.Error);
    }

    [Fact]
    public async Task Febrero_de_un_anio_bisiesto_termina_el_29()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).GenerarAnioAsync(AdminEmpresa1, 2028, CancellationToken.None);

        Assert.Equal(new DateOnly(2028, 2, 29), resultado.Valor!.Single(p => p.Mes == 2).FechaFin);
    }

    [Fact]
    public async Task No_se_cierra_un_periodo_si_hay_uno_anterior_abierto()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CerrarAsync(AdminEmpresa1, DatosDePrueba.Octubre2026, CancellationToken.None);

        Assert.Equal(TipoError.Validacion, resultado.Error);
        Assert.Contains("Primero cierre el período septiembre 2026", resultado.Mensaje);
    }

    [Fact]
    public async Task Cerrar_en_orden_funciona_y_audita()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CerrarAsync(AdminEmpresa1, DatosDePrueba.Septiembre2026, CancellationToken.None);

        Assert.Equal("CERRADO", resultado.Valor!.Estado);
        var log = Assert.Single(db.AuditoriaLogs);
        Assert.Equal(("CERRAR", "septiembre 2026"), (log.Accion, log.Detalle));
    }

    [Fact]
    public async Task No_se_cierra_un_periodo_con_asientos_en_borrador()
    {
        await using var db = CrearBase();
        var servicio = new ServicioPeriodos(db);
        await servicio.CerrarAsync(AdminEmpresa1, DatosDePrueba.Septiembre2026, CancellationToken.None);

        var resultado = await servicio.CerrarAsync(AdminEmpresa1, DatosDePrueba.Octubre2026, CancellationToken.None);

        Assert.Contains("1 asiento(s) en borrador", resultado.Mensaje);
    }

    [Fact]
    public async Task Cerrar_un_periodo_ya_cerrado_falla()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CerrarAsync(AdminEmpresa1, DatosDePrueba.Agosto2026, CancellationToken.None);

        Assert.Contains("ya está cerrado", resultado.Mensaje);
    }

    [Fact]
    public async Task Reabrir_exige_que_no_haya_periodos_posteriores_cerrados()
    {
        await using var db = CrearBase();
        var servicio = new ServicioPeriodos(db);
        await servicio.CerrarAsync(AdminEmpresa1, DatosDePrueba.Septiembre2026, CancellationToken.None);

        var agosto = await servicio.ReabrirAsync(AdminEmpresa1, DatosDePrueba.Agosto2026, CancellationToken.None);
        var septiembre = await servicio.ReabrirAsync(AdminEmpresa1, DatosDePrueba.Septiembre2026, CancellationToken.None);

        Assert.Contains("Primero reabra el período septiembre 2026", agosto.Mensaje);
        Assert.Equal("ABIERTO", septiembre.Valor!.Estado);
    }

    [Fact]
    public async Task Periodo_de_otra_empresa_no_se_encuentra()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioPeriodos(db).CerrarAsync(AdminEmpresa1, DatosDePrueba.Octubre2026Empresa2, CancellationToken.None);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
    }

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
