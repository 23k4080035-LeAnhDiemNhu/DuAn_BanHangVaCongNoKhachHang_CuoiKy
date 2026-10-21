using System;
using System.Collections.Generic;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.EmailNhacNoScreen;

public class HoaDonNhacNoItemDto
{
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal TongTien { get; set; }
    public decimal TongDaPhanBoCoHieuLuc { get; set; }
    public decimal DuNoTaiNgayChot => TongTien - TongDaPhanBoCoHieuLuc;
    public int SoNgayQuaHan { get; set; }
    public bool LaTrongHan => SoNgayQuaHan <= 0;
}

public class DuLieuKhachHangNhacNoDto
{
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTime NgayChotBaoCao { get; set; }
    public decimal HanMucCongNo { get; set; }
    public decimal TongCongNo { get; set; }
    public decimal TuoiNo_0_30 { get; set; }
    public decimal TuoiNo_31_60 { get; set; }
    public decimal TuoiNo_61_90 { get; set; }
    public decimal TuoiNo_Tren_90 { get; set; }
    public decimal TienThuChuaPhanBo { get; set; }
    public List<HoaDonNhacNoItemDto> DanhSachHoaDon { get; set; } = new();

    /// <summary>
    /// Mã hash vân tay trạng thái công nợ tại ngày chốt. Dùng để đối soát trước khi gửi.
    /// </summary>
    public string HashDuLieuCongNo { get; set; } = string.Empty;

    public bool EmailHopLe => KiemTraEmailHopLe(Email);

    public static bool KiemTraEmailHopLe(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email.Trim());
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}

public class YeuCauSoanEmailDto
{
    public int KhachHangId { get; set; }
    public DateTime NgayChotBaoCao { get; set; }
    public GiongVanEmail GiongVan { get; set; } = GiongVanEmail.TieuChuan;
    public string? EmailTuyChinh { get; set; }

    /// <summary>
    /// Yêu cầu chỉnh cách viết (Ví dụ: "ngắn hơn", "lịch sự hơn", "nhấn mạnh khoản quá hạn")
    /// </summary>
    public string? YeuCauChinhCachViet { get; set; }

    /// <summary>
    /// Khi người dùng chủ động chọn "Dùng mẫu có sẵn" thay vì gọi AI
    /// </summary>
    public bool ChiDungMauCoSan { get; set; } = false;
}

public class BanNhapEmailDto
{
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string EmailNguoiNhan { get; set; } = string.Empty;
    public DateTime NgayChotBaoCao { get; set; }
    public GiongVanEmail GiongVan { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public decimal TongCongNo { get; set; }
    public decimal TienThuChuaPhanBo { get; set; }
    public string HashDuLieuCongNo { get; set; } = string.Empty;

    public bool EmailHopLe => DuLieuKhachHangNhacNoDto.KiemTraEmailHopLe(EmailNguoiNhan);
    public string? CanhBaoEmail { get; set; }
    public string? CanhBaoTienChuaPhanBo { get; set; }
    public bool CoTheGui => EmailHopLe && !string.IsNullOrWhiteSpace(NoiDung) && !string.IsNullOrWhiteSpace(TieuDe);

    /// <summary>
    /// Nguồn soạn thảo: "AI thật", "Mô phỏng", "Mẫu có sẵn"
    /// </summary>
    public string NguonSoan { get; set; } = "AI thật";

    /// <summary>
    /// Thông báo nếu AI gặp lỗi hoặc timeout (nội dung cũ của người dùng được giữ nguyên)
    /// </summary>
    public string? ThongBaoLoiAi { get; set; }
}

public class YeuCauGuiEmailDto
{
    public int KhachHangId { get; set; }
    public string EmailNguoiNhan { get; set; } = string.Empty;
    public DateTime NgayChotBaoCao { get; set; }
    public GiongVanEmail GiongVan { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>
    /// Vân tay công nợ của bản nháp đã được kế toán duyệt. 
    /// Máy chủ sẽ so sánh lại với dữ liệu hiện tại trước khi gửi.
    /// </summary>
    public string HashDuLieuCongNo { get; set; } = string.Empty;

    /// <summary>
    /// Mã phê duyệt được máy chủ cấp sau khi kế toán duyệt bản nháp.
    /// Bắt buộc phải có để đảm bảo bản gửi khớp 100% bản đã duyệt.
    /// </summary>
    public string MaPheDuyet { get; set; } = string.Empty;

    /// <summary>
    /// Khóa idempotency chống gửi trùng phía máy chủ (chống bấm lặp và 2 request đồng thời)
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Cờ boolean do client gửi lên (không được tin cậy một mình, phải phối hợp kiểm tra MaPheDuyet)
    /// </summary>
    public bool DaDuyet { get; set; }

    /// <summary>
    /// Chế độ gửi: 'MoPhong' (Sandbox) hoặc 'GuiThat' (Real SMTP)
    /// </summary>
    public string CheDoGui { get; set; } = "MoPhong";

    /// <summary>
    /// Nguồn soạn thảo bản nháp
    /// </summary>
    public string NguonSoan { get; set; } = "AI thật";
}

public class KetQuaGuiEmailDto
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public TrangThaiGuiEmail TrangThai { get; set; }
    public int? NhatKyId { get; set; }

    /// <summary>
    /// Cảnh báo riêng biệt khi dịch vụ gửi email đã tiếp nhận nhưng ghi nhật ký audit vào CSDL thất bại.
    /// </summary>
    public string? CanhBaoAudit { get; set; }
}
