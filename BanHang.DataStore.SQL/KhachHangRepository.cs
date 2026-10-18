using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai IKhachHangRepository truy xuất dữ liệu từ SQL Server bằng Dapper
/// </summary>
public class KhachHangRepository : IKhachHangRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public KhachHangRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync()
    {
        // Truy vấn danh sách khách hàng, tổng hợp trước ConLai của hóa đơn để tránh nhân đôi khi JOIN
        const string sql = @"
            SELECT 
                k.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                k.SoDienThoai,
                k.Email,
                k.DiaChi,
                k.HanMucCongNo,
                k.TrangThai,
                ISNULL(hd.TongConLai, 0) AS SoDuCongNo
            FROM dbo.KhachHangs k
            LEFT JOIN (
                SELECT KhachHangId, SUM(ConLai) AS TongConLai
                FROM dbo.HoaDons
                WHERE ConLai > 0
                GROUP BY KhachHangId
            ) hd ON k.KhachHangId = hd.KhachHangId
            ORDER BY k.MaKhachHang ASC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<KhachHang>(sql);
    }

    public async Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId)
    {
        const string sql = @"
            SELECT 
                k.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                k.SoDienThoai,
                k.Email,
                k.DiaChi,
                k.HanMucCongNo,
                k.TrangThai,
                ISNULL(hd.TongConLai, 0) AS SoDuCongNo
            FROM dbo.KhachHangs k
            LEFT JOIN (
                SELECT KhachHangId, SUM(ConLai) AS TongConLai
                FROM dbo.HoaDons
                WHERE ConLai > 0
                GROUP BY KhachHangId
            ) hd ON k.KhachHangId = hd.KhachHangId
            WHERE k.KhachHangId = @khachHangId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<KhachHang>(sql, new { khachHangId });
    }

    public async Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.KhachHangs
            WHERE MaKhachHang = @maKhachHang
              AND (@loaiTruKhachHangId IS NULL OR KhachHangId <> @loaiTruKhachHangId);";

        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { maKhachHang, loaiTruKhachHangId });
        return count > 0;
    }

    public async Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId)
    {
        const string sql = @"
            SELECT ISNULL(SUM(TongTien - DaThanhToan), 0)
            FROM dbo.HoaDons
            WHERE KhachHangId = @khachHangId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<decimal>(sql, new { khachHangId });
    }

    public async Task<string> LayMaKhachHangTiepTheoAsync()
    {
        const string sql = @"
            SELECT ISNULL(MAX(CAST(SUBSTRING(MaKhachHang, 3, LEN(MaKhachHang) - 2) AS INT)), 0) + 1
            FROM dbo.KhachHangs
            WHERE MaKhachHang LIKE 'KH[0-9]%' 
              AND ISNUMERIC(SUBSTRING(MaKhachHang, 3, LEN(MaKhachHang) - 2)) = 1;";

        using var connection = _connectionFactory.CreateConnection();
        var nextNum = await connection.ExecuteScalarAsync<int>(sql);
        if (nextNum <= 0) nextNum = 1;
        return $"KH{nextNum:D3}";
    }

    public async Task<TaoKhachHangResult> ThemKhachHangAsync(TaoKhachHangRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
        {
            await dbConn.OpenAsync();
        }
        else if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // 1. Khóa ứng dụng cấp mã khách hàng để tránh đụng độ đồng thời
            const string sqlAppLock = @"
                EXEC sp_getapplock 
                    @Resource = 'QuanLyBanHang_CapMaKhachHang', 
                    @LockMode = 'Exclusive', 
                    @LockOwner = 'Transaction', 
                    @DbPrincipal = 'public';";
            await connection.ExecuteAsync(sqlAppLock, transaction: transaction);

            // 2. Xác định mã khách hàng thực tế
            string maThucTe = request.MaKhachHang?.Trim() ?? string.Empty;

            bool canCapMaMoi = false;
            if (string.IsNullOrWhiteSpace(maThucTe))
            {
                canCapMaMoi = true;
            }
            else
            {
                // Kiểm tra xem mã yêu cầu đã tồn tại chưa
                const string sqlCheckTonTai = "SELECT COUNT(1) FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK) WHERE MaKhachHang = @MaKhachHang;";
                var daCo = await connection.ExecuteScalarAsync<int>(sqlCheckTonTai, new { MaKhachHang = maThucTe }, transaction: transaction);
                if (daCo > 0)
                {
                    canCapMaMoi = true;
                }
            }

            if (canCapMaMoi)
            {
                const string sqlMaxNum = @"
                    SELECT ISNULL(MAX(CAST(SUBSTRING(MaKhachHang, 3, LEN(MaKhachHang) - 2) AS INT)), 0) + 1
                    FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaKhachHang LIKE 'KH[0-9]%' 
                      AND ISNUMERIC(SUBSTRING(MaKhachHang, 3, LEN(MaKhachHang) - 2)) = 1;";

                var nextNum = await connection.ExecuteScalarAsync<int>(sqlMaxNum, transaction: transaction);
                if (nextNum <= 0) nextNum = 1;
                maThucTe = $"KH{nextNum:D3}";
            }

            const string sql = @"
                INSERT INTO dbo.KhachHangs 
                    (MaKhachHang, TenKhachHang, SoDienThoai, Email, DiaChi, HanMucCongNo, TrangThai, NgayTao)
                VALUES 
                    (@MaKhachHang, @TenKhachHang, @SoDienThoai, @Email, @DiaChi, @HanMucCongNo, @TrangThai, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idMoi = await connection.ExecuteScalarAsync<int>(sql, new
            {
                MaKhachHang = maThucTe,
                request.TenKhachHang,
                request.SoDienThoai,
                request.Email,
                request.DiaChi,
                request.HanMucCongNo,
                request.TrangThai
            }, transaction: transaction);

            transaction.Commit();

            return new TaoKhachHangResult
            {
                ThanhCong = true,
                KhachHangIdMoi = idMoi,
                MaKhachHangMoi = maThucTe,
                ThongBao = $"Thêm mới khách hàng '{request.TenKhachHang}' (Mã: {maThucTe}) thành công."
            };
        }
        catch (Exception ex)
        {
            try { transaction.Rollback(); } catch { }
            return new TaoKhachHangResult
            {
                ThanhCong = false,
                ThongBao = $"Lỗi khi thêm mới khách hàng: {ex.Message}"
            };
        }
    }

    public async Task<CapNhatKhachHangResult> CapNhatKhachHangAsync(CapNhatKhachHangRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
        {
            await dbConn.OpenAsync();
        }
        else if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            // 1. Khóa khách hàng theo hàng (UPDLOCK, HOLDLOCK) đồng bộ với quy ước tạo hóa đơn và thanh toán
            const string sqlLock = @"
                SELECT KhachHangId, MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai
                FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK)
                WHERE KhachHangId = @KhachHangId;";

            var khachHang = await connection.QuerySingleOrDefaultAsync<dynamic>(
                sqlLock,
                new { request.KhachHangId },
                transaction: transaction);

            if (khachHang == null)
            {
                transaction.Rollback();
                return new CapNhatKhachHangResult
                {
                    ThanhCong = false,
                    ThongBao = "Khách hàng không tồn tại trong hệ thống."
                };
            }

            // 2. Tính số dư công nợ hiện tại của khách hàng trong transaction
            const string sqlCongNo = @"
                SELECT ISNULL(SUM(TongTien - DaThanhToan), 0)
                FROM dbo.HoaDons
                WHERE KhachHangId = @KhachHangId;";

            var congNoHienTai = await connection.ExecuteScalarAsync<decimal>(
                sqlCongNo,
                new { request.KhachHangId },
                transaction: transaction);

            // 3. Quy tắc hạ hạn mức dưới công nợ hiện tại: Cần xác nhận rõ ràng từ người dùng
            if (request.HanMucCongNo < congNoHienTai && !request.XacNhanHaHanMucDuoiNo)
            {
                transaction.Rollback();
                return new CapNhatKhachHangResult
                {
                    ThanhCong = false,
                    CanXacNhanHaHanMuc = true,
                    CongNoHienTai = congNoHienTai,
                    ThongBao = $"Hạn mức mới ({request.HanMucCongNo:N0} đ) thấp hơn số dư công nợ hiện tại ({congNoHienTai:N0} đ). Khách hàng sẽ bị đánh dấu vượt hạn mức và không thể tiếp tục mua chịu cho đến khi trả bớt nợ. Bạn có chắc chắn muốn áp dụng hạn mức này không?"
                };
            }

            // 4. Cập nhật thông tin khách hàng (giữ nguyên KhachHangId và NgayTao, không ghi SoDuCongNo)
            const string sqlUpdate = @"
                UPDATE dbo.KhachHangs
                SET MaKhachHang = @MaKhachHang,
                    TenKhachHang = @TenKhachHang,
                    SoDienThoai = @SoDienThoai,
                    Email = @Email,
                    DiaChi = @DiaChi,
                    HanMucCongNo = @HanMucCongNo,
                    TrangThai = @TrangThai
                WHERE KhachHangId = @KhachHangId;";

            await connection.ExecuteAsync(sqlUpdate, new
            {
                request.KhachHangId,
                request.MaKhachHang,
                request.TenKhachHang,
                request.SoDienThoai,
                request.Email,
                request.DiaChi,
                request.HanMucCongNo,
                request.TrangThai
            }, transaction: transaction);

            transaction.Commit();

            return new CapNhatKhachHangResult
            {
                ThanhCong = true,
                CongNoHienTai = congNoHienTai,
                ThongBao = $"Cập nhật thông tin khách hàng '{request.TenKhachHang}' thành công."
            };
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            transaction.Rollback();
            return new CapNhatKhachHangResult
            {
                ThanhCong = false,
                ThongBao = $"Mã khách hàng '{request.MaKhachHang}' đã được sử dụng bởi khách hàng khác trong hệ thống."
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
        {
            await dbConn.OpenAsync();
        }
        else if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            // Khóa hàng khách hàng để đồng bộ với tạo hóa đơn và thanh toán
            const string sqlLock = @"
                SELECT KhachHangId, TenKhachHang, TrangThai
                FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK)
                WHERE KhachHangId = @KhachHangId;";

            var khachHang = await connection.QuerySingleOrDefaultAsync<dynamic>(
                sqlLock,
                new { khachHangId },
                transaction: transaction);

            if (khachHang == null)
            {
                transaction.Rollback();
                return new DoiTrangThaiKhachHangResult
                {
                    ThanhCong = false,
                    ThongBao = "Khách hàng không tồn tại trong hệ thống."
                };
            }

            const string sqlUpdate = @"
                UPDATE dbo.KhachHangs
                SET TrangThai = @trangThaiMoi
                WHERE KhachHangId = @khachHangId;";

            await connection.ExecuteAsync(sqlUpdate, new { khachHangId, trangThaiMoi }, transaction: transaction);

            transaction.Commit();

            string hanhDong = trangThaiMoi ? "Mở lại" : "Tạm khóa";
            return new DoiTrangThaiKhachHangResult
            {
                ThanhCong = true,
                TrangThaiMoi = trangThaiMoi,
                ThongBao = $"{hanhDong} khách hàng '{khachHang.TenKhachHang}' thành công."
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
