using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class CentroCosto
{
    public int IdCentroCosto { get; set; }

    public int IdEmpresa { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public bool? Activo { get; set; }

    public virtual ICollection<AsientoDetalle> AsientosDetalles { get; set; } = new List<AsientoDetalle>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;
}
