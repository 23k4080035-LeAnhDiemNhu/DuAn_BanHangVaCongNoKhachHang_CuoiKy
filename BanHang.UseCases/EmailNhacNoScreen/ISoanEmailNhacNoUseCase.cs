using System.Threading;
using System.Threading.Tasks;

namespace BanHang.UseCases.EmailNhacNoScreen;

public interface ISoanEmailNhacNoUseCase
{
    Task<BanNhapEmailDto> ExecuteAsync(YeuCauSoanEmailDto request, CancellationToken cancellationToken = default);
}
