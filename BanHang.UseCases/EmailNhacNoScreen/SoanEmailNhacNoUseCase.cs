using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.AI;

namespace BanHang.UseCases.EmailNhacNoScreen;

public class SoanEmailNhacNoUseCase : ISoanEmailNhacNoUseCase
{
    private readonly IXemDuLieuNhacNoKhachHangUseCase _xemDuLieuUseCase;
    private readonly IAiEmailDraftingService _aiDraftingService;
    private readonly ICurrentUserService _currentUserService;

    public SoanEmailNhacNoUseCase(
        IXemDuLieuNhacNoKhachHangUseCase xemDuLieuUseCase,
        IAiEmailDraftingService aiDraftingService,
        ICurrentUserService currentUserService)
    {
        _xemDuLieuUseCase = xemDuLieuUseCase ?? throw new ArgumentNullException(nameof(xemDuLieuUseCase));
        _aiDraftingService = aiDraftingService ?? throw new ArgumentNullException(nameof(aiDraftingService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<BanNhapEmailDto> ExecuteAsync(YeuCauSoanEmailDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Kiểm tra phân quyền: Chỉ Kế toán công nợ
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        // 2. Lấy dữ liệu thật từ máy chủ tại đúng ngày chốt báo cáo (không tin số liệu client)
        var duLieu = await _xemDuLieuUseCase.ExecuteAsync(request.KhachHangId, request.NgayChotBaoCao.Date);

        // Xác định email nhận
        var emailNhan = !string.IsNullOrWhiteSpace(request.EmailTuyChinh)
            ? request.EmailTuyChinh.Trim()
            : (duLieu.Email?.Trim() ?? string.Empty);

        string? canhBaoEmail = null;
        if (!DuLieuKhachHangNhacNoDto.KiemTraEmailHopLe(emailNhan))
        {
            canhBaoEmail = string.IsNullOrWhiteSpace(emailNhan)
                ? "Khách hàng chưa có địa chỉ email. Hệ thống vẫn tạo bản nháp nhưng bạn không thể gửi email cho đến khi nhập địa chỉ hợp lệ."
                : $"Địa chỉ email '{emailNhan}' không đúng định dạng. Vui lòng kiểm tra lại trước khi gửi.";
        }

        string? canhBaoTienChuaPhanBo = null;
        if (duLieu.TienThuChuaPhanBo > 0)
        {
            canhBaoTienChuaPhanBo = $"Lưu ý: Doanh nghiệp hiện có tổng cộng {duLieu.TienThuChuaPhanBo:N0} đ tiền thu chưa phân bổ trên hệ thống. Số tiền này không tự động cấn trừ vào các hóa đơn trên.";
        }

        // 3. Chuẩn bị dữ liệu tối thiểu và an toàn để gửi cho AI (Sanitize để chống prompt injection)
        var sanitizedTenKhachHang = SanitizeUserInput(duLieu.TenKhachHang);

        var aiInput = new DuLieuSoanEmailDto
        {
            KhachHangId = duLieu.KhachHangId,
            MaKhachHang = duLieu.MaKhachHang,
            TenKhachHang = sanitizedTenKhachHang,
            NgayChotBaoCao = duLieu.NgayChotBaoCao,
            TongCongNo = duLieu.TongCongNo,
            TuoiNo_0_30 = duLieu.TuoiNo_0_30,
            TuoiNo_31_60 = duLieu.TuoiNo_31_60,
            TuoiNo_61_90 = duLieu.TuoiNo_61_90,
            TuoiNo_Tren_90 = duLieu.TuoiNo_Tren_90,
            TienThuChuaPhanBo = duLieu.TienThuChuaPhanBo,
            GiongVan = request.GiongVan,
            YeuCauChinhCachViet = request.YeuCauChinhCachViet,
            DanhSachHoaDon = duLieu.DanhSachHoaDon.Select(h => new HoaDonTomTatDto
            {
                MaHoaDon = h.MaHoaDon,
                NgayLap = h.NgayLap,
                HanThanhToan = h.HanThanhToan,
                DuNoTaiNgayChot = h.DuNoTaiNgayChot,
                SoNgayQuaHan = h.SoNgayQuaHan
            }).ToList()
        };

        // 4. Gọi AI hỗ trợ soạn thảo hoặc Dùng mẫu có sẵn khi người dùng chọn
        KetQuaSoanThaoAiDto aiResult;
        string? thongBaoLoiAi = null;

        if (request.ChiDungMauCoSan)
        {
            aiResult = TaoBanNhapTheoTemplate(aiInput);
            aiResult.NguonSoan = "Mẫu có sẵn";
        }
        else
        {
            try
            {
                aiResult = await _aiDraftingService.SoanThaoEmailAsync(aiInput, cancellationToken);
                if (!aiResult.ThanhCong || string.IsNullOrWhiteSpace(aiResult.NoiDung))
                {
                    thongBaoLoiAi = aiResult.ThongBaoLoi ?? "Dịch vụ AI không trả về nội dung. Bạn có thể tự soạn tiếp hoặc chọn 'Dùng mẫu có sẵn'.";
                }
            }
            catch (Exception ex)
            {
                thongBaoLoiAi = $"Không thể kết nối AI: {ex.Message}. Bạn có thể tự soạn tiếp hoặc chọn 'Dùng mẫu có sẵn'.";
                aiResult = new KetQuaSoanThaoAiDto
                {
                    ThanhCong = false,
                    ThongBaoLoi = thongBaoLoiAi,
                    NguonSoan = "AI thật"
                };
            }
        }

        return new BanNhapEmailDto
        {
            KhachHangId = duLieu.KhachHangId,
            MaKhachHang = duLieu.MaKhachHang,
            TenKhachHang = duLieu.TenKhachHang,
            EmailNguoiNhan = emailNhan,
            NgayChotBaoCao = duLieu.NgayChotBaoCao,
            GiongVan = request.GiongVan,
            TieuDe = aiResult.TieuDe,
            NoiDung = aiResult.NoiDung,
            TongCongNo = duLieu.TongCongNo,
            TienThuChuaPhanBo = duLieu.TienThuChuaPhanBo,
            HashDuLieuCongNo = duLieu.HashDuLieuCongNo,
            CanhBaoEmail = canhBaoEmail,
            CanhBaoTienChuaPhanBo = canhBaoTienChuaPhanBo,
            NguonSoan = aiResult.NguonSoan,
            ThongBaoLoiAi = thongBaoLoiAi
        };
    }

    /// <summary>
    /// Tạo bản nháp chuẩn xác từ template nghiệp vụ để đảm bảo số liệu 100% chính xác
    /// </summary>
    public static KetQuaSoanThaoAiDto TaoBanNhapTheoTemplate(DuLieuSoanEmailDto duLieu)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();

        decimal tongDenVaQuaHan = duLieu.DanhSachHoaDon
            .Where(h => !h.LaTrongHan || h.HanThanhToan.Date <= duLieu.NgayChotBaoCao.Date)
            .Sum(h => h.DuNoTaiNgayChot);
        decimal tongChuaDenHan = duLieu.DanhSachHoaDon
            .Where(h => h.LaTrongHan && h.HanThanhToan.Date > duLieu.NgayChotBaoCao.Date)
            .Sum(h => h.DuNoTaiNgayChot);

        string loiChao = duLieu.GiongVan switch
        {
            GiongVanEmail.NheNhang => $"Kính gửi Quý khách hàng {duLieu.TenKhachHang},\n\nLời đầu tiên, chúng tôi xin chân thành cảm ơn Quý khách đã luôn tin tưởng và đồng hành cùng doanh nghiệp chúng tôi trong suốt thời gian qua.",
            GiongVanEmail.KhanTruong => $"Kính gửi Ban Giám đốc và Bộ phận Kế toán Quý khách hàng {duLieu.TenKhachHang},\n\nChúng tôi gửi thư này để thông báo về tình trạng công nợ quá hạn cần được ưu tiên thanh toán dứt điểm.",
            _ => $"Kính gửi Quý khách hàng {duLieu.TenKhachHang},\n\nBộ phận Kế toán công nợ xin gửi thông báo đối soát công nợ định kỳ đến Quý khách."
        };

        sb.AppendLine(loiChao);
        sb.AppendLine();
        sb.AppendLine($"Tính đến ngày chốt số liệu {duLieu.NgayChotBaoCao:dd/MM/yyyy}, tổng số dư công nợ của Quý khách theo sổ sách kế toán là: {duLieu.TongCongNo.ToString("N0", vi)} đ.");

        if (tongDenVaQuaHan > 0 && tongChuaDenHan > 0)
        {
            sb.AppendLine($"- Khoản đã đến hạn (đề nghị thanh toán): {tongDenVaQuaHan.ToString("N0", vi)} đ");
            sb.AppendLine($"- Khoản chưa đến hạn (theo dõi thanh toán): {tongChuaDenHan.ToString("N0", vi)} đ");
        }
        else if (tongDenVaQuaHan > 0 && tongChuaDenHan == 0)
        {
            sb.AppendLine($"- Toàn bộ dư nợ ({tongDenVaQuaHan.ToString("N0", vi)} đ) đã đến hạn thanh toán.");
        }
        else if (tongDenVaQuaHan == 0)
        {
            sb.AppendLine($"- Toàn bộ dư nợ ({tongChuaDenHan.ToString("N0", vi)} đ) hiện vẫn trong hạn thanh toán.");
        }

        sb.AppendLine();
        sb.AppendLine("Tổng hợp các khoản công nợ theo thời hạn:");
        sb.AppendLine($"- Nhóm 0–30 ngày (bao gồm các khoản chưa đến hạn và quá hạn đến 30 ngày): {duLieu.TuoiNo_0_30.ToString("N0", vi)} đ");
        if (duLieu.TuoiNo_31_60 > 0)
            sb.AppendLine($"- Nhóm quá hạn 31–60 ngày: {duLieu.TuoiNo_31_60.ToString("N0", vi)} đ");
        if (duLieu.TuoiNo_61_90 > 0)
            sb.AppendLine($"- Nhóm quá hạn 61–90 ngày: {duLieu.TuoiNo_61_90.ToString("N0", vi)} đ");
        if (duLieu.TuoiNo_Tren_90 > 0)
            sb.AppendLine($"- Nhóm quá hạn trên 90 ngày: {duLieu.TuoiNo_Tren_90.ToString("N0", vi)} đ");

        if (duLieu.DanhSachHoaDon.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Danh sách các hóa đơn có số dư tại ngày chốt:");
            sb.AppendLine("Mã HĐ | Hạn thanh toán | Số tiền còn nợ | Tình trạng");
            foreach (var hd in duLieu.DanhSachHoaDon)
            {
                string tinhTrang;
                if (hd.HanThanhToan.Date < duLieu.NgayChotBaoCao.Date)
                    tinhTrang = $"Quá hạn {hd.SoNgayQuaHan} ngày";
                else if (hd.HanThanhToan.Date == duLieu.NgayChotBaoCao.Date)
                    tinhTrang = "Đến hạn hôm nay";
                else
                    tinhTrang = "Chưa đến hạn";

                sb.AppendLine($"{hd.MaHoaDon} | {hd.HanThanhToan:dd/MM/yyyy} | {hd.DuNoTaiNgayChot.ToString("N0", vi)} đ | {tinhTrang}");
            }
        }

        if (duLieu.TienThuChuaPhanBo > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Ghi chú đối soát: Hệ thống hiện ghi nhận có {duLieu.TienThuChuaPhanBo.ToString("N0", vi)} đ tiền thu chưa phân bổ. Khoản này sẽ được cấn trừ theo thứ tự khi thực hiện phân bổ.");
        }

        sb.AppendLine();
        string ketThuc;
        if (tongDenVaQuaHan > 0)
        {
            ketThuc = duLieu.GiongVan switch
            {
                GiongVanEmail.NheNhang => $"Kính đề nghị Quý khách sắp xếp thanh toán khoản nợ đã đến hạn là {tongDenVaQuaHan.ToString("N0", vi)} đ. Các khoản chưa đến hạn xin vui lòng tiếp tục theo dõi thanh toán đúng hẹn.\n\nNếu có bất kỳ sai lệch nào về số liệu, xin vui lòng phản hồi lại để cùng rà soát.\n\nTrân trọng cảm ơn sự hợp tác của Quý khách!",
                GiongVanEmail.KhanTruong => $"Để đảm bảo quyền lợi duy trì hạn mức tín dụng và luồng giao dịch liên tục, kính đề nghị Quý khách ưu tiên thanh toán khoản nợ đã đến hạn ({tongDenVaQuaHan.ToString("N0", vi)} đ) trong thời gian sớm nhất.\n\nTrân trọng thông báo!",
                _ => $"Kính đề nghị Quý khách rà soát và sắp xếp thanh toán khoản nợ đã đến hạn là {tongDenVaQuaHan.ToString("N0", vi)} đ theo thỏa thuận. Các khoản chưa đến hạn xin vui lòng tiếp tục theo dõi để thanh toán đúng quy định.\n\nTrân trọng cảm ơn!"
            };
        }
        else
        {
            ketThuc = "Các khoản công nợ trên hiện đều trong hạn thanh toán. Chúng tôi gửi thông tin để Quý khách tiện theo dõi và chủ động kế hoạch tài chính.\n\nTrân trọng cảm ơn sự đồng hành của Quý khách!";
        }

        sb.AppendLine(ketThuc);
        sb.AppendLine();
        sb.AppendLine("Trân trọng,");
        sb.AppendLine("Bộ phận Kế toán Công nợ");

        string tieuDe = duLieu.GiongVan switch
        {
            GiongVanEmail.KhanTruong => $"[KHẨN] Thông báo công nợ quá hạn tính đến {duLieu.NgayChotBaoCao:dd/MM/yyyy} - {duLieu.TenKhachHang}",
            GiongVanEmail.NheNhang => $"Thư nhắc đối soát công nợ kỳ {duLieu.NgayChotBaoCao:dd/MM/yyyy} - {duLieu.TenKhachHang}",
            _ => $"Thông báo đối soát và nhắc thanh toán công nợ ngày {duLieu.NgayChotBaoCao:dd/MM/yyyy} - {duLieu.TenKhachHang}"
        };

        return new KetQuaSoanThaoAiDto
        {
            ThanhCong = true,
            TieuDe = tieuDe,
            NoiDung = sb.ToString().Trim()
        };
    }

    private static string SanitizeUserInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Bỏ các ký tự điều khiển và newlines nguy hiểm có thể phá vỡ cấu trúc prompt
        return Regex.Replace(input, @"[\r\n\t]+", " ").Trim();
    }
}
