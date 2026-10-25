using System;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.NhatKyEmailScreen;

/// <summary>
/// DTO đại diện cho một bản ghi nhật ký email nhắc nợ hiển thị trên giao diện
/// </summary>
public class NhatKyEmailItemDto
{
    public int NhatKyId { get; set; }

    public int KhachHangId { get; set; }

    public string MaKhachHang { get; set; } = string.Empty;

    public string TenKhachHang { get; set; } = string.Empty;

    public string EmailNguoiNhan { get; set; } = string.Empty;

    public string? EmailNhanThucTe { get; set; }

    public string NguoiDuyet { get; set; } = string.Empty;

    public DateTime ThoiDiemDuyet { get; set; }

    public DateTime ThoiDiemGui { get; set; }

    public DateTime NgayChotBaoCao { get; set; }

    public string TieuDe { get; set; } = string.Empty;

    public string NoiDung { get; set; } = string.Empty;

    public GiongVanEmail GiongVan { get; set; }

    public TrangThaiGuiEmail TrangThaiGui { get; set; }

    public string? ChiTietTrangThai { get; set; }

    public string? MaPhanHoiDichVu { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? MaPheDuyet { get; set; }

    public string? PayloadHash { get; set; }

    public string CheDoGui { get; set; } = "MoPhong";

    public string NguonSoan { get; set; } = "AI";

    // --- Thuộc tính hiển thị thân thiện trên UI ---

    public string TenTrangThaiHienThi => TrangThaiGui switch
    {
        TrangThaiGuiEmail.DangXuLy => "Đang xử lý",
        TrangThaiGuiEmail.TiepNhan => "SMTP đã tiếp nhận",
        TrangThaiGuiEmail.ThatBai => "Gửi thất bại",
        TrangThaiGuiEmail.ChuaRoKetQua => "Chưa rõ kết quả",
        TrangThaiGuiEmail.GuiThu => "Gửi mô phỏng",
        _ => "Không xác định"
    };

    public string IconTrangThai => TrangThaiGui switch
    {
        TrangThaiGuiEmail.DangXuLy => "bi-hourglass-split",
        TrangThaiGuiEmail.TiepNhan => "bi-check-circle-fill",
        TrangThaiGuiEmail.ThatBai => "bi-x-circle-fill",
        TrangThaiGuiEmail.ChuaRoKetQua => "bi-question-circle-fill",
        TrangThaiGuiEmail.GuiThu => "bi-info-circle-fill",
        _ => "bi-circle"
    };

    public string BadgeClassTrangThai => TrangThaiGui switch
    {
        TrangThaiGuiEmail.DangXuLy => "bg-info-subtle text-info-emphasis border border-info-subtle",
        TrangThaiGuiEmail.TiepNhan => "bg-success-subtle text-success-emphasis border border-success-subtle",
        TrangThaiGuiEmail.ThatBai => "bg-danger-subtle text-danger-emphasis border border-danger-subtle",
        TrangThaiGuiEmail.ChuaRoKetQua => "bg-warning-subtle text-warning-emphasis border border-warning-subtle",
        TrangThaiGuiEmail.GuiThu => "bg-secondary-subtle text-secondary-emphasis border border-secondary-subtle",
        _ => "bg-light text-dark border"
    };

    public string NguonSoanHienThi => NguonSoan?.Trim() switch
    {
        "AI" or "Gemini" => "Trợ lý AI (Gemini)",
        "MauCoSan" => "Mẫu có sẵn",
        "MoPhong" => "Mô phỏng",
        _ => NguonSoan ?? "—"
    };

    public string CheDoGuiHienThi => CheDoGui?.Trim() switch
    {
        "GuiThat" or "Real" or "SMTP" => "SMTP (Gửi thật)",
        "MoPhong" or "Dev" or "Sandbox" => "Mô phỏng (Sandbox / Dev)",
        "TestRedirect" => "Chuyển hướng test",
        _ => CheDoGui ?? "—"
    };
}
