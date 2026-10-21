-- =========================================================================
-- Migration: Nâng cấp bảng dbo.NhatKyEmailNhacNos hỗ trợ Idempotency bền vững
-- Bổ sung: IdempotencyKey (Unique Filtered Index), MaPheDuyet, EmailNhanThucTe,
-- CheDoGui, NguonSoan, PayloadHash
-- Thiết kế Idempotent: chạy nhiều lần an toàn, chỉ áp dụng trên DB Verify.
-- =========================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND type in (N'U'))
BEGIN
    -- 1. Bổ sung cột IdempotencyKey
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'IdempotencyKey')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [IdempotencyKey] NVARCHAR(100) NULL;
        PRINT 'Đã thêm cột IdempotencyKey.';
    END

    -- 2. Bổ sung cột MaPheDuyet
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'MaPheDuyet')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [MaPheDuyet] NVARCHAR(100) NULL;
        PRINT 'Đã thêm cột MaPheDuyet.';
    END

    -- 3. Bổ sung cột EmailNhanThucTe (lưu người nhận thực tế khi dùng redirect / test)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'EmailNhanThucTe')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [EmailNhanThucTe] NVARCHAR(256) NULL;
        PRINT 'Đã thêm cột EmailNhanThucTe.';
    END

    -- 4. Bổ sung cột CheDoGui ('MoPhong' hoặc 'GuiThat')
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'CheDoGui')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [CheDoGui] NVARCHAR(50) NOT NULL CONSTRAINT [DF_NhatKyEmailNhacNos_CheDoGui] DEFAULT 'MoPhong';
        PRINT 'Đã thêm cột CheDoGui.';
    END

    -- 5. Bổ sung cột NguonSoan ('AI', 'MauCoSan', 'MoPhong')
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'NguonSoan')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [NguonSoan] NVARCHAR(50) NOT NULL CONSTRAINT [DF_NhatKyEmailNhacNos_NguonSoan] DEFAULT 'AI';
        PRINT 'Đã thêm cột NguonSoan.';
    END

    -- 6. Bổ sung cột PayloadHash (phát hiện xung đột Idempotency khi cùng key khác nội dung)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND name = 'PayloadHash')
    BEGIN
        ALTER TABLE [dbo].[NhatKyEmailNhacNos] ADD [PayloadHash] NVARCHAR(128) NULL;
        PRINT 'Đã thêm cột PayloadHash.';
    END

    -- 7. Tạo Unique Filtered Index cho IdempotencyKey qua dynamic SQL với SET QUOTED_IDENTIFIER ON
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_NhatKyEmailNhacNos_IdempotencyKey' AND object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]'))
    BEGIN
        EXEC('SET QUOTED_IDENTIFIER ON;
              CREATE UNIQUE NONCLUSTERED INDEX [UX_NhatKyEmailNhacNos_IdempotencyKey]
              ON [dbo].[NhatKyEmailNhacNos] ([IdempotencyKey])
              WHERE [IdempotencyKey] IS NOT NULL;');
        PRINT 'Đã tạo Unique Index UX_NhatKyEmailNhacNos_IdempotencyKey.';
    END
END
ELSE
BEGIN
    PRINT 'Bảng dbo.NhatKyEmailNhacNos chưa tồn tại, vui lòng chạy migration khởi tạo trước.';
END
GO
