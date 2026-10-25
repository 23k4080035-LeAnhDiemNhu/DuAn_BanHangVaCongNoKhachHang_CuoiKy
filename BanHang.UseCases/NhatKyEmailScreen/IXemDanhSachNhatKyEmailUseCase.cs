using System.Threading.Tasks;

namespace BanHang.UseCases.NhatKyEmailScreen;

/// <summary>
/// Ca sử dụng: Xem danh sách nhật ký email nhắc nợ có phân trang và bộ lọc
/// </summary>
public interface IXemDanhSachNhatKyEmailUseCase
{
    Task<KetQuaDanhSachNhatKyEmailDto> ExecuteAsync(BoLocNhatKyEmailDto boLoc);
}
