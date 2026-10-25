using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.NhatKyEmailScreen;

public class XemChiTietNhatKyEmailUseCase : IXemChiTietNhatKyEmailUseCase
{
    private readonly INhatKyEmailRepository _nhatKyRepo;
    private readonly ICurrentUserService _currentUserService;

    public XemChiTietNhatKyEmailUseCase(
        INhatKyEmailRepository nhatKyRepo,
        ICurrentUserService currentUserService)
    {
        _nhatKyRepo = nhatKyRepo ?? throw new ArgumentNullException(nameof(nhatKyRepo));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<NhatKyEmailItemDto?> ExecuteAsync(int nhatKyId)
    {
        if (nhatKyId <= 0) return null;

        // 1. Phân quyền nghiệp vụ máy chủ: Chỉ Kế toán công nợ
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        // 2. Đọc bản ghi từ CSDL qua Repository
        var entity = await _nhatKyRepo.LayTheoIdAsync(nhatKyId);
        if (entity == null) return null;

        var dto = XemDanhSachNhatKyEmailUseCase.MapToDto(entity);

        // 3. Khử độc / rút gọn thông báo kỹ thuật, bảo đảm không lộ stack trace hay bí mật
        dto.ChiTietTrangThai = LamSachThongBaoKyThuat(dto.ChiTietTrangThai);

        return dto;
    }

    public static string? LamSachThongBaoKyThuat(string? rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage)) return rawMessage;

        // Cắt bỏ stack trace nếu có
        string cleaned = rawMessage;
        int atIndex = cleaned.IndexOf("   at ", StringComparison.OrdinalIgnoreCase);
        if (atIndex > 0)
        {
            cleaned = cleaned.Substring(0, atIndex).Trim();
        }

        int stackTraceIndex = cleaned.IndexOf("--- End of stack trace", StringComparison.OrdinalIgnoreCase);
        if (stackTraceIndex > 0)
        {
            cleaned = cleaned.Substring(0, stackTraceIndex).Trim();
        }

        // Lọc bỏ pattern mật khẩu, token nếu xuất hiện
        cleaned = Regex.Replace(cleaned, @"(password|pwd|token|apikey|secret)\s*=\s*[^;\s]+", "$1=***", RegexOptions.IgnoreCase);

        return cleaned.Trim();
    }
}
