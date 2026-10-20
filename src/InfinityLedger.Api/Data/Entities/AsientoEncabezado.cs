using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class AsientoEncabezado
{
    public int IdAsiento { get; set; }

    public int IdEmpresa { get; set; }

    public int IdTipoComprobante { get; set; }

    public int IdPeriodo { get; set; }

    public int IdUsuario { get; set; }

    public int NumeroComprobante { get; set; }

    public DateOnly FechaAsiento { get; set; }

    public string ConceptoGeneral { get; set; } = null!;

    public string? Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsientoDetalle> AsientosDetalles { get; set; } = new List<AsientoDetalle>();

    public virtual ICollection<CarteraMovimiento> CarteraMovimientos { get; set; } = new List<CarteraMovimiento>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual PeriodoFiscal IdPeriodoNavigation { get; set; } = null!;

    public virtual TipoComprobante IdTipoComprobanteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
