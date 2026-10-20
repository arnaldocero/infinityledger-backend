using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Rol { get; set; }

    public string? Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsientoEncabezado> AsientosEncabezados { get; set; } = new List<AsientoEncabezado>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;
}
