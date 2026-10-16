using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai ISanPhamRepository truy xuất dữ liệu sản phẩm từ SQL Server bằng Dapper
/// </summary>
public class SanPhamRepository : ISanPhamRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SanPhamRepository>? _logger;

    public SanPhamRepository(ISqlConnectionFactory connectionFactory, ILogger<SanPhamRepository>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    public async Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync()
    {
        const string sql = @"
            SELECT 
                SanPhamId,
                MaSanPham,
                TenSanPham,
                DonViTinh,
                DonGia,
                SoLuongTon,
                MoTa,
                HinhAnh,
                NgayTao,
                TrangThai
            FROM dbo.SanPhams
            WHERE TrangThai = 1
            ORDER BY TenSanPham ASC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SanPham>(sql);
    }

    public async Task<IEnumerable<SanPham>> LayDanhSachTatCaSanPhamAsync()
    {
        const string sql = @"
            SELECT 
                SanPhamId,
                MaSanPham,
                TenSanPham,
                DonViTinh,
                DonGia,
                SoLuongTon,
                MoTa,
                HinhAnh,
                NgayTao,
                TrangThai
            FROM dbo.SanPhams
            ORDER BY SanPhamId DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SanPham>(sql);
    }

    public async Task<SanPham?> LaySanPhamTheoIdAsync(int sanPhamId)
    {
        const string sql = @"
            SELECT 
                SanPhamId,
                MaSanPham,
                TenSanPham,
                DonViTinh,
                DonGia,
                SoLuongTon,
                MoTa,
                HinhAnh,
                NgayTao,
                TrangThai
            FROM dbo.SanPhams
            WHERE SanPhamId = @SanPhamId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SanPham>(sql, new { SanPhamId = sanPhamId });
    }

    public async Task<bool> KiemTraTrungMaSanPhamAsync(string maSanPham)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.SanPhams 
            WHERE MaSanPham = @MaSanPham;";

        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { MaSanPham = maSanPham });
        return count > 0;
    }

    public async Task<TaoSanPhamResult> ThemSanPhamAsync(TaoSanPhamRequest request)
    {
        const string sql = @"
            INSERT INTO dbo.SanPhams (
                MaSanPham,
                TenSanPham,
                DonViTinh,
                DonGia,
                MoTa,
                HinhAnh,
                TrangThai
            )
            VALUES (
                @MaSanPham,
                @TenSanPham,
                @DonViTinh,
                @DonGia,
                @MoTa,
                @HinhAnh,
                @TrangThai
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var idMoi = await connection.ExecuteScalarAsync<int>(sql, new
            {
                request.MaSanPham,
                request.TenSanPham,
                request.DonViTinh,
                request.DonGia,
                request.MoTa,
                request.HinhAnh,
                request.TrangThai
            });

            return new TaoSanPhamResult
            {
                ThanhCong = true,
                SanPhamIdMoi = idMoi,
                ThongBao = $"Thêm mới sản phẩm '{request.TenSanPham}' (Mã: {request.MaSanPham}) thành công."
            };
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            _logger?.LogWarning(ex, "Trùng mã sản phẩm khi thêm mới: {MaSanPham}", request.MaSanPham);
            return new TaoSanPhamResult
            {
                ThanhCong = false,
                ThongBao = $"Mã sản phẩm '{request.MaSanPham}' đã được sử dụng. Vui lòng chọn mã khác.",
                LoiTheoTruong = new Dictionary<string, string>
                {
                    { "MaSanPham", $"Mã sản phẩm '{request.MaSanPham}' đã tồn tại trong hệ thống." }
                }
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi không xác định khi thêm sản phẩm: {MaSanPham}", request.MaSanPham);
            return new TaoSanPhamResult
            {
                ThanhCong = false,
                ThongBao = "Đã xảy ra lỗi trong quá trình lưu dữ liệu sản phẩm. Vui lòng thử lại sau."
            };
        }
    }

    public async Task<CapNhatSanPhamResult> CapNhatSanPhamAsync(CapNhatSanPhamRequest request)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            {
                await sqlConn.OpenAsync();
            }

            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // 1. Khóa hàng và kiểm tra sự tồn tại của sản phẩm
            const string sqlLock = @"
                SELECT SanPhamId, MaSanPham, TenSanPham, DonGia, TrangThai
                FROM dbo.SanPhams WITH (UPDLOCK, HOLDLOCK)
                WHERE SanPhamId = @SanPhamId;";

            var spHienTai = await connection.QuerySingleOrDefaultAsync<SanPham>(
                sqlLock, new { request.SanPhamId }, transaction: transaction);

            if (spHienTai == null)
            {
                transaction.Rollback();
                return new CapNhatSanPhamResult
                {
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy thông tin sản phẩm cần cập nhật trong hệ thống."
                };
            }

            // 2. Cập nhật thông tin: giữ nguyên SanPhamId, MaSanPham, NgayTao và SoLuongTon
            const string sqlUpdate = @"
                UPDATE dbo.SanPhams
                SET TenSanPham = @TenSanPham,
                    DonViTinh = @DonViTinh,
                    DonGia = @DonGia,
                    MoTa = @MoTa,
                    HinhAnh = @HinhAnh,
                    TrangThai = @TrangThai
                WHERE SanPhamId = @SanPhamId;";

            await connection.ExecuteAsync(sqlUpdate, new
            {
                request.TenSanPham,
                request.DonViTinh,
                request.DonGia,
                request.MoTa,
                request.HinhAnh,
                request.TrangThai,
                request.SanPhamId
            }, transaction: transaction);

            transaction.Commit();

            return new CapNhatSanPhamResult
            {
                ThanhCong = true,
                ThongBao = $"Cập nhật sản phẩm '{request.TenSanPham}' thành công."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi cập nhật sản phẩm ID {SanPhamId}", request.SanPhamId);
            return new CapNhatSanPhamResult
            {
                ThanhCong = false,
                ThongBao = "Đã xảy ra lỗi trong quá trình cập nhật sản phẩm. Vui lòng thử lại sau."
            };
        }
    }

    public async Task<DoiTrangThaiSanPhamResult> DoiTrangThaiSanPhamAsync(int sanPhamId, bool trangThaiMoi)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            {
                await sqlConn.OpenAsync();
            }

            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            const string sqlLock = @"
                SELECT SanPhamId, MaSanPham, TenSanPham, TrangThai
                FROM dbo.SanPhams WITH (UPDLOCK, HOLDLOCK)
                WHERE SanPhamId = @SanPhamId;";

            var sp = await connection.QuerySingleOrDefaultAsync<SanPham>(
                sqlLock, new { SanPhamId = sanPhamId }, transaction: transaction);

            if (sp == null)
            {
                transaction.Rollback();
                return new DoiTrangThaiSanPhamResult
                {
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy sản phẩm trong hệ thống."
                };
            }

            const string sqlUpdate = @"
                UPDATE dbo.SanPhams
                SET TrangThai = @TrangThaiMoi
                WHERE SanPhamId = @SanPhamId;";

            await connection.ExecuteAsync(sqlUpdate, new
            {
                TrangThaiMoi = trangThaiMoi,
                SanPhamId = sanPhamId
            }, transaction: transaction);

            transaction.Commit();

            string hanhDong = trangThaiMoi ? "Mở bán lại" : "Ngừng bán";
            return new DoiTrangThaiSanPhamResult
            {
                ThanhCong = true,
                TrangThaiMoi = trangThaiMoi,
                ThongBao = $"{hanhDong} sản phẩm '{sp.TenSanPham}' (Mã: {sp.MaSanPham}) thành công."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi thay đổi trạng thái sản phẩm ID {SanPhamId}", sanPhamId);
            return new DoiTrangThaiSanPhamResult
            {
                ThanhCong = false,
                ThongBao = "Đã xảy ra lỗi khi thay đổi trạng thái sản phẩm. Vui lòng thử lại sau."
            };
        }
    }
}
