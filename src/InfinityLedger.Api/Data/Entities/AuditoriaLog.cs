using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class AuditoriaLog
{
    public int IdLog { get; set; }

    public int IdEmpresa { get; set; }

    public int? IdUsuario { get; set; }

    public string Accion { get; set; } = null!;

    public string TablaAfectada { get; set; } = null!;

    public string? Detalle { get; set; }

    public string? IpOrigen { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;
}
