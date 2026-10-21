using System;

namespace BanHang.CoreBusiness;

/// <summary>
/// Thực thể ghi nhận nhật ký gửi email nhắc nợ phục vụ truy vết và đối soát (Audit Trail)
/// </summary>
public class NhatKyEmailNhacNo
{
    public int NhatKyId { get; set; }

    public int KhachHangId { get; set; }

    public string MaKhachHang { get; set; } = string.Empty;

    public string TenKhachHang { get; set; } = string.Empty;

    public string EmailNguoiNhan { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ email nhận thực tế khi hệ thống áp dụng cấu hình chuyển hướng kiểm thử (TestRecipientRedirect)
    /// </summary>
    public string? EmailNhanThucTe { get; set; }

    /// <summary>
    /// Tài khoản kế toán công nợ đã xem duyệt nội dung và kích hoạt gửi
    /// </summary>
    public string NguoiDuyet { get; set; } = string.Empty;

    public DateTime ThoiDiemDuyet { get; set; } = DateTime.Now;

    public DateTime ThoiDiemGui { get; set; } = DateTime.Now;

    /// <summary>
    /// Ngày chốt báo cáo tuổi nợ áp dụng cho số liệu nhắc nợ này
    /// </summary>
    public DateTime NgayChotBaoCao { get; set; }

    public string TieuDe { get; set; } = string.Empty;

    public string NoiDung { get; set; } = string.Empty;

    public GiongVanEmail GiongVan { get; set; } = GiongVanEmail.TieuChuan;

    public TrangThaiGuiEmail TrangThaiGui { get; set; } = TrangThaiGuiEmail.DangXuLy;

    /// <summary>
    /// Chi tiết phản hồi kỹ thuật từ dịch vụ (nếu có, không lưu token/mật khẩu)
    /// </summary>
    public string? ChiTietTrangThai { get; set; }

    /// <summary>
    /// Mã ID phản hồi từ dịch vụ tiếp nhận (MessageId / RequestId)
    /// </summary>
    public string? MaPhanHoiDichVu { get; set; }

    /// <summary>
    /// Khóa idempotency định danh duy nhất lượt gửi (Unique Index tại CSDL)
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Mã phê duyệt bản nháp máy chủ
    /// </summary>
    public string? MaPheDuyet { get; set; }

    /// <summary>
    /// Băm SHA256 payload dữ liệu gửi để phát hiện xung đột idempotency
    /// </summary>
    public string? PayloadHash { get; set; }

    /// <summary>
    /// Chế độ gửi: 'MoPhong' (Sandbox) hoặc 'GuiThat' (Real SMTP)
    /// </summary>
    public string CheDoGui { get; set; } = "MoPhong";

    /// <summary>
    /// Nguồn soạn thảo: 'AI', 'MauCoSan', 'MoPhong'
    /// </summary>
    public string NguonSoan { get; set; } = "AI";
}
