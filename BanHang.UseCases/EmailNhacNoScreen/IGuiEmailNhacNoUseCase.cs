using System.Threading;
using System.Threading.Tasks;

namespace BanHang.UseCases.EmailNhacNoScreen;

public interface IGuiEmailNhacNoUseCase
{
    Task<KetQuaGuiEmailDto> ExecuteAsync(YeuCauGuiEmailDto request, CancellationToken cancellationToken = default);
}
