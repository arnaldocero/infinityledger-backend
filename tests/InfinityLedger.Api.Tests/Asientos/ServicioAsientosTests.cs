using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.Asientos;

public class ServicioAsientosTests
{
    private static readonly UsuarioActual Auxiliar = new(2, 1, "127.0.0.1");
    private static readonly DateOnly EnOctubre = new(2026, 10, 15);

    [Fact]
    public async Task Crear_borrador_toma_el_consecutivo_del_tipo_y_el_periodo_de_la_fecha()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioAsientos(db).CrearAsync(Auxiliar,
            Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 100)), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        var asiento = resultado.Valor!;
        Assert.Equal(("ING-0005", "BORRADOR", DatosDePrueba.Octubre2026), (asiento.Comprobante, asiento.Estado, asiento.IdPeriodo));
        Assert.Equal(6, db.ComprobantesTipos.Single(t => t.IdTipoComprobante == DatosDePrueba.TipoIngreso).ConsecutivoActual);
        Assert.Equal(("CREAR_BORRADOR", "ING-0005"), (db.AuditoriaLogs.Single().Accion, db.AuditoriaLogs.Single().Detalle));
    }

    [Fact]
    public async Task Crear_publicado_cuadrado_devuelve_lineas_y_totales()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioAsientos(db).CrearAsync(Auxiliar, Asiento(publicar: true,
            Linea(DatosDePrueba.CajaGeneral, debe: 1_190_000),
            Linea(DatosDePrueba.Ventas, haber: 1_190_000, tercero: DatosDePrueba.ClienteDemo)), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        var asiento = resultado.Valor!;
        Assert.Equal("PUBLICADO", asiento.Estado);
        Assert.Equal((1_190_000m, 1_190_000m), (asiento.TotalDebe, asiento.TotalHaber));
        Assert.Equal(("413505", "Cliente Demo S.A.S."), (asiento.Lineas[1].CodigoCuenta, asiento.Lineas[1].NombreTercero));
    }

    [Fact]
    public async Task Publicar_descuadrado_falla_con_la_diferencia()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioAsientos(db).CrearAsync(Auxiliar, Asiento(publicar: true,
            Linea(DatosDePrueba.CajaGeneral, debe: 100_000),
            Linea(DatosDePrueba.Ventas, haber: 90_000, tercero: DatosDePrueba.ClienteDemo)), CancellationToken.None);

        Assert.Equal(TipoError.Validacion, resultado.Error);
        Assert.Equal("El asiento no cuadra: débitos 100.000,00 y créditos 90.000,00 (diferencia 10.000,00).", resultado.Mensaje);
        Assert.Empty(db.AsientosEncabezados.Where(a => a.NumeroComprobante == 5));
    }

    [Fact]
    public async Task Publicar_con_una_sola_linea_falla()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioAsientos(db).CrearAsync(Auxiliar,
            Asiento(publicar: true, Linea(DatosDePrueba.CajaGeneral, debe: 100)), CancellationToken.None);

        Assert.Contains("al menos dos líneas", resultado.Mensaje);
    }

    [Theory]
    [InlineData(2027, 1, 10, "No existe un período fiscal para el 10/01/2027")]
    [InlineData(2026, 8, 20, "El período agosto 2026 está cerrado")]
    public async Task La_fecha_debe_caer_en_un_periodo_abierto(int anio, int mes, int dia, string mensaje)
    {
        await using var db = CrearBase();

        var resultado = await new ServicioAsientos(db).CrearAsync(Auxiliar,
            Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 100)) with { FechaAsiento = new DateOnly(anio, mes, dia) },
            CancellationToken.None);

        Assert.Contains(mensaje, resultado.Mensaje);
    }

    [Fact]
    public async Task Reglas_de_cada_linea()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);

        async Task<string?> Error(LineaAsientoRequest linea) =>
            (await servicio.CrearAsync(Auxiliar, Asiento(publicar: false, linea), CancellationToken.None)).Mensaje;

        Assert.Contains("1105 no es auxiliar", await Error(Linea(DatosDePrueba.Caja, debe: 10)));
        Assert.Contains("110595 está inactiva", await Error(Linea(DatosDePrueba.CajaInactiva, debe: 10)));
        Assert.Contains("la cuenta no existe", await Error(Linea(DatosDePrueba.ActivoEmpresa2, debe: 10)));
        Assert.Contains("no en ambos ni en ninguno", await Error(Linea(DatosDePrueba.CajaGeneral, debe: 10, haber: 10)));
        Assert.Contains("no en ambos ni en ninguno", await Error(Linea(DatosDePrueba.CajaGeneral)));
        Assert.Contains("máximo 2 decimales", await Error(Linea(DatosDePrueba.CajaGeneral, debe: 10.555m)));
        Assert.Contains("413505 exige un tercero", await Error(Linea(DatosDePrueba.Ventas, haber: 10)));
        Assert.Contains("el tercero no existe", await Error(Linea(DatosDePrueba.Ventas, haber: 10, tercero: DatosDePrueba.TerceroEmpresa2)));
        Assert.Contains("513505 exige un centro de costo", await Error(Linea(DatosDePrueba.Servicios, debe: 10)));
        Assert.Empty(db.AsientosEncabezados.Where(a => a.NumeroComprobante == 5));
    }

    [Fact]
    public async Task Editar_reemplaza_las_lineas_y_puede_publicar()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);
        var borrador = (await servicio.CrearAsync(Auxiliar, Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 50)), CancellationToken.None)).Valor!;

        var resultado = await servicio.EditarAsync(Auxiliar, borrador.IdAsiento, Asiento(publicar: true,
            Linea(DatosDePrueba.Servicios, debe: 80, centro: DatosDePrueba.CentroAdministracion),
            Linea(DatosDePrueba.CajaGeneral, haber: 80)), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal(("PUBLICADO", "ING-0005", 2), (resultado.Valor!.Estado, resultado.Valor.Comprobante, resultado.Valor.Lineas.Count));
        Assert.Equal(2, db.AsientosDetalles.Count(d => d.IdAsiento == borrador.IdAsiento));
    }

    [Fact]
    public async Task Solo_se_editan_borradores_y_sin_cambiar_el_tipo()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);

        var publicado = await servicio.EditarAsync(Auxiliar, 1, Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 1)), CancellationToken.None);
        var otroTipo = await servicio.EditarAsync(Auxiliar, 2, Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 1)), CancellationToken.None);

        Assert.Contains("Solo se pueden modificar asientos en borrador", publicado.Mensaje);
        Assert.Contains("No se puede cambiar el tipo", otroTipo.Mensaje);
    }

    [Fact]
    public async Task Publicar_un_borrador_lo_vuelve_a_validar()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);
        var borrador = (await servicio.CrearAsync(Auxiliar, Asiento(publicar: false,
            Linea(DatosDePrueba.CajaGeneral, debe: 30), Linea(DatosDePrueba.CajaGeneral, haber: 20)), CancellationToken.None)).Valor!;

        var descuadrado = await servicio.PublicarAsync(Auxiliar, borrador.IdAsiento, CancellationToken.None);
        await servicio.EditarAsync(Auxiliar, borrador.IdAsiento, Asiento(publicar: false,
            Linea(DatosDePrueba.CajaGeneral, debe: 30), Linea(DatosDePrueba.CajaGeneral, haber: 30)), CancellationToken.None);
        var cuadrado = await servicio.PublicarAsync(Auxiliar, borrador.IdAsiento, CancellationToken.None);

        Assert.Contains("no cuadra", descuadrado.Mensaje);
        Assert.Equal("PUBLICADO", cuadrado.Valor!.Estado);
    }

    [Fact]
    public async Task Anular_cambia_el_estado_y_no_se_repite()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);

        var primera = await servicio.AnularAsync(Auxiliar, 1, CancellationToken.None);
        var segunda = await servicio.AnularAsync(Auxiliar, 1, CancellationToken.None);

        Assert.Equal("ANULADO", primera.Valor!.Estado);
        Assert.Contains("ya está anulado", segunda.Mensaje);
    }

    [Fact]
    public async Task No_se_anula_un_asiento_de_un_periodo_cerrado()
    {
        await using var db = CrearBase();
        db.PeriodosFiscales.Single(p => p.IdPeriodo == DatosDePrueba.Septiembre2026).Estado = "CERRADO";
        await db.SaveChangesAsync();

        var resultado = await new ServicioAsientos(db).AnularAsync(Auxiliar, 1, CancellationToken.None);

        Assert.Contains("septiembre 2026 está cerrado", resultado.Mensaje);
    }

    [Fact]
    public async Task Asiento_de_otra_empresa_no_se_encuentra()
    {
        await using var db = CrearBase();
        var deOtraEmpresa = new UsuarioActual(3, 2, null);

        var resultado = await new ServicioAsientos(db).AnularAsync(deOtraEmpresa, 1, CancellationToken.None);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
    }

    [Fact]
    public async Task Si_otro_usuario_toma_el_mismo_consecutivo_el_asiento_se_renumera()
    {
        var nombreBase = Guid.NewGuid().ToString();
        await using var db1 = CrearBase(nombreBase);
        await using var db2 = Contexto(nombreBase);

        // db1 queda con el tipo ING en memoria (consecutivo 5) mientras otro usuario usa ese número.
        _ = db1.ComprobantesTipos.Single(t => t.IdTipoComprobante == DatosDePrueba.TipoIngreso);
        var otro = await new ServicioAsientos(db2).CrearAsync(Auxiliar, Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 1)), CancellationToken.None);

        var mio = await new ServicioAsientos(db1).CrearAsync(Auxiliar, Asiento(publicar: false, Linea(DatosDePrueba.CajaGeneral, debe: 2)), CancellationToken.None);

        Assert.Equal("ING-0005", otro.Valor!.Comprobante);
        Assert.True(mio.EsExito, mio.Mensaje);
        Assert.Equal("ING-0006", mio.Valor!.Comprobante);
    }

    [Fact]
    public async Task Listar_filtra_por_estado_y_suma_las_lineas()
    {
        await using var db = CrearBase();
        var servicio = new ServicioAsientos(db);
        await servicio.CrearAsync(Auxiliar, Asiento(publicar: false,
            Linea(DatosDePrueba.CajaGeneral, debe: 40), Linea(DatosDePrueba.CajaGeneral, debe: 2.5m)), CancellationToken.None);

        var borradores = await servicio.ListarAsync(1, new FiltroAsientos { Estado = "borrador" }, CancellationToken.None);

        Assert.Equal(new[] { "ING-0005", "NOT-0001" }, borradores.Select(a => a.Comprobante));
        Assert.Equal(42.5m, borradores[0].TotalDebe);
    }

    private static GuardarAsientoRequest Asiento(bool publicar, params LineaAsientoRequest[] lineas) => new()
    {
        IdTipoComprobante = DatosDePrueba.TipoIngreso,
        FechaAsiento = EnOctubre,
        ConceptoGeneral = "Venta de contado",
        Lineas = lineas,
        Publicar = publicar,
    };

    private static LineaAsientoRequest Linea(int idCuenta, decimal debe = 0, decimal haber = 0, int? tercero = null, int? centro = null) => new()
    {
        IdCuenta = idCuenta,
        Debe = debe,
        Haber = haber,
        IdTercero = tercero,
        IdCentroCosto = centro,
    };

    private static CodeLedgerContext CrearBase(string? nombre = null)
    {
        var db = Contexto(nombre ?? Guid.NewGuid().ToString());
        DatosDePrueba.Cargar(db);
        return db;
    }

    private static CodeLedgerContext Contexto(string nombre) =>
        new(new DbContextOptionsBuilder<CodeLedgerContext>().UseInMemoryDatabase(nombre).Options);
}
