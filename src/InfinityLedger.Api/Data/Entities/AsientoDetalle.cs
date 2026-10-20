using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class AsientoDetalle
{
    public int IdAsientoDetalle { get; set; }

    public int IdAsiento { get; set; }

    public int IdCuenta { get; set; }

    public int? IdTercero { get; set; }

    public int? IdCentroCosto { get; set; }

    public string? Concepto { get; set; }

    public decimal? Debe { get; set; }

    public decimal? Haber { get; set; }

    public virtual AsientoEncabezado IdAsientoNavigation { get; set; } = null!;

    public virtual CentroCosto? IdCentroCostoNavigation { get; set; }

    public virtual PlanCuenta IdCuentaNavigation { get; set; } = null!;

    public virtual Tercero? IdTerceroNavigation { get; set; }
}
