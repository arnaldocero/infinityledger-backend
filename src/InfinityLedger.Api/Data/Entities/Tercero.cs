using System;
using System.Collections.Generic;

namespace InfinityLedger.Api.Data.Entities;

public partial class Tercero
{
    public int IdTercero { get; set; }

    public int IdEmpresa { get; set; }

    public string TipoDocumento { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public int? DigitoVerificacion { get; set; }

    public string NombreRazonSocial { get; set; } = null!;

    /// <summary>
    /// Columna SET de MySQL: valores separados por coma, p. ej. "CLIENTE,PROVEEDOR".
    /// El scaffold de EF no la genera; se mantiene a mano en CodeLedgerContext.Custom.cs.
    /// </summary>
    public string TipoTercero { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public bool? Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsientoDetalle> AsientosDetalles { get; set; } = new List<AsientoDetalle>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;
}
