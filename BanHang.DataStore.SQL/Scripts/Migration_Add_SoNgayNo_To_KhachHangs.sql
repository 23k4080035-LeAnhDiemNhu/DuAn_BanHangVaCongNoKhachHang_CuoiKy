-- =========================================================================================
-- FILE: Migration_Add_SoNgayNo_To_KhachHangs.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG (ECO2415)
-- MÔ TẢ: Migration an toàn nâng cấp CSDL hiện hữu: Bổ sung cột [SoNgayNo] cho bảng [dbo].[KhachHangs].
-- QUY TẮC AN TOÀN:
--   1. Bảo toàn 100% dữ liệu hiện có của các khách hàng cũ; giá trị khởi tạo là 0 (NOT NULL DEFAULT 0).
--   2. Kiểm tra tính tương thích: Nếu cột hoặc ràng buộc đã tồn tại thì kiểm tra kiểu và bỏ qua.
--   3. Tuyệt đối KHÔNG thay đổi, cập nhật hay can thiệp vào [HanThanhToan] của bất kỳ hóa đơn nào trong [dbo].[HoaDons].
--   4. Không hardcode USE database; chặn chạy nhầm trên database hệ thống.
-- GHI CHÚ QUAN TRỌNG: Script này dùng để nâng cấp CSDL hiện hữu; CHƯA thực thi trên QuanLyBanHangCongNo.
-- =========================================================================================

DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF @CurrentDb IN ('master', 'model', 'msdb', 'tempdb')
BEGIN
    RAISERROR(N'LỖI AN TOÀN: Không được thực thi migration trên CSDL hệ thống [%s]! Vui lòng chọn (USE) đúng database nghiệp vụ trước khi chạy.', 16, 1, @CurrentDb);
    SET NOEXEC ON;
END
GO

DECLARE @initMsg NVARCHAR(200) = N'==> [Migration_Add_SoNgayNo] Bắt đầu kiểm tra và nâng cấp CSDL: ' + DB_NAME();
PRINT @initMsg;
GO

DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF OBJECT_ID('dbo.KhachHangs', 'U') IS NULL
BEGIN
    RAISERROR(N'LỖI: Bảng dbo.KhachHangs không tồn tại trên CSDL [%s]. Vui lòng chạy script khởi tạo schema trước.', 16, 1, @CurrentDb);
    RETURN;
END
GO

BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Kiểm tra sự tồn tại của cột SoNgayNo
    IF COL_LENGTH('dbo.KhachHangs', 'SoNgayNo') IS NULL
    BEGIN
        PRINT N'  -> Cột [SoNgayNo] chưa tồn tại. Đang bổ sung cột với giá trị khởi tạo 0 (NOT NULL DEFAULT 0)...';
        
        -- Thêm cột SoNgayNo qua sp_executesql để không bị lỗi biên dịch Msg 207 trong batch
        EXEC sp_executesql N'ALTER TABLE dbo.KhachHangs ADD SoNgayNo INT NOT NULL CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0) CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0)';

        PRINT N'  -> Đã thêm thành công cột [SoNgayNo] vào dbo.KhachHangs.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột [SoNgayNo] đã tồn tại trong dbo.KhachHangs. Đang kiểm tra tính tương thích nghiêm ngặt...';
        
        -- 1.1 Kiểm tra kiểu dữ liệu của cột hiện tại
        DECLARE @DataType NVARCHAR(50);
        DECLARE @IsNullable NVARCHAR(10);
        
        SELECT 
            @DataType = DATA_TYPE,
            @IsNullable = IS_NULLABLE
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'KhachHangs' AND COLUMN_NAME = 'SoNgayNo';

        IF @DataType <> 'int'
        BEGIN
            DECLARE @ErrType NVARCHAR(250) = N'LỖI KHÔNG TƯƠNG THÍCH: Cột SoNgayNo đang có kiểu [' + @DataType + N'], yêu cầu kiểu [int]. Dừng migration để bảo vệ CSDL.';
            RAISERROR(@ErrType, 16, 1);
        END
        ELSE
        BEGIN
            PRINT N'    + 1. Kiểu dữ liệu [int] hoàn toàn tương thích.';
        END

        -- 1.2 Kiểm tra dữ liệu âm hiện có: Thực thi qua sp_executesql để tránh Msg 207
        DECLARE @CountNegative INT = 0;
        EXEC sp_executesql 
            N'SELECT @cnt = COUNT(*) FROM dbo.KhachHangs WHERE SoNgayNo < 0',
            N'@cnt INT OUTPUT',
            @cnt = @CountNegative OUTPUT;

        IF @CountNegative > 0
        BEGIN
            DECLARE @ErrNeg NVARCHAR(300) = N'LỖI DỮ LIỆU BẤT HỢP LỆ: Phát hiện ' + CAST(@CountNegative AS NVARCHAR(10)) + N' bản ghi khách hàng có SoNgayNo < 0. Dừng migration để không làm sai lệch dữ liệu gốc.';
            RAISERROR(@ErrNeg, 16, 1);
        END
        ELSE
        BEGIN
            PRINT N'    + 2. Không có dữ liệu khách hàng mang giá trị SoNgayNo âm.';
        END

        -- 1.3 Kiểm tra Nullability: Đảm bảo cột NOT NULL (gán 0 cho các giá trị NULL nếu có trước khi đổi sang NOT NULL)
        IF @IsNullable = 'YES'
        BEGIN
            PRINT N'    + Cột SoNgayNo đang cho phép NULL. Đang chuẩn hóa giá trị NULL thành 0 và chuyển sang NOT NULL...';
            EXEC sp_executesql N'UPDATE dbo.KhachHangs SET SoNgayNo = 0 WHERE SoNgayNo IS NULL';
            EXEC sp_executesql N'ALTER TABLE dbo.KhachHangs ALTER COLUMN SoNgayNo INT NOT NULL';
            PRINT N'    + 3. Đã chuyển SoNgayNo sang INT NOT NULL thành công.';
        END
        ELSE
        BEGIN
            PRINT N'    + 3. Ràng buộc NOT NULL của cột [SoNgayNo] đã chuẩn xác.';
        END

        -- 1.4 Kiểm tra Default Constraint: Phải thực sự là 0, không âm thầm thay thế nếu có giá trị khác
        DECLARE @DefName NVARCHAR(128);
        DECLARE @DefValue NVARCHAR(MAX);

        SELECT 
            @DefName = dc.name,
            @DefValue = dc.definition
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.KhachHangs') AND c.name = 'SoNgayNo';

        IF @DefName IS NOT NULL
        BEGIN
            -- Chuẩn hóa chuỗi định nghĩa (loại bỏ dấu ngoặc, khoảng trắng)
            DECLARE @CleanDef NVARCHAR(MAX) = REPLACE(REPLACE(REPLACE(REPLACE(@DefValue, '(', ''), ')', ''), ' ', ''), CHAR(9), '');
            IF @CleanDef <> '0'
            BEGIN
                DECLARE @ErrDef NVARCHAR(300) = N'LỖI RÀNG BUỘC KHÔNG TƯƠNG THÍCH: Cột [SoNgayNo] đã có DEFAULT constraint [' + @DefName + N'] với giá trị [' + @DefValue + N'] khác 0. Dừng migration, không tự ý ghi đè.';
                RAISERROR(@ErrDef, 16, 1);
            END
            ELSE
            BEGIN
                PRINT N'    + 4. DEFAULT CONSTRAINT [' + @DefName + N'] đã tồn tại và thực sự có giá trị là 0: ' + @DefValue;
            END
        END
        ELSE
        BEGIN
            PRINT N'    + Chưa có DEFAULT CONSTRAINT. Đang bổ sung DEFAULT (0) cho cột SoNgayNo...';
            EXEC sp_executesql N'ALTER TABLE dbo.KhachHangs ADD CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0) FOR SoNgayNo';
            PRINT N'    + 4. Đã thêm DF_KhachHangs_SoNgayNo DEFAULT (0) thành công.';
        END
    END

    -- 2. Đảm bảo ràng buộc CHECK: SoNgayNo >= 0 phải thực sự bảo đảm điều kiện, đang enabled và trusted
    DECLARE @CheckName NVARCHAR(128);
    DECLARE @CheckDef NVARCHAR(MAX);
    DECLARE @IsDisabled BIT;
    DECLARE @IsNotTrusted BIT;

    SELECT TOP 1
        @CheckName = cc.name,
        @CheckDef = cc.definition,
        @IsDisabled = cc.is_disabled,
        @IsNotTrusted = cc.is_not_trusted
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID('dbo.KhachHangs')
      AND (cc.definition LIKE '%SoNgayNo%' OR cc.name LIKE '%SoNgayNo%');

    IF @CheckName IS NOT NULL
    BEGIN
        -- 2.1 Kiểm tra trạng thái Enabled
        IF @IsDisabled = 1
        BEGIN
            DECLARE @ErrDisabled NVARCHAR(300) = N'LỖI RÀNG BUỘC KHÔNG TƯƠNG THÍCH: Ràng buộc CHECK [' + @CheckName + N'] đang bị vô hiệu hóa (is_disabled = 1). Dừng migration.';
            RAISERROR(@ErrDisabled, 16, 1);
        END

        -- 2.2 Kiểm tra trạng thái Trusted
        IF @IsNotTrusted = 1
        BEGIN
            DECLARE @ErrNotTrusted NVARCHAR(300) = N'LỖI RÀNG BUỘC KHÔNG TƯƠNG THÍCH: Ràng buộc CHECK [' + @CheckName + N'] đang ở trạng thái không tin cậy (is_not_trusted = 1). Dừng migration.';
            RAISERROR(@ErrNotTrusted, 16, 1);
        END

        -- 2.3 Kiểm tra biểu thức định nghĩa: Chỉ chấp nhận đúng biểu thức đơn SoNgayNo >= 0, không dùng LIKE
        DECLARE @CleanCheckDef NVARCHAR(MAX) = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@CheckDef, '(', ''), ')', ''), '[', ''), ']', ''), ' ', ''), CHAR(9), ''), CHAR(10), '');
        SET @CleanCheckDef = REPLACE(@CleanCheckDef, CHAR(13), '');

        IF @CleanCheckDef <> 'SoNgayNo>=0'
        BEGIN
            DECLARE @ErrCheckDef NVARCHAR(400) = N'LỖI RÀNG BUỘC KHÔNG TƯƠNG THÍCH: Ràng buộc CHECK [' + @CheckName + N'] có định nghĩa [' + @CheckDef + N'] không phải là đúng biểu thức đơn [SoNgayNo >= 0]. Dừng migration, vui lòng kiểm tra thủ công.';
            RAISERROR(@ErrCheckDef, 16, 1);
        END

        PRINT N'  -> 5. Ràng buộc CHECK [' + @CheckName + N'] đã tồn tại, đúng biểu thức đơn [SoNgayNo >= 0], đang ENABLED và TRUSTED.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Đang bổ sung ràng buộc CHECK (SoNgayNo >= 0) ở trạng thái WITH CHECK (TRUSTED)...';
        EXEC sp_executesql N'ALTER TABLE dbo.KhachHangs WITH CHECK ADD CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0)';
        PRINT N'  -> 5. Đã thêm ràng buộc CK_KhachHangs_SoNgayNo_NonNegative (ENABLED & TRUSTED) thành công.';
    END

    COMMIT TRANSACTION;
    PRINT N'==> [Migration_Add_SoNgayNo] HOÀN TẤT NÂNG CẤP BẢNG KHACHHANGS THÀNH CÔNG VÀ AN TOÀN!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'LỖI MIGRATION: %s', 16, 1, @ErrorMessage);
END CATCH
GO
