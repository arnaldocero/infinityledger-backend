using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class PeriodoFiscal
{
    public int IdPeriodo { get; set; }

    public int IdEmpresa { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public string? Estado { get; set; }

    public virtual ICollection<AsientoEncabezado> AsientosEncabezados { get; set; } = new List<AsientoEncabezado>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual ICollection<SaldoMensual> SaldosMensuales { get; set; } = new List<SaldoMensual>();
}
