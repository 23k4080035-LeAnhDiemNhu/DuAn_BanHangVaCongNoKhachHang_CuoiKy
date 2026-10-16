using System.Data;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai IHoaDonRepository truy xuất dữ liệu từ SQL Server bằng Dapper
/// </summary>
public class HoaDonRepository : IHoaDonRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public HoaDonRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync()
    {
        // Truy vấn danh sách hóa đơn kết hợp thông tin khách hàng.
        // Tuyệt đối không JOIN trực tiếp với bảng ChiTietHoaDons hay PhanBoThanhToans để tránh nhân đôi dòng.
        const string sql = @"
            SELECT 
                h.HoaDonId,
                h.MaHoaDon,
                h.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                h.NgayLap,
                h.HanThanhToan,
                h.TongTien,
                h.DaThanhToan,
                h.ConLai,
                h.TrangThai,
                h.GhiChu
            FROM dbo.HoaDons h
            INNER JOIN dbo.KhachHangs k ON h.KhachHangId = k.KhachHangId
            ORDER BY h.NgayLap DESC, h.HoaDonId DESC;";

        // Tạo và tự động giải phóng kết nối cho mỗi lần truy vấn
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<HoaDonListItemDto>(sql);
    }

    public async Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
        }
        else
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            // 1. Khóa khách hàng theo hàng (UPDLOCK, HOLDLOCK) để tuần tự hóa việc kiểm tra hạn mức và công nợ.
            // Điều này đảm bảo hai yêu cầu đồng thời cho cùng một khách hàng không thể cùng lúc vượt qua hạn mức.
            const string sqlLockKhachHang = @"
                SELECT KhachHangId, MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai
                FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK)
                WHERE KhachHangId = @KhachHangId;";

            var khachHang = await connection.QuerySingleOrDefaultAsync<KhachHangDbDto>(
                sqlLockKhachHang,
                new { request.KhachHangId },
                transaction: transaction);

            if (khachHang == null)
            {
                transaction.Rollback();
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = "Khách hàng không tồn tại trong hệ thống."
                };
            }

            if (!khachHang.TrangThai)
            {
                transaction.Rollback();
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = $"Khách hàng '{khachHang.TenKhachHang}' hiện đang bị tạm khóa hoặc ngừng hoạt động."
                };
            }

            // 2. Kiểm tra tính duy nhất của Mã hóa đơn trước khi lưu
            const string sqlCheckMaHoaDon = @"
                SELECT COUNT(1) 
                FROM dbo.HoaDons WITH (UPDLOCK, HOLDLOCK) 
                WHERE MaHoaDon = @MaHoaDon;";

            var countMa = await connection.ExecuteScalarAsync<int>(
                sqlCheckMaHoaDon,
                new { request.MaHoaDon },
                transaction: transaction);

            if (countMa > 0)
            {
                transaction.Rollback();
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = $"Mã hóa đơn '{request.MaHoaDon}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác."
                };
            }

            // 3. Đọc lại danh sách sản phẩm trong transaction để lấy giá hiện hành và kiểm tra trạng thái bán
            var sanPhamIds = request.ChiTietHoaDons.Select(c => c.SanPhamId).Distinct().ToList();
            const string sqlSanPhams = @"
                SELECT SanPhamId, MaSanPham, TenSanPham, DonViTinh, DonGia, TrangThai
                FROM dbo.SanPhams
                WHERE SanPhamId IN @sanPhamIds;";

            var dbSanPhams = (await connection.QueryAsync<SanPhamDbDto>(
                sqlSanPhams,
                new { sanPhamIds },
                transaction: transaction)).ToDictionary(s => s.SanPhamId);

            // Kiểm tra từng sản phẩm trong chi tiết hóa đơn
            foreach (var item in request.ChiTietHoaDons)
            {
                if (!dbSanPhams.TryGetValue(item.SanPhamId, out var sp))
                {
                    transaction.Rollback();
                    return new TaoHoaDonResult
                    {
                        ThanhCong = false,
                        ThongBao = $"Sản phẩm ID {item.SanPhamId} không tồn tại trong danh mục."
                    };
                }

                if (!sp.TrangThai)
                {
                    transaction.Rollback();
                    return new TaoHoaDonResult
                    {
                        ThanhCong = false,
                        ThongBao = $"Sản phẩm '{sp.TenSanPham}' đã ngừng kinh doanh."
                    };
                }

                // Kiểm tra nếu đơn giá phía máy chủ đã thay đổi so với giá hiển thị trên màn hình người dùng
                if (sp.DonGia != item.DonGiaHienThi)
                {
                    transaction.Rollback();
                    return new TaoHoaDonResult
                    {
                        ThanhCong = false,
                        GiaBiThayDoi = true,
                        ThongBao = $"Đơn giá của sản phẩm '{sp.TenSanPham}' đã thay đổi từ {item.DonGiaHienThi:N0} đ thành {sp.DonGia:N0} đ trên hệ thống. Vui lòng kiểm tra lại đơn giá trước khi lưu."
                    };
                }
            }

            // 4. Máy chủ tính tổng tiền hóa đơn bằng decimal từ đơn giá DB snapshot
            decimal tongTienMoi = 0;
            foreach (var item in request.ChiTietHoaDons)
            {
                var sp = dbSanPhams[item.SanPhamId];
                tongTienMoi += sp.DonGia * item.SoLuong;
            }

            // 5. Tính công nợ hiện tại của khách hàng trong transaction
            const string sqlCongNoHienTai = @"
                SELECT ISNULL(SUM(TongTien - DaThanhToan), 0)
                FROM dbo.HoaDons
                WHERE KhachHangId = @KhachHangId;";

            var congNoHienTai = await connection.ExecuteScalarAsync<decimal>(
                sqlCongNoHienTai,
                new { request.KhachHangId },
                transaction: transaction);

            // 6. Kiểm tra quy tắc hạn mức tín dụng: CongNoHienTai + TongHoaDonMoi <= HanMucCongNo
            // Bằng hạn mức được phép; vượt hạn mức bị từ chối
            if (congNoHienTai + tongTienMoi > khachHang.HanMucCongNo)
            {
                transaction.Rollback();
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = $"Hóa đơn vượt quá hạn mức công nợ của khách hàng. Công nợ hiện tại: {congNoHienTai:N0} đ, Hóa đơn mới: {tongTienMoi:N0} đ, Tổng sau tạo: {congNoHienTai + tongTienMoi:N0} đ, Hạn mức tối đa: {khachHang.HanMucCongNo:N0} đ."
                };
            }

            // Kiểm tra hạn thanh toán hợp lệ
            if (!request.HanThanhToan.HasValue || request.HanThanhToan.Value.Date < request.NgayLap.Date)
            {
                transaction.Rollback();
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = "Hạn thanh toán không hợp lệ hoặc trước ngày lập hóa đơn."
                };
            }

            // 7. Ghi nhận Header hóa đơn
            // Lưu ý: Tuyệt đối không chèn cột ConLai vì đây là computed column PERSISTED
            const string sqlInsertHoaDon = @"
                INSERT INTO dbo.HoaDons 
                    (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
                VALUES 
                    (@MaHoaDon, @KhachHangId, @NgayLap, @HanThanhToan, @TongTien, 0, 0, @GhiChu, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var hoaDonId = await connection.ExecuteScalarAsync<int>(
                sqlInsertHoaDon,
                new
                {
                    request.MaHoaDon,
                    request.KhachHangId,
                    request.NgayLap,
                    HanThanhToan = request.HanThanhToan.Value,
                    TongTien = tongTienMoi,
                    request.GhiChu
                },
                transaction: transaction);

            // 8. Ghi nhận từng dòng ChiTietHoaDons (snapshot TenSanPham, DonViTinh, DonGia)
            // Lưu ý: Tuyệt đối không chèn cột ThanhTien vì đây là computed column PERSISTED
            const string sqlInsertChiTiet = @"
                INSERT INTO dbo.ChiTietHoaDons 
                    (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES 
                    (@HoaDonId, @SanPhamId, @TenSanPham, @DonViTinh, @SoLuong, @DonGia);";

            foreach (var item in request.ChiTietHoaDons)
            {
                var sp = dbSanPhams[item.SanPhamId];
                await connection.ExecuteAsync(
                    sqlInsertChiTiet,
                    new
                    {
                        HoaDonId = hoaDonId,
                        sp.SanPhamId,
                        sp.TenSanPham,
                        sp.DonViTinh,
                        item.SoLuong,
                        sp.DonGia
                    },
                    transaction: transaction);
            }

            // 9. Toàn bộ thao tác thành công -> Commit transaction
            transaction.Commit();

            return new TaoHoaDonResult
            {
                ThanhCong = true,
                HoaDonIdMoi = hoaDonId,
                TongTien = tongTienMoi,
                ThongBao = "Tạo hóa đơn bán chịu thành công."
            };
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
        {
            try { transaction.Rollback(); } catch { }
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = $"Mã hóa đơn '{request.MaHoaDon}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác."
            };
        }
        catch (Exception)
        {
            try { transaction.Rollback(); } catch { }
            throw;
        }
    }

    private sealed class KhachHangDbDto
    {
        public int KhachHangId { get; set; }
        public string MaKhachHang { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public decimal HanMucCongNo { get; set; }
        public bool TrangThai { get; set; }
    }

    private sealed class SanPhamDbDto
    {
        public int SanPhamId { get; set; }
        public string MaSanPham { get; set; } = string.Empty;
        public string TenSanPham { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public bool TrangThai { get; set; }
    }
}
