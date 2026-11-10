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

        // El UPDATE del consecutivo incluye su valor anterior en el WHERE: si otro usuario
        // tomó el mismo número en paralelo, SaveChanges falla y el asiento se renumera.
        modelBuilder.Entity<TipoComprobante>(entity =>
        {
            entity.Property(e => e.ConsecutivoActual).IsConcurrencyToken();
        });
    }
}
