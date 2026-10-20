using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class TipoComprobante
{
    public int IdTipoComprobante { get; set; }

    public int IdEmpresa { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int? ConsecutivoActual { get; set; }

    public virtual ICollection<AsientoEncabezado> AsientosEncabezados { get; set; } = new List<AsientoEncabezado>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;
}
