using System;
using System.Collections.Generic;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Data;

public partial class CodeLedgerContext : DbContext
{
    public CodeLedgerContext(DbContextOptions<CodeLedgerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AsientoDetalle> AsientosDetalles { get; set; }

    public virtual DbSet<AsientoEncabezado> AsientosEncabezados { get; set; }

    public virtual DbSet<AuditoriaLog> AuditoriaLogs { get; set; }

    public virtual DbSet<CarteraMovimiento> CarteraMovimientos { get; set; }

    public virtual DbSet<CentroCosto> CentrosCostos { get; set; }

    public virtual DbSet<TipoComprobante> ComprobantesTipos { get; set; }

    public virtual DbSet<Empresa> Empresas { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<PeriodoFiscal> PeriodosFiscales { get; set; }

    public virtual DbSet<PlanCuenta> PlanCuentas { get; set; }

    public virtual DbSet<SaldoMensual> SaldosMensuales { get; set; }

    public virtual DbSet<Tercero> Terceros { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AsientoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdAsientoDetalle).HasName("PRIMARY");

            entity.ToTable("asientos_detalle");

            entity.HasIndex(e => e.IdAsiento, "id_asiento");

            entity.HasIndex(e => e.IdCentroCosto, "id_centro_costo");

            entity.HasIndex(e => e.IdCuenta, "id_cuenta");

            entity.HasIndex(e => e.IdTercero, "id_tercero");

            entity.Property(e => e.IdAsientoDetalle)
                .HasColumnType("int(11)")
                .HasColumnName("id_asiento_detalle");
            entity.Property(e => e.Concepto)
                .HasMaxLength(255)
                .HasColumnName("concepto");
            entity.Property(e => e.Debe)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("debe");
            entity.Property(e => e.Haber)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("haber");
            entity.Property(e => e.IdAsiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_asiento");
            entity.Property(e => e.IdCentroCosto)
                .HasColumnType("int(11)")
                .HasColumnName("id_centro_costo");
            entity.Property(e => e.IdCuenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_cuenta");
            entity.Property(e => e.IdTercero)
                .HasColumnType("int(11)")
                .HasColumnName("id_tercero");

            entity.HasOne(d => d.IdAsientoNavigation).WithMany(p => p.AsientosDetalles)
                .HasForeignKey(d => d.IdAsiento)
                .HasConstraintName("asientos_detalle_ibfk_1");

            entity.HasOne(d => d.IdCentroCostoNavigation).WithMany(p => p.AsientosDetalles)
                .HasForeignKey(d => d.IdCentroCosto)
                .HasConstraintName("asientos_detalle_ibfk_4");

            entity.HasOne(d => d.IdCuentaNavigation).WithMany(p => p.AsientosDetalles)
                .HasForeignKey(d => d.IdCuenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asientos_detalle_ibfk_2");

            entity.HasOne(d => d.IdTerceroNavigation).WithMany(p => p.AsientosDetalles)
                .HasForeignKey(d => d.IdTercero)
                .HasConstraintName("asientos_detalle_ibfk_3");
        });

        modelBuilder.Entity<AsientoEncabezado>(entity =>
        {
            entity.HasKey(e => e.IdAsiento).HasName("PRIMARY");

            entity.ToTable("asientos_encabezado");

            entity.HasIndex(e => e.IdPeriodo, "id_periodo");

            entity.HasIndex(e => e.IdTipoComprobante, "id_tipo_comprobante");

            entity.HasIndex(e => e.IdUsuario, "id_usuario");

            entity.HasIndex(e => new { e.IdEmpresa, e.IdTipoComprobante, e.NumeroComprobante }, "uk_comprobante_num").IsUnique();

            entity.Property(e => e.IdAsiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_asiento");
            entity.Property(e => e.ConceptoGeneral)
                .HasColumnType("text")
                .HasColumnName("concepto_general");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'PUBLICADO'")
                .HasColumnType("enum('BORRADOR','PUBLICADO','ANULADO')")
                .HasColumnName("estado");
            entity.Property(e => e.FechaAsiento).HasColumnName("fecha_asiento");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.IdPeriodo)
                .HasColumnType("int(11)")
                .HasColumnName("id_periodo");
            entity.Property(e => e.IdTipoComprobante)
                .HasColumnType("int(11)")
                .HasColumnName("id_tipo_comprobante");
            entity.Property(e => e.IdUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuario");
            entity.Property(e => e.NumeroComprobante)
                .HasColumnType("int(11)")
                .HasColumnName("numero_comprobante");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.AsientosEncabezados)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("asientos_encabezado_ibfk_1");

            entity.HasOne(d => d.IdPeriodoNavigation).WithMany(p => p.AsientosEncabezados)
                .HasForeignKey(d => d.IdPeriodo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asientos_encabezado_ibfk_3");

            entity.HasOne(d => d.IdTipoComprobanteNavigation).WithMany(p => p.AsientosEncabezados)
                .HasForeignKey(d => d.IdTipoComprobante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asientos_encabezado_ibfk_2");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AsientosEncabezados)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asientos_encabezado_ibfk_4");
        });

        modelBuilder.Entity<AuditoriaLog>(entity =>
        {
            entity.HasKey(e => e.IdLog).HasName("PRIMARY");

            entity.ToTable("auditoria_logs");

            entity.HasIndex(e => e.IdEmpresa, "id_empresa");

            entity.Property(e => e.IdLog)
                .HasColumnType("int(11)")
                .HasColumnName("id_log");
            entity.Property(e => e.Accion)
                .HasMaxLength(50)
                .HasColumnName("accion");
            entity.Property(e => e.Detalle)
                .HasColumnType("text")
                .HasColumnName("detalle");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.IdUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuario");
            entity.Property(e => e.IpOrigen)
                .HasMaxLength(45)
                .HasColumnName("ip_origen");
            entity.Property(e => e.TablaAfectada)
                .HasMaxLength(50)
                .HasColumnName("tabla_afectada");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.AuditoriaLogs)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("auditoria_logs_ibfk_1");
        });

        modelBuilder.Entity<CarteraMovimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PRIMARY");

            entity.ToTable("cartera_movimientos");

            entity.HasIndex(e => e.IdAsiento, "id_asiento");

            entity.HasIndex(e => e.IdFactura, "id_factura");

            entity.Property(e => e.IdMovimiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_movimiento");
            entity.Property(e => e.FechaPago).HasColumnName("fecha_pago");
            entity.Property(e => e.IdAsiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_asiento");
            entity.Property(e => e.IdFactura)
                .HasColumnType("int(11)")
                .HasColumnName("id_factura");
            entity.Property(e => e.MetodoPago)
                .HasColumnType("enum('EFECTIVO','TRANSFERENCIA','CHEQUE','TARJETA')")
                .HasColumnName("metodo_pago");
            entity.Property(e => e.MontoPagado)
                .HasPrecision(15, 2)
                .HasColumnName("monto_pagado");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(255)
                .HasColumnName("observaciones");

            entity.HasOne(d => d.IdAsientoNavigation).WithMany(p => p.CarteraMovimientos)
                .HasForeignKey(d => d.IdAsiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cartera_movimientos_ibfk_2");

            entity.HasOne(d => d.IdFacturaNavigation).WithMany(p => p.CarteraMovimientos)
                .HasForeignKey(d => d.IdFactura)
                .HasConstraintName("cartera_movimientos_ibfk_1");
        });

        modelBuilder.Entity<CentroCosto>(entity =>
        {
            entity.HasKey(e => e.IdCentroCosto).HasName("PRIMARY");

            entity.ToTable("centros_costo");

            entity.HasIndex(e => new { e.IdEmpresa, e.Codigo }, "uk_empresa_centro_cod").IsUnique();

            entity.Property(e => e.IdCentroCosto)
                .HasColumnType("int(11)")
                .HasColumnName("id_centro_costo");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.CentrosCostos)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("centros_costo_ibfk_1");
        });

        modelBuilder.Entity<TipoComprobante>(entity =>
        {
            entity.HasKey(e => e.IdTipoComprobante).HasName("PRIMARY");

            entity.ToTable("comprobantes_tipos");

            entity.HasIndex(e => new { e.IdEmpresa, e.Codigo }, "uk_empresa_tipo_cod").IsUnique();

            entity.Property(e => e.IdTipoComprobante)
                .HasColumnType("int(11)")
                .HasColumnName("id_tipo_comprobante");
            entity.Property(e => e.Codigo)
                .HasMaxLength(10)
                .HasColumnName("codigo");
            entity.Property(e => e.ConsecutivoActual)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("consecutivo_actual");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.ComprobantesTipos)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("comprobantes_tipos_ibfk_1");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.IdEmpresa).HasName("PRIMARY");

            entity.ToTable("empresas");

            entity.HasIndex(e => e.NitRut, "nit_rut").IsUnique();

            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("creado_en");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'ACTIVA'")
                .HasColumnType("enum('ACTIVA','INACTIVA')")
                .HasColumnName("estado");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .HasColumnName("logo_url");
            entity.Property(e => e.MonedaPrincipal)
                .HasMaxLength(3)
                .HasDefaultValueSql("'COP'")
                .HasColumnName("moneda_principal");
            entity.Property(e => e.NitRut)
                .HasMaxLength(20)
                .HasColumnName("nit_rut");
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(150)
                .HasColumnName("nombre_comercial");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(150)
                .HasColumnName("razon_social");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.IdFactura).HasName("PRIMARY");

            entity.ToTable("facturas");

            entity.HasIndex(e => e.IdAsiento, "id_asiento");

            entity.HasIndex(e => e.IdTercero, "id_tercero");

            entity.HasIndex(e => new { e.IdEmpresa, e.TipoFactura, e.NumeroFactura }, "uk_empresa_factura").IsUnique();

            entity.Property(e => e.IdFactura)
                .HasColumnType("int(11)")
                .HasColumnName("id_factura");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'PENDIENTE'")
                .HasColumnType("enum('PENDIENTE','PAGADA_PARCIAL','PAGADA_TOTAL','ANULADA')")
                .HasColumnName("estado");
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.IdAsiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_asiento");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.IdTercero)
                .HasColumnType("int(11)")
                .HasColumnName("id_tercero");
            entity.Property(e => e.Impuestos)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("impuestos");
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(50)
                .HasColumnName("numero_factura");
            entity.Property(e => e.SaldoPendiente)
                .HasPrecision(15, 2)
                .HasColumnName("saldo_pendiente");
            entity.Property(e => e.Subtotal)
                .HasPrecision(15, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.TipoFactura)
                .HasColumnType("enum('VENTA','COMPRA')")
                .HasColumnName("tipo_factura");
            entity.Property(e => e.Total)
                .HasPrecision(15, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdAsientoNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdAsiento)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("facturas_ibfk_3");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("facturas_ibfk_1");

            entity.HasOne(d => d.IdTerceroNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdTercero)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("facturas_ibfk_2");
        });

        modelBuilder.Entity<PeriodoFiscal>(entity =>
        {
            entity.HasKey(e => e.IdPeriodo).HasName("PRIMARY");

            entity.ToTable("periodos_fiscales");

            entity.HasIndex(e => new { e.IdEmpresa, e.Anio, e.Mes }, "uk_empresa_periodo").IsUnique();

            entity.Property(e => e.IdPeriodo)
                .HasColumnType("int(11)")
                .HasColumnName("id_periodo");
            entity.Property(e => e.Anio)
                .HasColumnType("int(11)")
                .HasColumnName("anio");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'ABIERTO'")
                .HasColumnType("enum('ABIERTO','CERRADO')")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.Mes)
                .HasColumnType("int(11)")
                .HasColumnName("mes");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.PeriodosFiscales)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("periodos_fiscales_ibfk_1");
        });

        modelBuilder.Entity<PlanCuenta>(entity =>
        {
            entity.HasKey(e => e.IdCuenta).HasName("PRIMARY");

            entity.ToTable("plan_cuentas");

            entity.HasIndex(e => e.IdCuentaPadre, "id_cuenta_padre");

            entity.HasIndex(e => new { e.IdEmpresa, e.Codigo }, "uk_empresa_codigo").IsUnique();

            entity.Property(e => e.IdCuenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_cuenta");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.EsMovimiento)
                .HasDefaultValueSql("'0'")
                .HasColumnName("es_movimiento");
            entity.Property(e => e.IdCuentaPadre)
                .HasColumnType("int(11)")
                .HasColumnName("id_cuenta_padre");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.Naturaleza)
                .HasColumnType("enum('DEBITO','CREDITO')")
                .HasColumnName("naturaleza");
            entity.Property(e => e.Nivel)
                .HasColumnType("int(11)")
                .HasColumnName("nivel");
            entity.Property(e => e.NombreCuenta)
                .HasMaxLength(150)
                .HasColumnName("nombre_cuenta");
            entity.Property(e => e.PideCentroCosto)
                .HasDefaultValueSql("'0'")
                .HasColumnName("pide_centro_costo");
            entity.Property(e => e.PideTercero)
                .HasDefaultValueSql("'0'")
                .HasColumnName("pide_tercero");
            entity.Property(e => e.TipoCuenta)
                .HasColumnType("enum('ACTIVO','PASIVO','PATRIMONIO','INGRESO','GASTO','COSTO_VENTAS','COSTO_PRODUCCION','ORDEN')")
                .HasColumnName("tipo_cuenta");

            entity.HasOne(d => d.IdCuentaPadreNavigation).WithMany(p => p.InverseIdCuentaPadreNavigation)
                .HasForeignKey(d => d.IdCuentaPadre)
                .HasConstraintName("plan_cuentas_ibfk_2");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.PlanCuenta)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("plan_cuentas_ibfk_1");
        });

        modelBuilder.Entity<SaldoMensual>(entity =>
        {
            entity.HasKey(e => e.IdSaldo).HasName("PRIMARY");

            entity.ToTable("saldos_mensuales");

            entity.HasIndex(e => e.IdCuenta, "id_cuenta");

            entity.HasIndex(e => e.IdPeriodo, "id_periodo");

            entity.HasIndex(e => new { e.IdEmpresa, e.IdCuenta, e.IdPeriodo }, "uk_empresa_cuenta_periodo").IsUnique();

            entity.Property(e => e.IdSaldo)
                .HasColumnType("int(11)")
                .HasColumnName("id_saldo");
            entity.Property(e => e.IdCuenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_cuenta");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.IdPeriodo)
                .HasColumnType("int(11)")
                .HasColumnName("id_periodo");
            entity.Property(e => e.SaldoFinal)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("saldo_final");
            entity.Property(e => e.SaldoInicial)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("saldo_inicial");
            entity.Property(e => e.TotalDebe)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total_debe");
            entity.Property(e => e.TotalHaber)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total_haber");

            entity.HasOne(d => d.IdCuentaNavigation).WithMany(p => p.SaldosMensuales)
                .HasForeignKey(d => d.IdCuenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("saldos_mensuales_ibfk_2");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.SaldosMensuales)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("saldos_mensuales_ibfk_1");

            entity.HasOne(d => d.IdPeriodoNavigation).WithMany(p => p.SaldosMensuales)
                .HasForeignKey(d => d.IdPeriodo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("saldos_mensuales_ibfk_3");
        });

        modelBuilder.Entity<Tercero>(entity =>
        {
            entity.HasKey(e => e.IdTercero).HasName("PRIMARY");

            entity.ToTable("terceros");

            entity.HasIndex(e => new { e.IdEmpresa, e.NumeroDocumento }, "uk_empresa_tercero_doc").IsUnique();

            entity.Property(e => e.IdTercero)
                .HasColumnType("int(11)")
                .HasColumnName("id_tercero");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("creado_en");
            entity.Property(e => e.DigitoVerificacion)
                .HasColumnType("int(11)")
                .HasColumnName("digito_verificacion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.NombreRazonSocial)
                .HasMaxLength(200)
                .HasColumnName("nombre_razon_social");
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .HasColumnName("numero_documento");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
            entity.Property(e => e.TipoDocumento)
                .HasColumnType("enum('NIT','CC','CE','PASAPORTE','RUT')")
                .HasColumnName("tipo_documento");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Terceros)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("terceros_ibfk_1");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.IdEmpresa, "id_empresa");

            entity.Property(e => e.IdUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuario");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("creado_en");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'ACTIVO'")
                .HasColumnType("enum('ACTIVO','INACTIVO')")
                .HasColumnName("estado");
            entity.Property(e => e.IdEmpresa)
                .HasColumnType("int(11)")
                .HasColumnName("id_empresa");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Rol)
                .HasDefaultValueSql("'AUXILIAR'")
                .HasColumnType("enum('ADMIN','CONTADOR','AUXILIAR','AUDITOR')")
                .HasColumnName("rol");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("usuarios_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
