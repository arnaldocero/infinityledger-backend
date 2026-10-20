using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class SaldoMensual
{
    public int IdSaldo { get; set; }

    public int IdEmpresa { get; set; }

    public int IdCuenta { get; set; }

    public int IdPeriodo { get; set; }

    public decimal? SaldoInicial { get; set; }

    public decimal? TotalDebe { get; set; }

    public decimal? TotalHaber { get; set; }

    public decimal? SaldoFinal { get; set; }

    public virtual PlanCuenta IdCuentaNavigation { get; set; } = null!;

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual PeriodoFiscal IdPeriodoNavigation { get; set; } = null!;
}
