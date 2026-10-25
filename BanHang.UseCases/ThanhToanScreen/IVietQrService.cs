namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Giao diện dịch vụ khởi tạo và xác thực mã VietQR Quick Link
/// </summary>
public interface IVietQrService
{
    VietQrSettings Settings { get; }

    /// <summary>
    /// Tạo DTO VietQR chứa link ảnh Quick Link và thông tin chuyển khoản
    /// </summary>
    VietQrThanhToanDto TaoVietQr(string maPhieuThu, string maKhachHang, decimal soTien);

    /// <summary>
    /// Chuẩn hóa nội dung chuyển khoản: Không dấu, không ký tự đặc biệt, viết hoa, tối đa 50 ký tự
    /// Ví dụ: THU PT001 KH001
    /// </summary>
    string ChuanHoaNoiDung(string maPhieuThu, string maKhachHang);

    /// <summary>
    /// Dựng lại nội dung chuyển khoản theo chuẩn THU {MaPhieuThu} {MaKhachHang}
    /// </summary>
    string TaoNoiDungChuyenKhoan(string maPhieuThu, string maKhachHang);

    /// <summary>
    /// Kiểm tra số tiền hợp lệ theo chuẩn VietQR: Số nguyên VND, > 0, tối đa 13 chữ số
    /// </summary>
    (bool HopLe, string? Loi, long SoTienNguyen) KiemTraSoTienVietQr(decimal soTien);
}
