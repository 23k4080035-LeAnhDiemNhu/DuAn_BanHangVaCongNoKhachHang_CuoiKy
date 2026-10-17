using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace BanHang.InitTool;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string server = "(localdb)\\MSSQLLocalDB";
        string database = "QuanLyBanHangCongNo";
        bool autoConfirm = false;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--server" || args[i] == "-s") && i + 1 < args.Length)
            {
                server = args[++i];
            }
            else if ((args[i] == "--database" || args[i] == "-d") && i + 1 < args.Length)
            {
                database = args[++i];
            }
            else if (args[i] == "--yes" || args[i] == "-y")
            {
                autoConfirm = true;
            }
        }

        Console.WriteLine("==================================================================");
        Console.WriteLine("        CÔNG CỤ KHỞI TẠO TÀI KHOẢN HỆ THỐNG CỤC BỘ               ");
        Console.WriteLine("==================================================================");
        Console.WriteLine($"  [!] Server đích:   {server}");
        Console.WriteLine($"  [!] Database đích: {database}");
        Console.WriteLine("------------------------------------------------------------------");
        Console.WriteLine("  QUY TẮC AN TOÀN:");
        Console.WriteLine("  - Chỉ bổ sung bảng [dbo].[NguoiDungs] nếu chưa có.");
        Console.WriteLine("  - Không ghi đè nếu tài khoản đã tồn tại.");
        Console.WriteLine("  - Giữ nguyên toàn bộ bảng và dữ liệu nghiệp vụ.");
        Console.WriteLine("==================================================================");

        if (!autoConfirm)
        {
            Console.Write("Bạn có chắc chắn muốn tiến hành trên database trên? (gõ 'yes' để tiếp tục): ");
            string? confirm = Console.ReadLine();
            if (!string.Equals(confirm?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Đã hủy thao tác. Không có thay đổi nào được thực hiện.");
                return 1;
            }
        }

        string connectionString = $"Server={server};Database={database};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("[OK] Kết nối database thành công.");

            // 1. Kiểm tra và bổ sung bảng dbo.NguoiDungs
            await EnsureNguoiDungsTableAsync(connection);

            var hasher = new PasswordHasher<object>();
            var dummyUser = new object();

            // 2. Xử lý tài khoản nvbh
            bool nvbhTonTai = await KiemTraTonTaiAsync(connection, "nvbh");
            if (nvbhTonTai)
            {
                Console.WriteLine("[INFO] Tài khoản 'nvbh' đã tồn tại. Giữ nguyên, không ghi đè.");
            }
            else
            {
                Console.WriteLine("\n--- Khởi tạo tài khoản Nhân viên bán hàng (nvbh) ---");
                string matKhauNvbh = ReadPasswordWithConfirmation("Nhập mật khẩu cho 'nvbh': ");
                string hashNvbh = hasher.HashPassword(dummyUser, matKhauNvbh);

                await connection.ExecuteAsync(@"
                    INSERT INTO dbo.NguoiDungs (TenDangNhap, MatKhauHash, HoTen, VaiTro, TrangThai, NgayTao)
                    VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @VaiTro, @TrangThai, GETDATE())",
                    new
                    {
                        TenDangNhap = "nvbh",
                        MatKhauHash = hashNvbh,
                        HoTen = "Nhân viên bán hàng",
                        VaiTro = "NhanVienBanHang",
                        TrangThai = true
                    });
                Console.WriteLine("[OK] Đã tạo thành công tài khoản 'nvbh' (VaiTro: NhanVienBanHang).");
            }

            // 3. Xử lý tài khoản ktcn
            bool ktcnTonTai = await KiemTraTonTaiAsync(connection, "ktcn");
            if (ktcnTonTai)
            {
                Console.WriteLine("[INFO] Tài khoản 'ktcn' đã tồn tại. Giữ nguyên, không ghi đè.");
            }
            else
            {
                Console.WriteLine("\n--- Khởi tạo tài khoản Kế toán công nợ (ktcn) ---");
                string matKhauKtcn = ReadPasswordWithConfirmation("Nhập mật khẩu cho 'ktcn': ");
                string hashKtcn = hasher.HashPassword(dummyUser, matKhauKtcn);

                await connection.ExecuteAsync(@"
                    INSERT INTO dbo.NguoiDungs (TenDangNhap, MatKhauHash, HoTen, VaiTro, TrangThai, NgayTao)
                    VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @VaiTro, @TrangThai, GETDATE())",
                    new
                    {
                        TenDangNhap = "ktcn",
                        MatKhauHash = hashKtcn,
                        HoTen = "Kế toán công nợ",
                        VaiTro = "KeToanCongNo",
                        TrangThai = true
                    });
                Console.WriteLine("[OK] Đã tạo thành công tài khoản 'ktcn' (VaiTro: KeToanCongNo).");
            }

            Console.WriteLine("\n==================================================================");
            Console.WriteLine("HOÀN TẤT KHỞI TẠO TÀI KHOẢN AN TOÀN!");
            Console.WriteLine("Toàn bộ bảng và dữ liệu nghiệp vụ khác được giữ nguyên 100%.");
            Console.WriteLine("==================================================================");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[LỖI] Quá trình khởi tạo thất bại: {ex.Message}");
            Console.ResetColor();
            return 2;
        }
    }

    private static async Task EnsureNguoiDungsTableAsync(SqlConnection connection)
    {
        var tableExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE name = 'NguoiDungs' AND schema_id = SCHEMA_ID('dbo')");

        if (tableExists == 0)
        {
            Console.WriteLine("[INFO] Bảng [dbo].[NguoiDungs] chưa tồn tại. Đang tạo bảng...");
            string createTableSql = @"
                CREATE TABLE dbo.NguoiDungs (
                    NguoiDungId     INT IDENTITY(1,1)   NOT NULL,
                    TenDangNhap     NVARCHAR(50)        NOT NULL,
                    MatKhauHash     NVARCHAR(256)       NOT NULL,
                    HoTen           NVARCHAR(100)       NOT NULL,
                    VaiTro          NVARCHAR(50)        NOT NULL,
                    TrangThai       BIT                 NOT NULL DEFAULT (1),
                    NgayTao         DATETIME2           NOT NULL DEFAULT (GETDATE()),

                    CONSTRAINT PK_NguoiDungs PRIMARY KEY CLUSTERED (NguoiDungId),
                    CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap),
                    CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'NhanVienBanHang', N'KeToanCongNo'))
                );";
            await connection.ExecuteAsync(createTableSql);
            Console.WriteLine("[OK] Đã tạo bảng [dbo].[NguoiDungs] với ràng buộc UNIQUE và CHECK vai trò.");
        }
        else
        {
            Console.WriteLine("[INFO] Bảng [dbo].[NguoiDungs] đã tồn tại. Đang kiểm tra cấu trúc schema...");
            string checkColumnsSql = @"
                SELECT 
                    SUM(CASE WHEN COLUMN_NAME = 'NguoiDungId' THEN 1 ELSE 0 END) AS HasId,
                    SUM(CASE WHEN COLUMN_NAME = 'TenDangNhap' THEN 1 ELSE 0 END) AS HasTenDangNhap,
                    SUM(CASE WHEN COLUMN_NAME = 'MatKhauHash' THEN 1 ELSE 0 END) AS HasMatKhauHash,
                    SUM(CASE WHEN COLUMN_NAME = 'HoTen' THEN 1 ELSE 0 END) AS HasHoTen,
                    SUM(CASE WHEN COLUMN_NAME = 'VaiTro' THEN 1 ELSE 0 END) AS HasVaiTro,
                    SUM(CASE WHEN COLUMN_NAME = 'TrangThai' THEN 1 ELSE 0 END) AS HasTrangThai,
                    SUM(CASE WHEN COLUMN_NAME = 'NgayTao' THEN 1 ELSE 0 END) AS HasNgayTao
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'NguoiDungs';";

            var cols = await connection.QuerySingleAsync(checkColumnsSql);
            if (cols.HasId == 0 || cols.HasTenDangNhap == 0 || cols.HasMatKhauHash == 0 ||
                cols.HasHoTen == 0 || cols.HasVaiTro == 0 || cols.HasTrangThai == 0 || cols.HasNgayTao == 0)
            {
                throw new InvalidOperationException("Bảng dbo.NguoiDungs đã tồn tại nhưng thiếu các cột bắt buộc. Hãy kiểm tra lại schema database trước khi tiếp tục.");
            }

            // Đảm bảo ràng buộc UNIQUE
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_NguoiDungs_TenDangNhap')
                BEGIN
                    ALTER TABLE dbo.NguoiDungs ADD CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap);
                END");

            // Đảm bảo ràng buộc CHECK
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NguoiDungs_VaiTro')
                BEGIN
                    ALTER TABLE dbo.NguoiDungs ADD CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'NhanVienBanHang', N'KeToanCongNo'));
                END");

            Console.WriteLine("[OK] Cấu trúc bảng [dbo].[NguoiDungs] hợp lệ và đầy đủ ràng buộc.");
        }
    }

    private static async Task<bool> KiemTraTonTaiAsync(SqlConnection connection, string tenDangNhap)
    {
        string normalized = tenDangNhap.Trim().ToLowerInvariant();
        int count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.NguoiDungs WHERE LOWER(TRIM(TenDangNhap)) = @ten",
            new { ten = normalized });
        return count > 0;
    }

    private static string ReadPasswordWithConfirmation(string prompt)
    {
        while (true)
        {
            string pass1 = ReadPasswordHidden(prompt);
            if (string.IsNullOrWhiteSpace(pass1))
            {
                Console.WriteLine("Mật khẩu không được để trống. Vui lòng nhập lại.");
                continue;
            }
            if (pass1.Length < 6)
            {
                Console.WriteLine("Mật khẩu phải có ít nhất 6 ký tự. Vui lòng nhập lại.");
                continue;
            }

            string pass2 = ReadPasswordHidden("Xác nhận lại mật khẩu: ");
            if (pass1 != pass2)
            {
                Console.WriteLine("Mật khẩu xác nhận không khớp! Vui lòng nhập lại từ đầu.\n");
                continue;
            }

            return pass1;
        }
    }

    private static string ReadPasswordHidden(string prompt)
    {
        Console.Write(prompt);

        if (Console.IsInputRedirected)
        {
            string redirected = Console.ReadLine() ?? string.Empty;
            Console.WriteLine();
            return redirected.Trim();
        }

        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        return sb.ToString();
    }
}
