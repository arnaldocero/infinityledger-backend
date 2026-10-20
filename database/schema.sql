-- CodeLedger - Esquema de la base de datos (MariaDB 10.4 / MySQL 8)
-- Solo estructura, sin datos. No borra nada: las tablas que ya existen se omiten.
CREATE DATABASE IF NOT EXISTS codeledger CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE codeledger;


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `asientos_detalle` (
  `id_asiento_detalle` int(11) NOT NULL AUTO_INCREMENT,
  `id_asiento` int(11) NOT NULL,
  `id_cuenta` int(11) NOT NULL,
  `id_tercero` int(11) DEFAULT NULL,
  `id_centro_costo` int(11) DEFAULT NULL,
  `concepto` varchar(255) DEFAULT NULL,
  `debe` decimal(15,2) DEFAULT 0.00 CHECK (`debe` >= 0),
  `haber` decimal(15,2) DEFAULT 0.00 CHECK (`haber` >= 0),
  PRIMARY KEY (`id_asiento_detalle`),
  KEY `id_asiento` (`id_asiento`),
  KEY `id_cuenta` (`id_cuenta`),
  KEY `id_tercero` (`id_tercero`),
  KEY `id_centro_costo` (`id_centro_costo`),
  CONSTRAINT `asientos_detalle_ibfk_1` FOREIGN KEY (`id_asiento`) REFERENCES `asientos_encabezado` (`id_asiento`) ON DELETE CASCADE,
  CONSTRAINT `asientos_detalle_ibfk_2` FOREIGN KEY (`id_cuenta`) REFERENCES `plan_cuentas` (`id_cuenta`),
  CONSTRAINT `asientos_detalle_ibfk_3` FOREIGN KEY (`id_tercero`) REFERENCES `terceros` (`id_tercero`),
  CONSTRAINT `asientos_detalle_ibfk_4` FOREIGN KEY (`id_centro_costo`) REFERENCES `centros_costo` (`id_centro_costo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `asientos_encabezado` (
  `id_asiento` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `id_tipo_comprobante` int(11) NOT NULL,
  `id_periodo` int(11) NOT NULL,
  `id_usuario` int(11) NOT NULL,
  `numero_comprobante` int(11) NOT NULL,
  `fecha_asiento` date NOT NULL,
  `concepto_general` text NOT NULL,
  `estado` enum('BORRADOR','PUBLICADO','ANULADO') DEFAULT 'PUBLICADO',
  `creado_en` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id_asiento`),
  UNIQUE KEY `uk_comprobante_num` (`id_empresa`,`id_tipo_comprobante`,`numero_comprobante`),
  KEY `id_tipo_comprobante` (`id_tipo_comprobante`),
  KEY `id_periodo` (`id_periodo`),
  KEY `id_usuario` (`id_usuario`),
  CONSTRAINT `asientos_encabezado_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE,
  CONSTRAINT `asientos_encabezado_ibfk_2` FOREIGN KEY (`id_tipo_comprobante`) REFERENCES `comprobantes_tipos` (`id_tipo_comprobante`),
  CONSTRAINT `asientos_encabezado_ibfk_3` FOREIGN KEY (`id_periodo`) REFERENCES `periodos_fiscales` (`id_periodo`),
  CONSTRAINT `asientos_encabezado_ibfk_4` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`id_usuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `auditoria_logs` (
  `id_log` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `id_usuario` int(11) DEFAULT NULL,
  `accion` varchar(50) NOT NULL,
  `tabla_afectada` varchar(50) NOT NULL,
  `detalle` text DEFAULT NULL,
  `ip_origen` varchar(45) DEFAULT NULL,
  `fecha_hora` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id_log`),
  KEY `id_empresa` (`id_empresa`),
  CONSTRAINT `auditoria_logs_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `cartera_movimientos` (
  `id_movimiento` int(11) NOT NULL AUTO_INCREMENT,
  `id_factura` int(11) NOT NULL,
  `id_asiento` int(11) NOT NULL,
  `fecha_pago` date NOT NULL,
  `monto_pagado` decimal(15,2) NOT NULL,
  `metodo_pago` enum('EFECTIVO','TRANSFERENCIA','CHEQUE','TARJETA') NOT NULL,
  `observaciones` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id_movimiento`),
  KEY `id_factura` (`id_factura`),
  KEY `id_asiento` (`id_asiento`),
  CONSTRAINT `cartera_movimientos_ibfk_1` FOREIGN KEY (`id_factura`) REFERENCES `facturas` (`id_factura`) ON DELETE CASCADE,
  CONSTRAINT `cartera_movimientos_ibfk_2` FOREIGN KEY (`id_asiento`) REFERENCES `asientos_encabezado` (`id_asiento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `centros_costo` (
  `id_centro_costo` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `codigo` varchar(20) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `activo` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id_centro_costo`),
  UNIQUE KEY `uk_empresa_centro_cod` (`id_empresa`,`codigo`),
  CONSTRAINT `centros_costo_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `comprobantes_tipos` (
  `id_tipo_comprobante` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `codigo` varchar(10) NOT NULL,
  `nombre` varchar(80) NOT NULL,
  `consecutivo_actual` int(11) DEFAULT 1,
  PRIMARY KEY (`id_tipo_comprobante`),
  UNIQUE KEY `uk_empresa_tipo_cod` (`id_empresa`,`codigo`),
  CONSTRAINT `comprobantes_tipos_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `empresas` (
  `id_empresa` int(11) NOT NULL AUTO_INCREMENT,
  `nit_rut` varchar(20) NOT NULL,
  `razon_social` varchar(150) NOT NULL,
  `nombre_comercial` varchar(150) DEFAULT NULL,
  `direccion` varchar(200) DEFAULT NULL,
  `telefono` varchar(30) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `moneda_principal` varchar(3) DEFAULT 'COP',
  `logo_url` varchar(255) DEFAULT NULL,
  `estado` enum('ACTIVA','INACTIVA') DEFAULT 'ACTIVA',
  `creado_en` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id_empresa`),
  UNIQUE KEY `nit_rut` (`nit_rut`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `facturas` (
  `id_factura` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `id_tercero` int(11) NOT NULL,
  `id_asiento` int(11) DEFAULT NULL,
  `tipo_factura` enum('VENTA','COMPRA') NOT NULL,
  `numero_factura` varchar(50) NOT NULL,
  `fecha_emision` date NOT NULL,
  `fecha_vencimiento` date NOT NULL,
  `subtotal` decimal(15,2) NOT NULL,
  `impuestos` decimal(15,2) DEFAULT 0.00,
  `total` decimal(15,2) NOT NULL,
  `saldo_pendiente` decimal(15,2) NOT NULL,
  `estado` enum('PENDIENTE','PAGADA_PARCIAL','PAGADA_TOTAL','ANULADA') DEFAULT 'PENDIENTE',
  PRIMARY KEY (`id_factura`),
  UNIQUE KEY `uk_empresa_factura` (`id_empresa`,`tipo_factura`,`numero_factura`),
  KEY `id_tercero` (`id_tercero`),
  KEY `id_asiento` (`id_asiento`),
  CONSTRAINT `facturas_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE,
  CONSTRAINT `facturas_ibfk_2` FOREIGN KEY (`id_tercero`) REFERENCES `terceros` (`id_tercero`),
  CONSTRAINT `facturas_ibfk_3` FOREIGN KEY (`id_asiento`) REFERENCES `asientos_encabezado` (`id_asiento`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `periodos_fiscales` (
  `id_periodo` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `anio` int(11) NOT NULL,
  `mes` int(11) NOT NULL CHECK (`mes` between 1 and 12),
  `fecha_inicio` date NOT NULL,
  `fecha_fin` date NOT NULL,
  `estado` enum('ABIERTO','CERRADO') DEFAULT 'ABIERTO',
  PRIMARY KEY (`id_periodo`),
  UNIQUE KEY `uk_empresa_periodo` (`id_empresa`,`anio`,`mes`),
  CONSTRAINT `periodos_fiscales_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `plan_cuentas` (
  `id_cuenta` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `codigo` varchar(20) NOT NULL,
  `nombre_cuenta` varchar(150) NOT NULL,
  `id_cuenta_padre` int(11) DEFAULT NULL,
  `nivel` int(11) NOT NULL,
  `tipo_cuenta` enum('ACTIVO','PASIVO','PATRIMONIO','INGRESO','GASTO','COSTO_VENTAS','COSTO_PRODUCCION','ORDEN') NOT NULL,
  `naturaleza` enum('DEBITO','CREDITO') NOT NULL,
  `es_movimiento` tinyint(1) DEFAULT 0,
  `pide_tercero` tinyint(1) DEFAULT 0,
  `pide_centro_costo` tinyint(1) DEFAULT 0,
  `activo` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id_cuenta`),
  UNIQUE KEY `uk_empresa_codigo` (`id_empresa`,`codigo`),
  KEY `id_cuenta_padre` (`id_cuenta_padre`),
  CONSTRAINT `plan_cuentas_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE,
  CONSTRAINT `plan_cuentas_ibfk_2` FOREIGN KEY (`id_cuenta_padre`) REFERENCES `plan_cuentas` (`id_cuenta`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `saldos_mensuales` (
  `id_saldo` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `id_cuenta` int(11) NOT NULL,
  `id_periodo` int(11) NOT NULL,
  `saldo_inicial` decimal(15,2) DEFAULT 0.00,
  `total_debe` decimal(15,2) DEFAULT 0.00,
  `total_haber` decimal(15,2) DEFAULT 0.00,
  `saldo_final` decimal(15,2) DEFAULT 0.00,
  PRIMARY KEY (`id_saldo`),
  UNIQUE KEY `uk_empresa_cuenta_periodo` (`id_empresa`,`id_cuenta`,`id_periodo`),
  KEY `id_cuenta` (`id_cuenta`),
  KEY `id_periodo` (`id_periodo`),
  CONSTRAINT `saldos_mensuales_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE,
  CONSTRAINT `saldos_mensuales_ibfk_2` FOREIGN KEY (`id_cuenta`) REFERENCES `plan_cuentas` (`id_cuenta`),
  CONSTRAINT `saldos_mensuales_ibfk_3` FOREIGN KEY (`id_periodo`) REFERENCES `periodos_fiscales` (`id_periodo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `terceros` (
  `id_tercero` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `tipo_documento` enum('NIT','CC','CE','PASAPORTE','RUT') NOT NULL,
  `numero_documento` varchar(20) NOT NULL,
  `digito_verificacion` int(11) DEFAULT NULL,
  `nombre_razon_social` varchar(200) NOT NULL,
  `tipo_tercero` set('CLIENTE','PROVEEDOR','EMPLEADO','OTRO') NOT NULL,
  `direccion` varchar(200) DEFAULT NULL,
  `telefono` varchar(30) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `estado` tinyint(1) DEFAULT 1,
  `creado_en` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id_tercero`),
  UNIQUE KEY `uk_empresa_tercero_doc` (`id_empresa`,`numero_documento`),
  CONSTRAINT `terceros_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE IF NOT EXISTS `usuarios` (
  `id_usuario` int(11) NOT NULL AUTO_INCREMENT,
  `id_empresa` int(11) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `rol` enum('ADMIN','CONTADOR','AUXILIAR','AUDITOR') DEFAULT 'AUXILIAR',
  `estado` enum('ACTIVO','INACTIVO') DEFAULT 'ACTIVO',
  `creado_en` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id_usuario`),
  UNIQUE KEY `email` (`email`),
  KEY `id_empresa` (`id_empresa`),
  CONSTRAINT `usuarios_ibfk_1` FOREIGN KEY (`id_empresa`) REFERENCES `empresas` (`id_empresa`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

