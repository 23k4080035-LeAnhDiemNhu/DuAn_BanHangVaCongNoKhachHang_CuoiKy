using System;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Use Case xử lý nghiệp vụ đọc chi tiết hóa đơn
/// - Kiểm tra quyền hạn phía máy chủ: Cả NhanVienBanHang và KeToanCongNo đều được phép
/// - Trả về dữ liệu chi tiết hóa đơn gồm snapshot dòng hàng và lịch sử phân bổ
/// </summary>
public class XemChiTietHoaDonUseCase : IXemChiTietHoaDonUseCase
{
    private readonly IHoaDonRepository _hoaDonRepository;
    private readonly ICurrentUserService _currentUserService;

    public XemChiTietHoaDonUseCase(
        IHoaDonRepository hoaDonRepository,
        ICurrentUserService currentUserService)
    {
        _hoaDonRepository = hoaDonRepository ?? throw new ArgumentNullException(nameof(hoaDonRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<ChiTietHoaDonDto?> ExecuteAsync(int hoaDonId)
    {
        // 1. Kiểm tra quyền hạn phía máy chủ: cả NhanVienBanHang và KeToanCongNo đều được xem
        await AuthorizeHelper.RequireRoleAsync(
            _currentUserService,
            VaiTroConstants.NhanVienBanHang,
            VaiTroConstants.KeToanCongNo);

        // 2. Kiểm tra tham số đầu vào
        if (hoaDonId <= 0)
        {
            return null;
        }

        // 3. Đọc dữ liệu từ Repository
        return await _hoaDonRepository.LayChiTietHoaDonAsync(hoaDonId);
    }
}
