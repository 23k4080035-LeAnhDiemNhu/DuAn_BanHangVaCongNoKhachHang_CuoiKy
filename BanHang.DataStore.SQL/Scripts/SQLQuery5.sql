USE [QuanLyBanHangCongNo];
GO

-- Khóa chính
SELECT
    T.name AS TenBang,
    K.name AS KhoaChinh,
    C.name AS CotKhoaChinh
FROM sys.tables T
JOIN sys.key_constraints K
    ON K.parent_object_id = T.object_id
JOIN sys.index_columns IC
    ON IC.object_id = T.object_id
   AND IC.index_id = K.unique_index_id
JOIN sys.columns C
    ON C.object_id = IC.object_id
   AND C.column_id = IC.column_id
WHERE K.type = 'PK'
  AND SCHEMA_NAME(T.schema_id) = N'dbo'
ORDER BY T.name, IC.key_ordinal;

-- Khóa ngoại
SELECT
    FK.name AS KhoaNgoai,
    OBJECT_NAME(FK.parent_object_id) AS BangCon,
    OBJECT_NAME(FK.referenced_object_id) AS BangCha,
    FK.is_disabled AS BiVoHieuHoa,
    FK.is_not_trusted AS ChuaDuocXacThuc
FROM sys.foreign_keys FK
WHERE SCHEMA_NAME(FK.schema_id) = N'dbo'
ORDER BY BangCon, KhoaNgoai;