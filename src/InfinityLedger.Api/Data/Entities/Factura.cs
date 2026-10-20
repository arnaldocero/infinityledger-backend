using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class Factura
{
    public int IdFactura { get; set; }

    public int IdEmpresa { get; set; }

    public int IdTercero { get; set; }

    public int? IdAsiento { get; set; }

    public string TipoFactura { get; set; } = null!;

    public string NumeroFactura { get; set; } = null!;

    public DateOnly FechaEmision { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? Impuestos { get; set; }

    public decimal Total { get; set; }

    public decimal SaldoPendiente { get; set; }

    public string? Estado { get; set; }

    public virtual ICollection<CarteraMovimiento> CarteraMovimientos { get; set; } = new List<CarteraMovimiento>();

    public virtual AsientoEncabezado? IdAsientoNavigation { get; set; }

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual Tercero IdTerceroNavigation { get; set; } = null!;
}
