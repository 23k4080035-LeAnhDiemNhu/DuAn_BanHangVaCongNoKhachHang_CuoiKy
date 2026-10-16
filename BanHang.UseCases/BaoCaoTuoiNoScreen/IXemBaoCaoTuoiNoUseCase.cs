using System;
using System.Threading.Tasks;

namespace BanHang.UseCases.BaoCaoTuoiNoScreen;

public interface IXemBaoCaoTuoiNoUseCase
{
    Task<BaoCaoTuoiNoResult> ExecuteAsync(DateTime ngayBaoCao);
}
