using System.Data;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.ThanhToanScreen;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai IThanhToanRepository truy xuất dữ liệu Phiếu thu & Phân bổ FIFO bằng Dapper
/// </summary>
public class ThanhToanRepository : IThanhToanRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ThanhToanRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<ThanhToanListItemDto>> LayDanhSachThanhToanAsync()
    {
        const string sql = @"
            SELECT 
                t.ThanhToanId,
                t.MaPhieuThu,
                t.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                t.NgayThanhToan,
                t.SoTien,
                t.PhuongThuc,
                t.GhiChu,
                ISNULL(pb.TongDaPhanBo, 0) AS TongDaPhanBo,
                (t.SoTien - ISNULL(pb.TongDaPhanBo, 0)) AS TienChuaPhanBo
            FROM dbo.ThanhToans t
            INNER JOIN dbo.KhachHangs k ON t.KhachHangId = k.KhachHangId
            LEFT JOIN (
                SELECT ThanhToanId, SUM(SoTienPhanBo) AS TongDaPhanBo
                FROM dbo.PhanBoThanhToans
                GROUP BY ThanhToanId
            ) pb ON t.ThanhToanId = pb.ThanhToanId
            ORDER BY t.NgayThanhToan DESC, t.ThanhToanId DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ThanhToanListItemDto>(sql);
    }

    public async Task<IEnumerable<HoaDonNoModel>> LayDanhSachHoaDonConNoTheoKhachHangAsync(int khachHangId)
    {
        const string sql = @"
            SELECT 
                HoaDonId,
                MaHoaDon,
                NgayLap,
                TongTien,
                DaThanhToan,
                ConLai
            FROM dbo.HoaDons
            WHERE KhachHangId = @khachHangId AND ConLai > 0
            ORDER BY NgayLap ASC, HoaDonId ASC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<HoaDonNoModel>(sql, new { khachHangId });
    }

    public async Task<string> LayMaPhieuThuTiepTheoAsync()
    {
        const string sql = @"
            SELECT ISNULL(MAX(CAST(SUBSTRING(MaPhieuThu, 3, LEN(MaPhieuThu) - 2) AS INT)), 0) + 1
            FROM dbo.ThanhToans
            WHERE MaPhieuThu LIKE 'PT[0-9]%' 
              AND ISNUMERIC(SUBSTRING(MaPhieuThu, 3, LEN(MaPhieuThu) - 2)) = 1;";

        using var connection = _connectionFactory.CreateConnection();
        var nextNum = await connection.ExecuteScalarAsync<int>(sql);
        if (nextNum <= 0) nextNum = 1;
        return $"PT{nextNum:D3}";
    }

    public async Task<TaoThanhToanResult> GhiNhanThanhToanAsync(TaoThanhToanRequest request)
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
            // 1. Khóa ứng dụng cấp mã phiếu thu để tránh đụng độ đồng thời (timeout 5 giây)
            const string sqlAppLock = @"
                DECLARE @res INT;
                EXEC @res = sp_getapplock 
                    @Resource = 'QuanLyBanHang_CapMaPhieuThu', 
                    @LockMode = 'Exclusive', 
                    @LockOwner = 'Transaction', 
                    @LockTimeout = 5000,
                    @DbPrincipal = 'public';
                SELECT @res;";

            var lockResult = await connection.ExecuteScalarAsync<int>(sqlAppLock, transaction: transaction);
            if (lockResult < 0)
            {
                transaction.Rollback();
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = lockResult switch
                    {
                        -1 => "Hệ thống đang bận xử lý cấp mã phiếu thu cho giao dịch khác (quá thời gian chờ khóa 5 giây). Vui lòng thử lại sau.",
                        -3 => "Xung đột tranh chấp khóa cấp mã phiếu thu (deadlock victim). Giao dịch đã được hủy an toàn, vui lòng thử lại.",
                        _ => $"Không thể nhận khóa độc quyền để cấp mã phiếu thu (mã lỗi khóa: {lockResult}). Vui lòng thử lại."
                    }
                };
            }

            // 2. Khóa khách hàng theo hàng (UPDLOCK, HOLDLOCK) đồng bộ với quy ước tạo hóa đơn
            const string sqlLockKhachHang = @"
                SELECT KhachHangId, MaKhachHang, TenKhachHang, TrangThai
                FROM dbo.KhachHangs WITH (UPDLOCK, HOLDLOCK)
                WHERE KhachHangId = @KhachHangId;";

            var khachHang = await connection.QuerySingleOrDefaultAsync<dynamic>(
                sqlLockKhachHang,
                new { request.KhachHangId },
                transaction: transaction);

            if (khachHang == null)
            {
                transaction.Rollback();
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = "Khách hàng không tồn tại trong hệ thống."
                };
            }

            // 3. Xác định mã phiếu thu thực tế
            string maThucTe;
            bool laMaTuyChinh = !string.IsNullOrWhiteSpace(request.MaPhieuThu) 
                && !(System.Text.RegularExpressions.Regex.IsMatch(request.MaPhieuThu.Trim(), @"^PT\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

            if (laMaTuyChinh)
            {
                maThucTe = request.MaPhieuThu.Trim();
                const string sqlCheckCustom = "SELECT COUNT(1) FROM dbo.ThanhToans WITH (UPDLOCK, HOLDLOCK) WHERE MaPhieuThu = @maThucTe;";
                var countCustom = await connection.ExecuteScalarAsync<int>(sqlCheckCustom, new { maThucTe }, transaction: transaction);
                if (countCustom > 0)
                {
                    transaction.Rollback();
                    return new TaoThanhToanResult
                    {
                        ThanhCong = false,
                        ThongBao = $"Mã phiếu thu '{maThucTe}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác."
                    };
                }
            }
            else
            {
                const string sqlMaxNum = @"
                    SELECT ISNULL(MAX(CAST(SUBSTRING(MaPhieuThu, 3, LEN(MaPhieuThu) - 2) AS INT)), 0) + 1
                    FROM dbo.ThanhToans WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaPhieuThu LIKE 'PT[0-9]%' 
                      AND ISNUMERIC(SUBSTRING(MaPhieuThu, 3, LEN(MaPhieuThu) - 2)) = 1;";

                var nextNum = await connection.ExecuteScalarAsync<int>(sqlMaxNum, transaction: transaction);
                if (nextNum <= 0) nextNum = 1;
                maThucTe = $"PT{nextNum:D3}";
            }

            request.MaPhieuThu = maThucTe;

            // 3. Đọc lại danh sách hóa đơn còn nợ đủ điều kiện trong transaction có khóa UPDLOCK, HOLDLOCK
            const string sqlHoaDonConNo = @"
                SELECT 
                    HoaDonId,
                    MaHoaDon,
                    NgayLap,
                    TongTien,
                    DaThanhToan,
                    ConLai
                FROM dbo.HoaDons WITH (UPDLOCK, HOLDLOCK)
                WHERE KhachHangId = @KhachHangId AND ConLai > 0
                ORDER BY NgayLap ASC, HoaDonId ASC;";

            var hoaDons = (await connection.QueryAsync<HoaDonNoModel>(
                sqlHoaDonConNo,
                new { request.KhachHangId },
                transaction: transaction)).ToList();

            // 4. Áp dụng thuật toán FIFO thuần túy trong CoreBusiness
            var ketQuaFifo = ThuatToanFIFO.PhanBo(hoaDons, request.SoTien, request.NgayThanhToan);

            // 5. Kiểm tra phát hiện thay đổi so với bản xem trước (nếu có gửi bản xem trước)
            if (request.DanhSachXemTruoc != null)
            {
                bool khopXemTruoc = true;
                if (request.DanhSachXemTruoc.Count != ketQuaFifo.DanhSachPhanBo.Count)
                {
                    khopXemTruoc = false;
                }
                else
                {
                    for (int i = 0; i < request.DanhSachXemTruoc.Count; i++)
                    {
                        var xt = request.DanhSachXemTruoc[i];
                        var pb = ketQuaFifo.DanhSachPhanBo[i];
                        if (xt.HoaDonId != pb.HoaDonId || xt.SoTienPhanBo != pb.SoTienPhanBo)
                        {
                            khopXemTruoc = false;
                            break;
                        }
                    }
                }

                if (!khopXemTruoc)
                {
                    transaction.Rollback();
                    return new TaoThanhToanResult
                    {
                        ThanhCong = false,
                        DuLieuThayDoiSoVoiXemTruoc = true,
                        DanhSachPhanBo = ketQuaFifo.DanhSachPhanBo,
                        TongDaPhanBo = ketQuaFifo.TongDaPhanBo,
                        TienChuaPhanBo = ketQuaFifo.TienChuaPhanBo,
                        ThongBao = "Dữ liệu công nợ hóa đơn của khách hàng đã thay đổi so với bản xem trước. Vui lòng xem lại danh sách phân bổ mới trước khi xác nhận lưu."
                    };
                }
            }

            // 6. Ghi nhận Header phiếu thu vào dbo.ThanhToans
            // Kiểm tra schema: Bắt buộc phải có đủ 6 cột audit VietQR nếu là ChuyenKhoan
            const string sqlCheckVietQrCols = @"
                SELECT COUNT(1) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.ThanhToans') 
                  AND name IN ('NoiDungChuyenKhoan', 'TaiKhoanThuHuong', 'NganHangThuHuong', 'DaXacNhanTienVao', 'ThoiDiemXacNhan', 'NguoiXacNhan');";

            var vietQrColsCount = await connection.ExecuteScalarAsync<int>(sqlCheckVietQrCols, transaction: transaction);
            bool hasVietQrCols = (vietQrColsCount == 6);

            bool laChuyenKhoan = string.Equals(request.PhuongThuc, "ChuyenKhoan", StringComparison.OrdinalIgnoreCase) 
                              || string.Equals(request.PhuongThuc, "Chuyển khoản", StringComparison.OrdinalIgnoreCase);

            if (laChuyenKhoan)
            {
                if (!hasVietQrCols)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("Lỗi cấu hình cơ sở dữ liệu: Bảng dbo.ThanhToans chưa được bổ sung các cột đối soát chuyển khoản VietQR (NoiDungChuyenKhoan, TaiKhoanThuHuong, NganHangThuHuong, DaXacNhanTienVao, ThoiDiemXacNhan, NguoiXacNhan). Vui lòng áp dụng migration 'Migration_Add_VietQR_Audit_To_ThanhToans.sql' trước khi thực hiện thanh toán chuyển khoản.");
                }

                if (string.IsNullOrWhiteSpace(request.NoiDungChuyenKhoan))
                {
                    request.NoiDungChuyenKhoan = $"THU {maThucTe} {khachHang.MaKhachHang}".Trim().ToUpperInvariant();
                }
            }
            else
            {
                // Phiếu thu tiền mặt không lưu dữ liệu QR, tài khoản hoặc xác nhận chuyển khoản
                request.NoiDungChuyenKhoan = null;
                request.TaiKhoanThuHuong = null;
                request.NganHangThuHuong = null;
                request.DaXacNhanTienVao = false;
                request.ThoiDiemXacNhan = null;
                request.NguoiXacNhan = null;
            }

            int thanhToanId;
            if (hasVietQrCols)
            {
                const string sqlInsertThanhToan = @"
                    INSERT INTO dbo.ThanhToans 
                        (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu,
                         NoiDungChuyenKhoan, TaiKhoanThuHuong, NganHangThuHuong, DaXacNhanTienVao, ThoiDiemXacNhan, NguoiXacNhan, NgayTao)
                    VALUES 
                        (@MaPhieuThu, @KhachHangId, @NgayThanhToan, @SoTien, @PhuongThuc, @GhiChu,
                         @NoiDungChuyenKhoan, @TaiKhoanThuHuong, @NganHangThuHuong, @DaXacNhanTienVao, @ThoiDiemXacNhan, @NguoiXacNhan, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                thanhToanId = await connection.ExecuteScalarAsync<int>(
                    sqlInsertThanhToan,
                    new
                    {
                        request.MaPhieuThu,
                        request.KhachHangId,
                        request.NgayThanhToan,
                        request.SoTien,
                        request.PhuongThuc,
                        request.GhiChu,
                        request.NoiDungChuyenKhoan,
                        request.TaiKhoanThuHuong,
                        request.NganHangThuHuong,
                        request.DaXacNhanTienVao,
                        request.ThoiDiemXacNhan,
                        request.NguoiXacNhan
                    },
                    transaction: transaction);
            }
            else
            {
                // Chỉ chạy khi là Tiền mặt (do ChuyenKhoan đã bị chặn và rollback ở trên)
                const string sqlInsertThanhToan = @"
                    INSERT INTO dbo.ThanhToans 
                        (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu, NgayTao)
                    VALUES 
                        (@MaPhieuThu, @KhachHangId, @NgayThanhToan, @SoTien, @PhuongThuc, @GhiChu, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                thanhToanId = await connection.ExecuteScalarAsync<int>(
                    sqlInsertThanhToan,
                    new
                    {
                        request.MaPhieuThu,
                        request.KhachHangId,
                        request.NgayThanhToan,
                        request.SoTien,
                        request.PhuongThuc,
                        request.GhiChu
                    },
                    transaction: transaction);
            }

            // 7. Ghi nhận từng dòng phân bổ và cập nhật hóa đơn
            const string sqlInsertPhanBo = @"
                INSERT INTO dbo.PhanBoThanhToans 
                    (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                VALUES 
                    (@ThanhToanId, @HoaDonId, @SoTienPhanBo, GETDATE());";

            const string sqlUpdateHoaDon = @"
                UPDATE dbo.HoaDons
                SET DaThanhToan = DaThanhToan + @SoTienPhanBo,
                    TrangThai = CASE WHEN (DaThanhToan + @SoTienPhanBo) >= TongTien THEN 2 ELSE 1 END
                WHERE HoaDonId = @HoaDonId;";

            foreach (var pb in ketQuaFifo.DanhSachPhanBo)
            {
                // Thêm dòng phân bổ (NgayPhanBo là thời điểm thực tế GETDATE())
                await connection.ExecuteAsync(
                    sqlInsertPhanBo,
                    new
                    {
                        ThanhToanId = thanhToanId,
                        pb.HoaDonId,
                        pb.SoTienPhanBo
                    },
                    transaction: transaction);

                // Cập nhật DaThanhToan và TrangThai hóa đơn (Tuyệt đối không update cột computed ConLai)
                await connection.ExecuteAsync(
                    sqlUpdateHoaDon,
                    new
                    {
                        pb.HoaDonId,
                        pb.SoTienPhanBo
                    },
                    transaction: transaction);
            }

            // 8. Toàn bộ thao tác thành công -> Commit transaction
            transaction.Commit();

            return new TaoThanhToanResult
            {
                ThanhCong = true,
                ThanhToanIdMoi = thanhToanId,
                MaPhieuThu = request.MaPhieuThu,
                SoTien = request.SoTien,
                TongDaPhanBo = ketQuaFifo.TongDaPhanBo,
                TienChuaPhanBo = ketQuaFifo.TienChuaPhanBo,
                DanhSachPhanBo = ketQuaFifo.DanhSachPhanBo,
                ThongBao = "Ghi nhận phiếu thu và phân bổ công nợ thành công."
            };
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
        {
            try { transaction.Rollback(); } catch { }
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = $"Mã phiếu thu '{request.MaPhieuThu}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác."
            };
        }
        catch (Exception)
        {
            try { transaction.Rollback(); } catch { }
            throw;
        }
    }
}
