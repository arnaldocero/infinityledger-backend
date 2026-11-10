using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Terceros;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.Terceros;

public class ServicioTercerosTests
{
    private static readonly UsuarioActual AdminEmpresa1 = new(1, 1, null);

    [Fact]
    public async Task Crear_con_nit_calcula_el_digito_y_ordena_los_tipos()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1,
            Nuevo("NIT", "800197268", tipos: ["proveedor", "CLIENTE"]), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal(4, resultado.Valor!.DigitoVerificacion);
        Assert.Equal(new[] { "CLIENTE", "PROVEEDOR" }, resultado.Valor.TiposTercero);
        Assert.Equal("CLIENTE,PROVEEDOR", db.Terceros.Single(t => t.NumeroDocumento == "800197268").TipoTercero);
        Assert.Equal("CREAR", Assert.Single(db.AuditoriaLogs).Accion);
    }

    [Fact]
    public async Task Digito_de_verificacion_incorrecto_falla()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1,
            Nuevo("NIT", "800197268", dv: 5), CancellationToken.None);

        Assert.Equal(TipoError.Validacion, resultado.Error);
        Assert.Contains("debe ser 4", resultado.Mensaje);
    }

    [Fact]
    public async Task Cedula_no_lleva_digito_de_verificacion()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1, Nuevo("CC", "1020304050"), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Null(resultado.Valor!.DigitoVerificacion);
    }

    [Theory]
    [InlineData("CC", "10.203.040", "dígitos")]
    [InlineData("NIT", "900123456-8", "dígitos")]
    [InlineData("PASAPORTE", "AB", "letras o dígitos")]
    [InlineData("TI", "12345", "Tipo de documento no válido")]
    public async Task Documento_con_formato_invalido_falla(string tipo, string numero, string mensaje)
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1, Nuevo(tipo, numero), CancellationToken.None);

        Assert.Equal(TipoError.Validacion, resultado.Error);
        Assert.Contains(mensaje, resultado.Mensaje);
    }

    [Fact]
    public async Task Pasaporte_acepta_letras_y_lo_guarda_en_mayusculas()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1, Nuevo("PASAPORTE", "ab123456"), CancellationToken.None);

        Assert.Equal("AB123456", resultado.Valor!.NumeroDocumento);
    }

    [Fact]
    public async Task Sin_tipos_o_con_tipo_desconocido_falla()
    {
        await using var db = CrearBase();
        var servicio = new ServicioTerceros(db);

        var sinTipos = await servicio.CrearAsync(AdminEmpresa1, Nuevo("CC", "1020304050", tipos: []), CancellationToken.None);
        var desconocido = await servicio.CrearAsync(AdminEmpresa1, Nuevo("CC", "1020304050", tipos: ["SOCIO"]), CancellationToken.None);

        Assert.Contains("al menos un tipo", sinTipos.Mensaje);
        Assert.Contains("SOCIO", desconocido.Mensaje);
    }

    [Fact]
    public async Task Documento_repetido_en_la_empresa_es_conflicto()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).CrearAsync(AdminEmpresa1, Nuevo("NIT", "900123456"), CancellationToken.None);

        Assert.Equal(TipoError.Conflicto, resultado.Error);
    }

    [Fact]
    public async Task Editar_sin_cambiar_el_documento_no_choca_consigo_mismo()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).EditarAsync(AdminEmpresa1, DatosDePrueba.ClienteDemo,
            Nuevo("NIT", "900123456", nombre: "Cliente Demo Renombrado", activo: false), CancellationToken.None);

        Assert.True(resultado.EsExito, resultado.Mensaje);
        Assert.Equal("Cliente Demo Renombrado", resultado.Valor!.NombreRazonSocial);
        Assert.False(resultado.Valor.Activo);
    }

    [Fact]
    public async Task Editar_tercero_de_otra_empresa_no_lo_encuentra()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTerceros(db).EditarAsync(AdminEmpresa1, DatosDePrueba.TerceroEmpresa2,
            Nuevo("NIT", "900123456"), CancellationToken.None);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
    }

    [Fact]
    public async Task Listar_filtra_por_empresa_tipo_y_texto()
    {
        await using var db = CrearBase();
        var servicio = new ServicioTerceros(db);

        var todos = await servicio.ListarAsync(1, null, null, CancellationToken.None);
        var proveedores = await servicio.ListarAsync(1, null, "proveedor", CancellationToken.None);
        var porDocumento = await servicio.ListarAsync(1, "9001", null, CancellationToken.None);

        Assert.Equal(new[] { "Cliente Demo S.A.S.", "Proveedor Persona" }, todos.Select(t => t.NombreRazonSocial));
        Assert.Equal(DatosDePrueba.ProveedorDemo, Assert.Single(proveedores).IdTercero);
        Assert.Equal(DatosDePrueba.ClienteDemo, Assert.Single(porDocumento).IdTercero);
    }

    private static GuardarTerceroRequest Nuevo(
        string tipoDocumento, string numero, int? dv = null, string[]? tipos = null,
        string nombre = "Tercero de prueba", bool activo = true) => new()
    {
        TipoDocumento = tipoDocumento,
        NumeroDocumento = numero,
        DigitoVerificacion = dv,
        NombreRazonSocial = nombre,
        TiposTercero = tipos ?? ["CLIENTE"],
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
