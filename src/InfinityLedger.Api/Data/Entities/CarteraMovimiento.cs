using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class CarteraMovimiento
{
    public int IdMovimiento { get; set; }

    public int IdFactura { get; set; }

    public int IdAsiento { get; set; }

    public DateOnly FechaPago { get; set; }

    public decimal MontoPagado { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string? Observaciones { get; set; }

    public virtual AsientoEncabezado IdAsientoNavigation { get; set; } = null!;

    public virtual Factura IdFacturaNavigation { get; set; } = null!;
}
