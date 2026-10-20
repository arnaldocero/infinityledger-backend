-- =====================================================================
-- CodeLedger - Fase 1 / Base de datos
-- Crea los períodos fiscales de septiembre y octubre 2026 (abiertos)
-- para la empresa 1. Los períodos que ya existan se omiten.
-- =====================================================================

INSERT INTO periodos_fiscales (id_empresa, anio, mes, fecha_inicio, fecha_fin, estado)
SELECT n.id_empresa, n.anio, n.mes, n.fecha_inicio, n.fecha_fin, 'ABIERTO'
FROM (
    SELECT 1 AS id_empresa, 2026 AS anio, 9  AS mes, DATE '2026-09-01' AS fecha_inicio, DATE '2026-09-30' AS fecha_fin UNION ALL
    SELECT 1,               2026,         10,        DATE '2026-10-01',                 DATE '2026-10-31'
) AS n
WHERE NOT EXISTS (
    SELECT 1
    FROM periodos_fiscales p
    WHERE p.id_empresa = n.id_empresa AND p.anio = n.anio AND p.mes = n.mes
);
