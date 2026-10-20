using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class PlanCuenta
{
    public int IdCuenta { get; set; }

    public int IdEmpresa { get; set; }

    public string Codigo { get; set; } = null!;

    public string NombreCuenta { get; set; } = null!;

    public int? IdCuentaPadre { get; set; }

    public int Nivel { get; set; }

    public string TipoCuenta { get; set; } = null!;

    public string Naturaleza { get; set; } = null!;

    public bool? EsMovimiento { get; set; }

    public bool? PideTercero { get; set; }

    public bool? PideCentroCosto { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<AsientoDetalle> AsientosDetalles { get; set; } = new List<AsientoDetalle>();

    public virtual PlanCuenta? IdCuentaPadreNavigation { get; set; }

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual ICollection<PlanCuenta> InverseIdCuentaPadreNavigation { get; set; } = new List<PlanCuenta>();

    public virtual ICollection<SaldoMensual> SaldosMensuales { get; set; } = new List<SaldoMensual>();
}
