using System;
using System.Threading.Tasks;

namespace BanHang.UseCases.EmailNhacNoScreen;

public interface IXemDuLieuNhacNoKhachHangUseCase
{
    Task<DuLieuKhachHangNhacNoDto> ExecuteAsync(int khachHangId, DateTime ngayChotBaoCao);
}
