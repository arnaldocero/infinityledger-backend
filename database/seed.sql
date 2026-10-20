-- CodeLedger - Datos iniciales (empresa demo, PUC base, comprobantes, periodos, centros de costo)
-- El usuario admin se inserta SIN contrasena valida: ver README.md para asignarla.
USE codeledger;


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

LOCK TABLES `empresas` WRITE;
/*!40000 ALTER TABLE `empresas` DISABLE KEYS */;
INSERT INTO `empresas` (`id_empresa`, `nit_rut`, `razon_social`, `nombre_comercial`, `direccion`, `telefono`, `email`, `moneda_principal`, `logo_url`, `estado`, `creado_en`) VALUES (1,'901345678-1','CodeLedger Solutions S.A.S.','CodeLedger Software','Calle 79 # 51-36, Barranquilla','+57 300 123 4567','contacto@codeledger.com','COP',NULL,'ACTIVA','2026-08-06 19:17:13');
/*!40000 ALTER TABLE `empresas` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `usuarios` WRITE;
/*!40000 ALTER TABLE `usuarios` DISABLE KEYS */;
INSERT INTO `usuarios` (`id_usuario`, `id_empresa`, `nombre`, `email`, `password_hash`, `rol`, `estado`, `creado_en`) VALUES (1,1,'Administrador Principal','admin@codeledger.com','PENDIENTE_ASIGNAR_PASSWORD','ADMIN','ACTIVO','2026-08-06 19:17:13');
/*!40000 ALTER TABLE `usuarios` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `plan_cuentas` WRITE;
/*!40000 ALTER TABLE `plan_cuentas` DISABLE KEYS */;
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (1,1,'1','ACTIVO',NULL,1,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (2,1,'11','EFECTIVO Y EQUIVALENTES DE EFECTIVO',1,2,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (3,1,'1105','CAJA',2,3,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (4,1,'110505','Caja General',3,4,'ACTIVO','DEBITO',1,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (5,1,'1110','BANCOS',2,3,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (6,1,'111005','Bancos Nacionales - M/N',5,4,'ACTIVO','DEBITO',1,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (7,1,'13','CUENTAS POR COBRAR (CLIENTES)',1,2,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (8,1,'130505','Clientes Nacionales',21,4,'ACTIVO','DEBITO',1,1,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (9,1,'2','PASIVO',NULL,1,'PASIVO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (10,1,'22','PROVEEDORES',9,2,'PASIVO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (11,1,'220505','Proveedores Nacionales',22,4,'PASIVO','CREDITO',1,1,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (12,1,'3','PATRIMONIO',NULL,1,'PATRIMONIO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (13,1,'31','CAPITAL SOCIAL',12,2,'PATRIMONIO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (14,1,'310505','Capital Suscrito y Pagado',23,4,'PATRIMONIO','CREDITO',1,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (15,1,'4','INGRESOS',NULL,1,'INGRESO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (16,1,'41','OPERACIONALES',15,2,'INGRESO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (17,1,'413505','Venta de Software y Servicios TI',24,4,'INGRESO','CREDITO',1,1,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (18,1,'5','GASTOS',NULL,1,'GASTO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (19,1,'51','ADMINISTRATIVOS',18,2,'GASTO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (20,1,'513505','Servicios Públicos e Internet',25,4,'GASTO','DEBITO',1,1,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (21,1,'1305','CLIENTES',7,3,'ACTIVO','DEBITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (22,1,'2205','NACIONALES',10,3,'PASIVO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (23,1,'3105','CAPITAL SUSCRITO Y PAGADO',13,3,'PATRIMONIO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (24,1,'4135','COMERCIO AL POR MAYOR Y AL POR MENOR',16,3,'INGRESO','CREDITO',0,0,0,1);
INSERT INTO `plan_cuentas` (`id_cuenta`, `id_empresa`, `codigo`, `nombre_cuenta`, `id_cuenta_padre`, `nivel`, `tipo_cuenta`, `naturaleza`, `es_movimiento`, `pide_tercero`, `pide_centro_costo`, `activo`) VALUES (25,1,'5135','SERVICIOS',19,3,'GASTO','DEBITO',0,0,0,1);
/*!40000 ALTER TABLE `plan_cuentas` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `centros_costo` WRITE;
/*!40000 ALTER TABLE `centros_costo` DISABLE KEYS */;
INSERT INTO `centros_costo` (`id_centro_costo`, `id_empresa`, `codigo`, `nombre`, `activo`) VALUES (1,1,'CC-100','Administración General',1);
INSERT INTO `centros_costo` (`id_centro_costo`, `id_empresa`, `codigo`, `nombre`, `activo`) VALUES (2,1,'CC-200','Desarrollo de Software',1);
/*!40000 ALTER TABLE `centros_costo` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `comprobantes_tipos` WRITE;
/*!40000 ALTER TABLE `comprobantes_tipos` DISABLE KEYS */;
INSERT INTO `comprobantes_tipos` (`id_tipo_comprobante`, `id_empresa`, `codigo`, `nombre`, `consecutivo_actual`) VALUES (1,1,'ING','Comprobante de Ingreso / Caja',1);
INSERT INTO `comprobantes_tipos` (`id_tipo_comprobante`, `id_empresa`, `codigo`, `nombre`, `consecutivo_actual`) VALUES (2,1,'EGR','Comprobante de Egreso / Pago',1);
INSERT INTO `comprobantes_tipos` (`id_tipo_comprobante`, `id_empresa`, `codigo`, `nombre`, `consecutivo_actual`) VALUES (3,1,'FACT','Facturación de Venta',1);
INSERT INTO `comprobantes_tipos` (`id_tipo_comprobante`, `id_empresa`, `codigo`, `nombre`, `consecutivo_actual`) VALUES (4,1,'COMP','Facturación de Compra',1);
INSERT INTO `comprobantes_tipos` (`id_tipo_comprobante`, `id_empresa`, `codigo`, `nombre`, `consecutivo_actual`) VALUES (5,1,'NOT','Nota Contable / Ajustes',1);
/*!40000 ALTER TABLE `comprobantes_tipos` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `periodos_fiscales` WRITE;
/*!40000 ALTER TABLE `periodos_fiscales` DISABLE KEYS */;
INSERT INTO `periodos_fiscales` (`id_periodo`, `id_empresa`, `anio`, `mes`, `fecha_inicio`, `fecha_fin`, `estado`) VALUES (1,1,2026,8,'2026-08-01','2026-08-31','ABIERTO');
INSERT INTO `periodos_fiscales` (`id_periodo`, `id_empresa`, `anio`, `mes`, `fecha_inicio`, `fecha_fin`, `estado`) VALUES (2,1,2026,9,'2026-09-01','2026-09-30','ABIERTO');
INSERT INTO `periodos_fiscales` (`id_periodo`, `id_empresa`, `anio`, `mes`, `fecha_inicio`, `fecha_fin`, `estado`) VALUES (3,1,2026,10,'2026-10-01','2026-10-31','ABIERTO');
/*!40000 ALTER TABLE `periodos_fiscales` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `terceros` WRITE;
/*!40000 ALTER TABLE `terceros` DISABLE KEYS */;
INSERT INTO `terceros` (`id_tercero`, `id_empresa`, `tipo_documento`, `numero_documento`, `digito_verificacion`, `nombre_razon_social`, `tipo_tercero`, `direccion`, `telefono`, `email`, `estado`, `creado_en`) VALUES (1,1,'NIT','900123456',8,'Empresa Cliente Demo S.A.S.','CLIENTE','Av. El Dorado # 68-90, Bogotá','+57 601 555 0100','facturacion@clientedemo.com',1,'2026-08-06 19:17:14');
/*!40000 ALTER TABLE `terceros` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

