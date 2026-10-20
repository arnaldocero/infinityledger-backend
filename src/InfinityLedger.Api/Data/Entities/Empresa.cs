using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class Empresa
{
    public int IdEmpresa { get; set; }

    public string NitRut { get; set; } = null!;

    public string RazonSocial { get; set; } = null!;

    public string? NombreComercial { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? MonedaPrincipal { get; set; }

    public string? LogoUrl { get; set; }

    public string? Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsientoEncabezado> AsientosEncabezados { get; set; } = new List<AsientoEncabezado>();

    public virtual ICollection<AuditoriaLog> AuditoriaLogs { get; set; } = new List<AuditoriaLog>();

    public virtual ICollection<CentroCosto> CentrosCostos { get; set; } = new List<CentroCosto>();

    public virtual ICollection<TipoComprobante> ComprobantesTipos { get; set; } = new List<TipoComprobante>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual ICollection<PeriodoFiscal> PeriodosFiscales { get; set; } = new List<PeriodoFiscal>();

    public virtual ICollection<PlanCuenta> PlanCuenta { get; set; } = new List<PlanCuenta>();

    public virtual ICollection<SaldoMensual> SaldosMensuales { get; set; } = new List<SaldoMensual>();

    public virtual ICollection<Tercero> Terceros { get; set; } = new List<Tercero>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
