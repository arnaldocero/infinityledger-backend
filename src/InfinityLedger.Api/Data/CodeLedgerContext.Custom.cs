using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Data;

/// <summary>
/// Configuración que el scaffold de EF no genera. Va en un archivo aparte para que
/// volver a ejecutar el scaffold sobre CodeLedgerContext.cs no la borre.
/// </summary>
public partial class CodeLedgerContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tercero>(entity =>
        {
            entity.Property(e => e.TipoTercero)
                .HasColumnType("set('CLIENTE','PROVEEDOR','EMPLEADO','OTRO')")
                .HasColumnName("tipo_tercero");
        });
    }
}
