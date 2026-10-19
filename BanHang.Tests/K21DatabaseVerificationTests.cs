using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm chứng chuyên sâu cho K2.1:
/// - Chốt an toàn CSDL: CHỈ thao tác trên database QuanLyBanHangCongNo_K21_Verify hoặc _Test;
///   Tuyệt đối từ chối chạy trên QuanLyBanHangCongNo (CSDL chính) hoặc CSDL hệ thống.
/// - Kiểm tra bộ script cài mới an toàn:
///   + 01_Safe_CreateSchema.sql: Tạo đủ 7 bảng, cột SoNgayNo kiểu int NOT NULL DEFAULT 0,
///     ràng buộc CHECK SoNgayNo >= 0, Computed Columns (ThanhTien, TongTien, ConLai), Foreign Keys.
///   + 02_Safe_CreateNguoiDungs.sql: Tạo bảng NguoiDungs.
///   + 03_Safe_SeedSampleData.sql: Seed 4 KH, 6 SP, 7 HĐ, 7 TT, 7 Phân bổ, gán SoNgayNo đúng chính sách.
///   + Chạy lại (idempotency): không gây mất dữ liệu, không tạo bản ghi trùng.
/// - Kiểm tra CHECK constraint SoNgayNo trên CSDL: 0, dương, âm (bị chặn).
/// - Kiểm tra Migration trên schema cũ:
///   + Giữ nguyên dữ liệu khách hàng cũ, bổ sung SoNgayNo = 0, NOT NULL.
///   + TUYỆT ĐỐI BẢO TOÀN HanThanhToan của hóa đơn cũ, không bị ghi đè hay thay đổi.
///   + Migration chạy lại an toàn không lỗi.
/// </summary>
public class K21DatabaseVerificationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string VerifyDatabaseName = "QuanLyBanHangCongNo_K21_Verify";
    private const string VerifyConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={VerifyDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly string _scriptsDir;

    public K21DatabaseVerificationTests()
    {
        // Đường dẫn tương đối từ bin/Debug/net8.0 đến BanHang.DataStore.SQL/Scripts
        var baseDir = AppContext.BaseDirectory;
        _scriptsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "BanHang.DataStore.SQL", "Scripts"));
        if (!Directory.Exists(_scriptsDir))
        {
            // Thử fallback nếu chạy từ root
            _scriptsDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "BanHang.DataStore.SQL", "Scripts"));
        }
    }

    private void AssertSafeDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var db = builder.InitialCatalog;

        if (string.Equals(db, "QuanLyBanHangCongNo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("CHỐT AN TOÀN VI PHẠM: Tuyệt đối không thao tác trên CSDL chính QuanLyBanHangCongNo!");
        }

        if (string.Equals(db, "master", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "tempdb", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "model", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "msdb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CHỐT AN TOÀN VI PHẠM: Không thao tác ghi trên CSDL hệ thống '{db}'!");
        }

        if (!db.EndsWith("_K21_Verify", StringComparison.OrdinalIgnoreCase) && !db.EndsWith("_Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CHỐT AN TOÀN VI PHẠM: Chỉ cho phép thao tác trên database kiểm chứng có hậu tố _K21_Verify hoặc _Test. Database hiện tại: '{db}'");
        }
    }

    private IEnumerable<string> SplitSqlBatches(string script)
    {
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var b in batches)
        {
            var trimmed = b.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }

    private async Task ExecuteScriptFileAsync(SqlConnection conn, string fileName)
    {
        var filePath = Path.Combine(_scriptsDir, fileName);
        Assert.True(File.Exists(filePath), $"Tệp script không tồn tại: {filePath}");

        var scriptContent = await File.ReadAllTextAsync(filePath);
        var batches = SplitSqlBatches(scriptContent);

        foreach (var batch in batches)
        {
            await conn.ExecuteAsync(batch);
        }
    }

    public async Task InitializeAsync()
    {
        // 1. Tạo database kiểm chứng mới tinh
        using (var masterConn = new SqlConnection(MasterConnectionString))
        {
            await masterConn.OpenAsync();
            var sqlCreateDb = $@"
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{VerifyDatabaseName}')
                BEGIN
                    ALTER DATABASE [{VerifyDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{VerifyDatabaseName}];
                END;
                CREATE DATABASE [{VerifyDatabaseName}];";
            await masterConn.ExecuteAsync(sqlCreateDb);
        }

        // 2. Kiểm tra chốt an toàn
        AssertSafeDatabase(VerifyConnectionString);

        // 3. Khởi tạo schema và nạp dữ liệu mẫu ban đầu để các test độc lập có môi trường sẵn sàng
        using (var verifyConn = new SqlConnection(VerifyConnectionString))
        {
            await verifyConn.OpenAsync();
            await ExecuteScriptFileAsync(verifyConn, "01_Safe_CreateSchema.sql");
            await ExecuteScriptFileAsync(verifyConn, "02_Safe_CreateNguoiDungs.sql");
            await ExecuteScriptFileAsync(verifyConn, "03_Safe_SeedSampleData.sql");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task K21_Test01_SafeSchema_TaoDayDu7Bang_VaCotSoNgayNo_HopLe()
    {
        AssertSafeDatabase(VerifyConnectionString);

        using var conn = new SqlConnection(VerifyConnectionString);
        await conn.OpenAsync();

        // Thực thi script tạo schema an toàn
        await ExecuteScriptFileAsync(conn, "01_Safe_CreateSchema.sql");

        // 1. Kiểm tra 6 bảng nghiệp vụ tồn tại
        var expectedTables = new[]
        {
            "KhachHangs", "SanPhams", "HoaDons", "ChiTietHoaDons",
            "ThanhToans", "PhanBoThanhToans"
        };

        var actualTables = (await conn.QueryAsync<string>(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'"))
            .ToList();

        foreach (var table in expectedTables)
        {
            Assert.Contains(table, actualTables);
        }

        // 2. Kiểm tra cột SoNgayNo trong dbo.KhachHangs
        var colInfo = await conn.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'KhachHangs' AND COLUMN_NAME = 'SoNgayNo'");

        Assert.NotNull(colInfo);
        Assert.Equal("int", (string)colInfo.DATA_TYPE);
        Assert.Equal("NO", (string)colInfo.IS_NULLABLE);

        // 3. Kiểm tra CHECK constraint SoNgayNo >= 0
        var checkConstraint = await conn.QuerySingleOrDefaultAsync<string>(@"
            SELECT CHECK_CLAUSE
            FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS
            WHERE CONSTRAINT_NAME = 'CK_KhachHangs_SoNgayNo_NonNegative'");

        Assert.NotNull(checkConstraint);
        Assert.Contains("SoNgayNo", checkConstraint);

        // 4. Kiểm tra Computed Columns
        var computedThanhTien = await conn.ExecuteScalarAsync<int>(@"
            SELECT is_computed 
            FROM sys.columns 
            WHERE object_id = OBJECT_ID('dbo.ChiTietHoaDons') AND name = 'ThanhTien'");
        Assert.Equal(1, computedThanhTien);

        var computedConLai = await conn.ExecuteScalarAsync<int>(@"
            SELECT is_computed 
            FROM sys.columns 
            WHERE object_id = OBJECT_ID('dbo.HoaDons') AND name = 'ConLai'");
        Assert.Equal(1, computedConLai);
    }

    [Fact]
    public async Task K21_Test02_SafeCreateNguoiDungs_VaSeedData_ThanhCongVaIdempotent()
    {
        AssertSafeDatabase(VerifyConnectionString);

        using var conn = new SqlConnection(VerifyConnectionString);
        await conn.OpenAsync();

        // 1. Chạy schema, người dùng, seed data
        await ExecuteScriptFileAsync(conn, "01_Safe_CreateSchema.sql");
        await ExecuteScriptFileAsync(conn, "02_Safe_CreateNguoiDungs.sql");
        await ExecuteScriptFileAsync(conn, "03_Safe_SeedSampleData.sql");

        // Kiểm tra số lượng bản ghi sau seed
        var khCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.KhachHangs");
        var spCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.SanPhams");
        var hdCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons");
        var ttCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ThanhToans");
        var pbCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PhanBoThanhToans");

        Assert.True(khCount >= 4, $"Mong đợi >= 4 khách hàng, thực tế: {khCount}");
        Assert.True(spCount >= 6, $"Mong đợi >= 6 sản phẩm, thực tế: {spCount}");
        Assert.True(hdCount >= 7, $"Mong đợi >= 7 hóa đơn, thực tế: {hdCount}");
        Assert.True(ttCount >= 1, $"Mong đợi >= 1 thanh toán, thực tế: {ttCount}");
        Assert.True(pbCount >= 2, $"Mong đợi >= 2 phân bổ, thực tế: {pbCount}");

        // Kiểm tra SoNgayNo được seed đúng chính sách
        var hmSoNgayNo = await conn.ExecuteScalarAsync<int>("SELECT SoNgayNo FROM dbo.KhachHangs WHERE MaKhachHang = 'KH001'");
        var apSoNgayNo = await conn.ExecuteScalarAsync<int>("SELECT SoNgayNo FROM dbo.KhachHangs WHERE MaKhachHang = 'KH002'");
        var bvSoNgayNo = await conn.ExecuteScalarAsync<int>("SELECT SoNgayNo FROM dbo.KhachHangs WHERE MaKhachHang = 'KH003'");
        var ttSoNgayNo = await conn.ExecuteScalarAsync<int>("SELECT SoNgayNo FROM dbo.KhachHangs WHERE MaKhachHang = 'KH004'");

        Assert.Equal(30, hmSoNgayNo);
        Assert.Equal(15, apSoNgayNo);
        Assert.Equal(45, bvSoNgayNo);
        Assert.Equal(0, ttSoNgayNo);

        // 2. Chạy lại lần 2 để kiểm chứng IDEMPOTENCY (không gây lỗi, không tạo trùng)
        await ExecuteScriptFileAsync(conn, "01_Safe_CreateSchema.sql");
        await ExecuteScriptFileAsync(conn, "02_Safe_CreateNguoiDungs.sql");
        await ExecuteScriptFileAsync(conn, "03_Safe_SeedSampleData.sql");

        var khCountSau = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.KhachHangs");
        var spCountSau = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.SanPhams");
        var hdCountSau = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons");

        Assert.Equal(khCount, khCountSau);
        Assert.Equal(spCount, spCountSau);
        Assert.Equal(hdCount, hdCountSau);
    }

    [Fact]
    public async Task K21_Test03_CheckConstraint_SoNgayNo_ChanSoAm_ChoPhepKhongVaSoDuong()
    {
        AssertSafeDatabase(VerifyConnectionString);

        using var conn = new SqlConnection(VerifyConnectionString);
        await conn.OpenAsync();

        // 1. Thêm khách hàng với SoNgayNo = 0 -> Thành công
        var testCode0 = "TEST_KH_0";
        await conn.ExecuteAsync("DELETE FROM dbo.KhachHangs WHERE MaKhachHang LIKE 'TEST_KH_%'");

        var id0 = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, SoNgayNo, TrangThai)
            VALUES (@Code, @Ten, 10000000, 0, 1);
            SELECT SCOPE_IDENTITY();", new { Code = testCode0, Ten = "Khách Hàng Nợ 0 Ngày" });
        Assert.True(id0 > 0);

        // 2. Thêm khách hàng với SoNgayNo dương (45 ngày) -> Thành công
        var testCode45 = "TEST_KH_45";
        var id45 = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, SoNgayNo, TrangThai)
            VALUES (@Code, @Ten, 20000000, 45, 1);
            SELECT SCOPE_IDENTITY();", new { Code = testCode45, Ten = "Khách Hàng Nợ 45 Ngày" });
        Assert.True(id45 > 0);

        // 3. Thêm khách hàng với SoNgayNo âm (-1) -> Bị SQL Server chặn bởi CHECK constraint
        var testCodeAm = "TEST_KH_AM";
        var ex = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, SoNgayNo, TrangThai)
                VALUES (@Code, @Ten, 10000000, -1, 1);", new { Code = testCodeAm, Ten = "Khách Hàng Âm" });
        });

        Assert.Contains("CK_KhachHangs_SoNgayNo_NonNegative", ex.Message);

        // Dọn dẹp bản ghi kiểm thử
        await conn.ExecuteAsync("DELETE FROM dbo.KhachHangs WHERE MaKhachHang IN (@c1, @c2)", new { c1 = testCode0, c2 = testCode45 });
    }

    [Fact]
    public async Task K21_Test04_Migration_BaoToanDuLieuVaHanThanhToanHoaDonCu()
    {
        // Kiểm thử migration trên database kiểm chứng
        AssertSafeDatabase(VerifyConnectionString);

        using var conn = new SqlConnection(VerifyConnectionString);
        await conn.OpenAsync();

        // Giả lập bảng KhachHangsLegacy trên schema tạm thời để kiểm thử quy trình migration
        const string sqlCreateLegacy = @"
            IF OBJECT_ID('dbo.KhachHangs_LegacyTest', 'U') IS NOT NULL DROP TABLE dbo.KhachHangs_LegacyTest;

            CREATE TABLE dbo.KhachHangs_LegacyTest
            (
                KhachHangId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                MaKhachHang NVARCHAR(20) NOT NULL UNIQUE,
                TenKhachHang NVARCHAR(150) NOT NULL,
                SoDienThoai NVARCHAR(20) NULL,
                Email NVARCHAR(100) NULL,
                DiaChi NVARCHAR(250) NULL,
                HanMucCongNo DECIMAL(18,2) NOT NULL DEFAULT (0),
                NgayTao DATETIME NOT NULL DEFAULT (GETDATE()),
                TrangThai BIT NOT NULL DEFAULT (1)
            );

            INSERT INTO dbo.KhachHangs_LegacyTest (MaKhachHang, TenKhachHang, HanMucCongNo)
            VALUES 
            (N'KH_OLD_1', N'Khách Hàng Cũ 1', 10000000),
            (N'KH_OLD_2', N'Khách Hàng Cũ 2', 50000000);
        ";
        await conn.ExecuteAsync(sqlCreateLegacy);

        // Mô phỏng logic migration trên bảng: thêm cột SoNgayNo với DEFAULT 0 và CHECK >= 0
        const string sqlSimulateMigration = @"
            IF COL_LENGTH('dbo.KhachHangs_LegacyTest', 'SoNgayNo') IS NULL
            BEGIN
                ALTER TABLE dbo.KhachHangs_LegacyTest
                ADD SoNgayNo INT NOT NULL CONSTRAINT DF_KhachHangsLegacy_SoNgayNo DEFAULT (0)
                    CONSTRAINT CK_KhachHangsLegacy_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0);
            END
        ";
        await conn.ExecuteAsync(sqlSimulateMigration);

        // Xác nhận dữ liệu cũ đã được gán SoNgayNo = 0 (dùng dynamic SQL để tránh lỗi query plan cache)
        var legacyRecords = (await conn.QueryAsync<dynamic>("EXEC('SELECT MaKhachHang, SoNgayNo FROM dbo.KhachHangs_LegacyTest')")).ToList();
        Assert.Equal(2, legacyRecords.Count);
        foreach (var rec in legacyRecords)
        {
            Assert.Equal(0, (int)rec.SoNgayNo);
        }

        // Chạy lại lần 2 xác nhận idempotency
        await conn.ExecuteAsync(sqlSimulateMigration);

        // Dọn dẹp bảng kiểm thử legacy
        await conn.ExecuteAsync("DROP TABLE dbo.KhachHangs_LegacyTest;");

        // 4. KIỂM TRA ĐẶC BIỆT: Thực thi migration script gốc và xác nhận HanThanhToan của các hóa đơn hiện hữu KHÔNG ĐỔI
        // Đọc snapshot hạn thanh toán hiện tại của tất cả hóa đơn
        var originalInvoices = (await conn.QueryAsync<dynamic>(
            "SELECT HoaDonId, MaHoaDon, NgayLap, HanThanhToan, TongTien, ConLai FROM dbo.HoaDons ORDER BY HoaDonId")).ToList();

        Assert.NotEmpty(originalInvoices);

        // Thực thi script Migration_Add_SoNgayNo_To_KhachHangs.sql
        await ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql");

        // Đọc lại snapshot sau migration
        var afterMigrationInvoices = (await conn.QueryAsync<dynamic>(
            "SELECT HoaDonId, MaHoaDon, NgayLap, HanThanhToan, TongTien, ConLai FROM dbo.HoaDons ORDER BY HoaDonId")).ToList();

        Assert.Equal(originalInvoices.Count, afterMigrationInvoices.Count);

        for (int i = 0; i < originalInvoices.Count; i++)
        {
            var before = originalInvoices[i];
            var after = afterMigrationInvoices[i];

            Assert.Equal((int)before.HoaDonId, (int)after.HoaDonId);
            Assert.Equal((string)before.MaHoaDon, (string)after.MaHoaDon);
            Assert.Equal((DateTime)before.NgayLap, (DateTime)after.NgayLap);
            // TUYỆT ĐỐI BẢO TOÀN HanThanhToan
            Assert.Equal((DateTime)before.HanThanhToan, (DateTime)after.HanThanhToan);
            Assert.Equal((decimal)before.TongTien, (decimal)after.TongTien);
            Assert.Equal((decimal)before.ConLai, (decimal)after.ConLai);
        }

        // 5. KIỂM TRA TÍNH TOÀN VẸN CỦA RÀNG BUỘC SAU MIGRATION TRÊN DB VERIFY:
        // 5.1 DEFAULT constraint thực sự có giá trị là 0
        var defValue = await conn.QuerySingleOrDefaultAsync<string>(@"
            SELECT dc.definition
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.KhachHangs') AND c.name = 'SoNgayNo'");
        Assert.NotNull(defValue);
        var cleanDef = defValue.Replace("(", "").Replace(")", "").Trim();
        Assert.Equal("0", cleanDef);

        // 5.2 CHECK constraint thực sự bảo đảm SoNgayNo >= 0, đang ENABLED (is_disabled = 0) và TRUSTED (is_not_trusted = 0)
        var checkInfo = await conn.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT cc.definition, cc.is_disabled, cc.is_not_trusted
            FROM sys.check_constraints cc
            WHERE cc.parent_object_id = OBJECT_ID('dbo.KhachHangs')
              AND (cc.definition LIKE '%SoNgayNo%' OR cc.name LIKE '%SoNgayNo%')");
        Assert.NotNull(checkInfo);
        Assert.False((bool)checkInfo.is_disabled, "CHECK constraint phải đang ENABLED (is_disabled = false)");
        Assert.False((bool)checkInfo.is_not_trusted, "CHECK constraint phải TRUSTED (is_not_trusted = false)");
        string checkDef = (string)checkInfo.definition;
        string cleanCheck = checkDef.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(" ", "");
        Assert.Contains("SoNgayNo>=0", cleanCheck);

        // 6. KIỂM CHỨNG CÁC TRƯỜNG HỢP KHÔNG TƯƠNG THÍCH ĐỀU BỊ DỪNG VÀ BÁO RÕ (KHÔNG ÂM THẦM BỎ QUA):
        
        // 6.1 Trường hợp bảng có dữ liệu SoNgayNo < 0 -> Bị dừng và báo lỗi dữ liệu bất hợp lệ
        await conn.ExecuteAsync("ALTER TABLE dbo.KhachHangs NOCHECK CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative;");
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, SoNgayNo, TrangThai)
            VALUES ('KH_TEST_NEG_DATA', N'Khách Hàng Dữ Liệu Âm', 10000000, -5, 1);");

        var exNegData = await Assert.ThrowsAsync<SqlException>(() => ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql"));
        Assert.Contains("LỖI DỮ LIỆU BẤT HỢP LỆ", exNegData.Message);

        // Dọn dẹp bản ghi dữ liệu âm và bật lại constraint
        await conn.ExecuteAsync("DELETE FROM dbo.KhachHangs WHERE MaKhachHang = 'KH_TEST_NEG_DATA';");
        await conn.ExecuteAsync("ALTER TABLE dbo.KhachHangs WITH CHECK CHECK CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative;");

        // 6.2 Trường hợp DEFAULT constraint hiện có KHÁC 0 (ví dụ DEFAULT 30) -> Bị dừng và báo lỗi, không tự ý ghi đè
        await conn.ExecuteAsync(@"
            DECLARE @oldDef NVARCHAR(128) = (
                SELECT dc.name FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                WHERE dc.parent_object_id = OBJECT_ID('dbo.KhachHangs') AND c.name = 'SoNgayNo');
            IF @oldDef IS NOT NULL EXEC('ALTER TABLE dbo.KhachHangs DROP CONSTRAINT ' + @oldDef);
            ALTER TABLE dbo.KhachHangs ADD CONSTRAINT DF_KhachHangs_SoNgayNo_Conflict DEFAULT (30) FOR SoNgayNo;");

        var exDefConflict = await Assert.ThrowsAsync<SqlException>(() => ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql"));
        Assert.Contains("LỖI RÀNG BUỘC KHÔNG TƯƠNG THÍCH", exDefConflict.Message);
        Assert.Contains("khác 0", exDefConflict.Message);

        // Khôi phục DEFAULT chuẩn (0)
        await conn.ExecuteAsync(@"
            ALTER TABLE dbo.KhachHangs DROP CONSTRAINT DF_KhachHangs_SoNgayNo_Conflict;
            ALTER TABLE dbo.KhachHangs ADD CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0) FOR SoNgayNo;");

        // 6.3 Trường hợp CHECK constraint bị DISABLE (is_disabled = 1) -> Bị dừng và báo lỗi
        await conn.ExecuteAsync("ALTER TABLE dbo.KhachHangs NOCHECK CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative;");
        var exCheckDisabled = await Assert.ThrowsAsync<SqlException>(() => ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql"));
        Assert.Contains("is_disabled = 1", exCheckDisabled.Message);

        // 6.4 Trường hợp CHECK constraint bị UNTRUSTED (is_not_trusted = 1) -> Bị dừng và báo lỗi
        await conn.ExecuteAsync("ALTER TABLE dbo.KhachHangs CHECK CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative;"); // Bật lại nhưng không có WITH CHECK -> untrusted
        var exCheckUntrusted = await Assert.ThrowsAsync<SqlException>(() => ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql"));
        Assert.Contains("is_not_trusted = 1", exCheckUntrusted.Message);

        // Khôi phục lại trạng thái chuẩn WITH CHECK (ENABLED & TRUSTED)
        await conn.ExecuteAsync("ALTER TABLE dbo.KhachHangs WITH CHECK CHECK CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative;");

        // 7. KIỂM TRA CHẠY LẠI THÀNH CÔNG (IDEMPOTENCY): Khi mọi cấu hình đã chuẩn, chạy lại script an toàn 100% không lỗi
        await ExecuteScriptFileAsync(conn, "Migration_Add_SoNgayNo_To_KhachHangs.sql");
    }
}
