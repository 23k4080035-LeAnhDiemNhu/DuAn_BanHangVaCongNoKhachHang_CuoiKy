using System.Threading.Tasks;

namespace BanHang.UseCases.NhatKyEmailScreen;

/// <summary>
/// Ca sử dụng: Xem thông tin chi tiết một bản ghi nhật ký email nhắc nợ
/// </summary>
public interface IXemChiTietNhatKyEmailUseCase
{
    Task<NhatKyEmailItemDto?> ExecuteAsync(int nhatKyId);
}
