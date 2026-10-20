-- =====================================================================
-- CodeLedger - Fase 1 / Base de datos
-- Completa las cuentas de nivel 3 que faltan en el plan de cuentas (PUC)
-- de la empresa 1 y reasigna las subcuentas de nivel 4 a su padre correcto.
--
--   13 -> 1305 -> 130505   Clientes
--   22 -> 2205 -> 220505   Proveedores nacionales
--   31 -> 3105 -> 310505   Capital suscrito y pagado
--   41 -> 4135 -> 413505   Comercio al por mayor y al por menor
--   51 -> 5135 -> 513505   Servicios
--
-- Si algo falla, la transacción se revierte completa.
-- =====================================================================

START TRANSACTION;

-- 1. Crear las cuentas de nivel 3 (no reciben movimiento) colgando de su grupo (nivel 2)
INSERT INTO plan_cuentas
    (id_empresa, codigo, nombre_cuenta, id_cuenta_padre, nivel,
     tipo_cuenta, naturaleza, es_movimiento, pide_tercero, pide_centro_costo, activo)
SELECT p.id_empresa, n.codigo, n.nombre, p.id_cuenta, 3,
       p.tipo_cuenta, p.naturaleza, 0, 0, 0, 1
FROM (
    SELECT '1305' AS codigo, 'CLIENTES'                            AS nombre, '13' AS codigo_padre UNION ALL
    SELECT '2205',           'NACIONALES',                                    '22'                 UNION ALL
    SELECT '3105',           'CAPITAL SUSCRITO Y PAGADO',                     '31'                 UNION ALL
    SELECT '4135',           'COMERCIO AL POR MAYOR Y AL POR MENOR',          '41'                 UNION ALL
    SELECT '5135',           'SERVICIOS',                                     '51'
) AS n
JOIN plan_cuentas p
  ON p.id_empresa = 1
 AND p.codigo     = n.codigo_padre;

-- 2. Reasignar cada subcuenta de nivel 4 a su nueva cuenta de nivel 3
UPDATE plan_cuentas h
JOIN plan_cuentas p
  ON p.id_empresa = h.id_empresa
 AND p.codigo     = LEFT(h.codigo, 4)
SET h.id_cuenta_padre = p.id_cuenta
WHERE h.id_empresa = 1
  AND h.codigo IN ('130505', '220505', '310505', '413505', '513505');

COMMIT;
