using System;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BanHang.UseCases.EmailNhacNoScreen;

public class GuiEmailNhacNoUseCase : IGuiEmailNhacNoUseCase
{
    private readonly IXemDuLieuNhacNoKhachHangUseCase _xemDuLieuUseCase;
    private readonly IEmailSender _emailSender;
    private readonly INhatKyEmailRepository _nhatKyRepo;
    private readonly IDuyetEmailStore _duyetStore;
    private readonly IEmailIdempotencyStore _idempotencyStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GuiEmailNhacNoUseCase> _logger;

    public GuiEmailNhacNoUseCase(
        IXemDuLieuNhacNoKhachHangUseCase xemDuLieuUseCase,
        IEmailSender emailSender,
        INhatKyEmailRepository nhatKyRepo,
        IDuyetEmailStore duyetStore,
        IEmailIdempotencyStore idempotencyStore,
        ICurrentUserService currentUserService,
        ILogger<GuiEmailNhacNoUseCase>? logger = null)
    {
        _xemDuLieuUseCase = xemDuLieuUseCase ?? throw new ArgumentNullException(nameof(xemDuLieuUseCase));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _nhatKyRepo = nhatKyRepo ?? throw new ArgumentNullException(nameof(nhatKyRepo));
        _duyetStore = duyetStore ?? throw new ArgumentNullException(nameof(duyetStore));
        _idempotencyStore = idempotencyStore ?? throw new ArgumentNullException(nameof(idempotencyStore));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? NullLogger<GuiEmailNhacNoUseCase>.Instance;
    }

    public GuiEmailNhacNoUseCase(
        IXemDuLieuNhacNoKhachHangUseCase xemDuLieuUseCase,
        IEmailSender emailSender,
        INhatKyEmailRepository nhatKyRepo,
        IDuyetEmailStore duyetStore,
        ICurrentUserService currentUserService)
        : this(xemDuLieuUseCase, emailSender, nhatKyRepo, duyetStore, new MemoryEmailIdempotencyStore(), currentUserService, NullLogger<GuiEmailNhacNoUseCase>.Instance)
    {
    }

    public async Task<KetQuaGuiEmailDto> ExecuteAsync(YeuCauGuiEmailDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Phân quyền: Chỉ Kế toán công nợ
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);
        var currentUser = await _currentUserService.GetTenDangNhapAsync() ?? "KeToanCongNo";

        // 2. Chống gửi trùng Idempotency phía máy chủ (chống bấm lặp & request đồng thời)
        string? idempotencyKey = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? request.IdempotencyKey.Trim()
            : (!string.IsNullOrWhiteSpace(request.MaPheDuyet) ? request.MaPheDuyet.Trim() : null);

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var payloadHash = _idempotencyStore.TinhPayloadHash(request);

            // Kiểm tra cache đã hoàn tất (đồng thời kiểm tra phân quyền sở hữu và xung đột payload)
            var cached = await _idempotencyStore.LayKetQuaDaHoanThanhAsync(idempotencyKey, currentUser, payloadHash);
            if (cached != null)
            {
                return cached;
            }

            // Đăng ký In-Flight Task (Không gọi sender trong factory)
            var tcs = new TaskCompletionSource<KetQuaGuiEmailDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            var (isLeader, inFlightTask) = _idempotencyStore.BatDauXuLyInFlight(idempotencyKey, currentUser, payloadHash, tcs);

            if (!isLeader)
            {
                // Follower chờ tác vụ đang thực thi của Leader, không gọi sender lần 2!
                return await inFlightTask;
            }

            try
            {
                // Kiểm tra Idempotency bền vững trong CSDL (phục vụ trường hợp tạo lại service / service re-instantiation)
                var durableRecord = await _nhatKyRepo.LayTheoIdempotencyKeyAsync(idempotencyKey);
                if (durableRecord != null)
                {
                    if (!string.Equals(durableRecord.NguoiDuyet, currentUser, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Khóa Idempotency này thuộc sở hữu của một người dùng khác.");
                    }
                    if (!string.Equals(durableRecord.PayloadHash, payloadHash, StringComparison.Ordinal))
                    {
                        throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối do xung đột Idempotency: Khóa này đã được sử dụng trước đó cho một yêu cầu có nội dung, người nhận hoặc tham số khác.");
                    }
                    if (durableRecord.TrangThaiGui == TrangThaiGuiEmail.DangXuLy)
                    {
                        throw new QuyTacNghiepVuException("Yêu cầu gửi email này đang được xử lý bởi hệ thống. Vui lòng không gửi lặp lại.");
                    }
                    if (durableRecord.TrangThaiGui == TrangThaiGuiEmail.ChuaRoKetQua)
                    {
                        throw new QuyTacNghiepVuException("Yêu cầu gửi email này trước đó ở trạng thái Chưa rõ kết quả (nguy cơ timeout hoặc gián đoạn mạng). Hệ thống không tự động gửi lại để tránh gửi trùng thư cho khách hàng.");
                    }

                    var durableResult = new KetQuaGuiEmailDto
                    {
                        ThanhCong = durableRecord.TrangThaiGui == TrangThaiGuiEmail.GuiThu || durableRecord.TrangThaiGui == TrangThaiGuiEmail.TiepNhan,
                        TrangThai = durableRecord.TrangThaiGui,
                        ThongBao = durableRecord.ChiTietTrangThai ?? "Yêu cầu gửi email đã được ghi nhận trước đó (Durable Idempotent response).",
                        NhatKyId = durableRecord.NhatKyId
                    };

                    _idempotencyStore.LuuKetQuaHoanThanh(idempotencyKey, currentUser, payloadHash, durableResult);
                    tcs.TrySetResult(durableResult);
                    return durableResult;
                }

                var result = await XuLyGuiEmailHopLeAsync(request, currentUser, cancellationToken);
                _idempotencyStore.LuuKetQuaHoanThanh(idempotencyKey, currentUser, payloadHash, result);
                tcs.TrySetResult(result);
                return result;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _idempotencyStore.KetThucInFlight(idempotencyKey);
            }
        }
        else
        {
            return await XuLyGuiEmailHopLeAsync(request, currentUser, cancellationToken);
        }
    }

    private async Task<KetQuaGuiEmailDto> XuLyGuiEmailHopLeAsync(YeuCauGuiEmailDto request, string currentUser, CancellationToken cancellationToken)
    {
        // 2. Kiểm tra duyệt tại máy chủ: Không chỉ tin boolean DaDuyet do client gửi
        if (string.IsNullOrWhiteSpace(request.MaPheDuyet))
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Bản nháp chưa được phê duyệt tại máy chủ (hệ thống không tin cậy cờ boolean DaDuyet do client gửi lên).");
        }

        var snapshot = await _duyetStore.LayBanDuyetAsync(request.MaPheDuyet);
        if (snapshot == null)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Mã phê duyệt không tồn tại hoặc đã hết hạn (sau 30 phút). Vui lòng kiểm tra và duyệt lại.");
        }

        // Kiểm tra chủ sở hữu bản duyệt: Người kích hoạt gửi phải đúng là người đã duyệt bản nháp
        if (!string.Equals(snapshot.NguoiDuyet, currentUser, StringComparison.OrdinalIgnoreCase))
        {
            throw new QuyTacNghiepVuException(
                $"Yêu cầu gửi email bị từ chối: Bản duyệt này thuộc sở hữu của kế toán '{snapshot.NguoiDuyet}'. Bạn ('{currentUser}') không có quyền kích hoạt gửi bản duyệt của người khác.");
        }

        // Kiểm tra thời hạn hiệu lực của mã duyệt
        if (DateTime.Now > snapshot.ThoiDiemHetHan)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Mã phê duyệt đã hết hạn hiệu lực (sau 30 phút). Vui lòng kiểm tra và duyệt lại.");
        }

        if (snapshot.DaSuDung)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Bản duyệt này đã được sử dụng trước đó. Không được gửi lặp lại bản duyệt cũ.");
        }

        // Kiểm tra khớp tuyệt đối nội dung đã duyệt: Thay đổi người nhận, tiêu đề, nội dung, KH hoặc ngày chốt làm duyệt cũ mất hiệu lực
        if (request.KhachHangId != snapshot.KhachHangId)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Khách hàng không khớp với bản đã duyệt. Phê duyệt cũ mất hiệu lực.");
        }

        if (request.NgayChotBaoCao.Date != snapshot.NgayChotBaoCao.Date)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Ngày chốt báo cáo không khớp với bản đã duyệt. Phê duyệt cũ mất hiệu lực.");
        }

        if (!string.Equals(request.EmailNguoiNhan?.Trim(), snapshot.EmailNguoiNhan.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Địa chỉ email người nhận đã bị thay đổi sau khi duyệt. Phê duyệt cũ mất hiệu lực, vui lòng duyệt lại.");
        }

        if (!string.Equals(request.TieuDe?.Trim(), snapshot.TieuDe.Trim(), StringComparison.Ordinal))
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Tiêu đề email đã bị chỉnh sửa sau khi duyệt. Phê duyệt cũ mất hiệu lực, vui lòng duyệt lại.");
        }

        if (!string.Equals(request.NoiDung?.Trim(), snapshot.NoiDung.Trim(), StringComparison.Ordinal))
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Nội dung email đã bị chỉnh sửa sau khi duyệt. Phê duyệt cũ mất hiệu lực, vui lòng duyệt lại.");
        }

        // 3. Đối soát lại trạng thái công nợ hiện tại tại đúng ngày chốt trên máy chủ
        var duLieuHienTai = await _xemDuLieuUseCase.ExecuteAsync(request.KhachHangId, request.NgayChotBaoCao.Date);

        if (!string.Equals(duLieuHienTai.HashDuLieuCongNo, snapshot.HashDuLieuCongNo, StringComparison.Ordinal))
        {
            throw new QuyTacNghiepVuException(
                "Dữ liệu công nợ của khách hàng đã thay đổi so với thời điểm phê duyệt bản nháp (do có hóa đơn mới, thanh toán mới hoặc điều chỉnh số dư). Vui lòng làm mới và duyệt lại bản nháp trước khi gửi.");
        }

        // 4. Tiêu thụ mã duyệt nguyên tử (Atomic Consumption) qua Store
        bool daTieuThu = await _duyetStore.TryConsumeBanDuyetAsync(snapshot.MaPheDuyet);
        if (!daTieuThu)
        {
            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Mã phê duyệt này đã được tiêu thụ bởi một lượt gửi khác hoặc đã hết hạn. Không được gửi lặp lại.");
        }

        return await ThucHienGuiVaLuuNhatKyAsync(request, duLieuHienTai, snapshot, currentUser, cancellationToken);
    }

    private async Task<KetQuaGuiEmailDto> ThucHienGuiVaLuuNhatKyAsync(
        YeuCauGuiEmailDto request,
        DuLieuKhachHangNhacNoDto duLieuHienTai,
        BanDuyetSnapshot snapshot,
        string currentUser,
        CancellationToken cancellationToken)
    {
        string idempotencyKey = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? request.IdempotencyKey.Trim()
            : $"SEND-{snapshot.MaPheDuyet}";
        string payloadHash = _idempotencyStore.TinhPayloadHash(request);

        // 1. Kiểm tra tồn tại trong CSDL bền vững trước
        var existingRecord = await _nhatKyRepo.LayTheoIdempotencyKeyAsync(idempotencyKey);
        if (existingRecord != null)
        {
            if (!string.Equals(existingRecord.NguoiDuyet, currentUser, StringComparison.OrdinalIgnoreCase))
            {
                throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Khóa này thuộc sở hữu của một người dùng khác.");
            }
            if (!string.Equals(existingRecord.PayloadHash, payloadHash, StringComparison.Ordinal))
            {
                throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Xung đột Idempotency khi cùng khóa nhưng khác tham số.");
            }
            if (existingRecord.TrangThaiGui == TrangThaiGuiEmail.DangXuLy)
            {
                throw new QuyTacNghiepVuException("Yêu cầu gửi email này đang được xử lý bởi hệ thống. Vui lòng không gửi lặp lại.");
            }
            if (existingRecord.TrangThaiGui == TrangThaiGuiEmail.ChuaRoKetQua)
            {
                throw new QuyTacNghiepVuException("Yêu cầu gửi email này trước đó ở trạng thái Chưa rõ kết quả (nguy cơ timeout hoặc gián đoạn mạng). Hệ thống không tự động gửi lại để tránh gửi trùng thư cho khách hàng.");
            }
            return new KetQuaGuiEmailDto
            {
                ThanhCong = existingRecord.TrangThaiGui == TrangThaiGuiEmail.GuiThu || existingRecord.TrangThaiGui == TrangThaiGuiEmail.TiepNhan,
                TrangThai = existingRecord.TrangThaiGui,
                ThongBao = existingRecord.ChiTietTrangThai ?? "Yêu cầu gửi email đã được ghi nhận trước đó (Idempotent response).",
                NhatKyId = existingRecord.NhatKyId
            };
        }

        // 2. Ghi nhận yêu cầu trước khi gọi sender (Pre-registration: TrangThaiGui = DangXuLy)
        string cheDoGuiKhoiTao = _emailSender.GetType().Name.Contains("Smtp", StringComparison.OrdinalIgnoreCase)
            ? "GuiThat"
            : (_emailSender.GetType().Name.Contains("Dev", StringComparison.OrdinalIgnoreCase) ||
               _emailSender.GetType().Name.Contains("Mock", StringComparison.OrdinalIgnoreCase) ||
               _emailSender.GetType().Name.Contains("Fake", StringComparison.OrdinalIgnoreCase)
                ? "MoPhong"
                : (request.CheDoGui ?? "MoPhong"));

        var preReg = new NhatKyEmailNhacNo
        {
            KhachHangId = duLieuHienTai.KhachHangId,
            MaKhachHang = duLieuHienTai.MaKhachHang,
            TenKhachHang = duLieuHienTai.TenKhachHang,
            EmailNguoiNhan = snapshot.EmailNguoiNhan.Trim(),
            EmailNhanThucTe = null,
            NguoiDuyet = snapshot.NguoiDuyet,
            ThoiDiemDuyet = snapshot.ThoiDiemDuyet,
            ThoiDiemGui = DateTime.Now,
            NgayChotBaoCao = request.NgayChotBaoCao.Date,
            TieuDe = snapshot.TieuDe.Trim(),
            NoiDung = snapshot.NoiDung.Trim(),
            GiongVan = request.GiongVan,
            TrangThaiGui = TrangThaiGuiEmail.DangXuLy,
            ChiTietTrangThai = "Hệ thống đang phát lệnh gửi tới dịch vụ email...",
            MaPhanHoiDichVu = null,
            IdempotencyKey = idempotencyKey,
            MaPheDuyet = snapshot.MaPheDuyet,
            PayloadHash = payloadHash,
            CheDoGui = cheDoGuiKhoiTao,
            NguonSoan = request.NguonSoan
        };

        int nhatKyId;
        try
        {
            nhatKyId = await _nhatKyRepo.GhiNhanYeuCauGuiAsync(preReg);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi ghi nhận pre-registration cho IdempotencyKey '{Key}'. Kiểm tra trùng lặp CSDL.", idempotencyKey);

            bool isDuplicateKey = false;
            for (Exception? curr = ex; curr != null; curr = curr.InnerException)
            {
                if (curr.GetType().Name.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
                {
                    var numProp = curr.GetType().GetProperty("Number");
                    if (numProp != null && numProp.GetValue(curr) is int num && (num == 2601 || num == 2627))
                    {
                        isDuplicateKey = true;
                        break;
                    }
                }

                if (curr.Message.Contains("UX_NhatKyEmailNhacNos_IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
                    curr.Message.Contains("2601", StringComparison.OrdinalIgnoreCase) ||
                    curr.Message.Contains("2627", StringComparison.OrdinalIgnoreCase) ||
                    curr.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                    curr.Message.Contains("trùng lặp khóa", StringComparison.OrdinalIgnoreCase) ||
                    curr.Message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase))
                {
                    isDuplicateKey = true;
                    break;
                }
            }

            var dup = await _nhatKyRepo.LayTheoIdempotencyKeyAsync(idempotencyKey);
            if (dup != null)
            {
                // Kiểm tra lại NguoiDuyet và PayloadHash trước khi trả kết quả
                if (!string.Equals(dup.NguoiDuyet, currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối: Khóa Idempotency này thuộc sở hữu của một người dùng khác.");
                }
                if (!string.Equals(dup.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    throw new QuyTacNghiepVuException("Yêu cầu gửi email bị từ chối do xung đột Idempotency: Khóa này đã được sử dụng trước đó cho một yêu cầu có nội dung, người nhận hoặc tham số khác.");
                }

                if (dup.TrangThaiGui == TrangThaiGuiEmail.DangXuLy)
                {
                    throw new QuyTacNghiepVuException("Yêu cầu gửi email này đang được xử lý đồng thời bởi hệ thống. Vui lòng không bấm lặp.");
                }
                if (dup.TrangThaiGui == TrangThaiGuiEmail.ChuaRoKetQua)
                {
                    throw new QuyTacNghiepVuException("Yêu cầu gửi email này trước đó ở trạng thái Chưa rõ kết quả (nguy cơ timeout hoặc gián đoạn mạng). Hệ thống không tự động gửi lại để tránh gửi trùng thư cho khách hàng.");
                }
                return new KetQuaGuiEmailDto
                {
                    ThanhCong = dup.TrangThaiGui == TrangThaiGuiEmail.GuiThu || dup.TrangThaiGui == TrangThaiGuiEmail.TiepNhan,
                    TrangThai = dup.TrangThaiGui,
                    ThongBao = dup.ChiTietTrangThai ?? "Yêu cầu gửi email đã được ghi nhận trước đó (Idempotent response).",
                    NhatKyId = dup.NhatKyId
                };
            }

            // Nếu không phải lỗi trùng khóa, không mô tả thành "đang xử lý đồng thời"
            if (!isDuplicateKey)
            {
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi ghi nhận yêu cầu gửi email: {ex.Message}", ex);
            }

            throw new QuyTacNghiepVuException("Yêu cầu gửi email bị tranh chấp khóa Idempotency trong CSDL. Vui lòng thử lại.");
        }

        // 3. Gọi dịch vụ gửi email với bắt lỗi timeout / mất kết nối
        KetQuaGuiEmailRawDto rawResult;
        try
        {
            rawResult = await _emailSender.GuiEmailAsync(new ThongTinEmailGuiDto
            {
                NguoiNhanEmail = snapshot.EmailNguoiNhan.Trim(),
                TenNguoiNhan = duLieuHienTai.TenKhachHang,
                TieuDe = snapshot.TieuDe.Trim(),
                NoiDung = snapshot.NoiDung.Trim()
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mất kết nối hoặc timeout sau khi phát lệnh gửi cho khách hàng {KhachHangId}.", duLieuHienTai.KhachHangId);
            try
            {
                await _nhatKyRepo.CapNhatTrangThaiGuiAsync(
                    nhatKyId,
                    TrangThaiGuiEmail.ChuaRoKetQua,
                    $"Mất kết nối hoặc timeout sau khi phát lệnh gửi: {ex.Message}. Tuyệt đối không tự động gửi lại.",
                    null,
                    snapshot.EmailNguoiNhan.Trim(),
                    cheDoGuiKhoiTao);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Lỗi cập nhật CSDL khi ghi nhận Chưa rõ kết quả.");
            }

            return new KetQuaGuiEmailDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ChuaRoKetQua,
                ThongBao = "Mất kết nối hoặc quá thời gian chờ phản hồi từ dịch vụ email. Trạng thái hiện tại: Chưa rõ kết quả. Để đảm bảo an toàn, hệ thống tuyệt đối KHÔNG tự động gửi lại.",
                NhatKyId = nhatKyId
            };
        }

        // 4. Chuyển trạng thái nguyên tử tại CSDL sau khi dịch vụ phản hồi (Bảo toàn EmailNguoiNhan dự kiến, cập nhật EmailNhanThucTe & CheDoGui)
        string? canhBaoAudit = null;
        string actualEmail = !string.IsNullOrWhiteSpace(rawResult.EmailNhanThucTe)
            ? rawResult.EmailNhanThucTe.Trim()
            : snapshot.EmailNguoiNhan.Trim();

        string actualCheDoGui = !string.IsNullOrWhiteSpace(rawResult.CheDoGui)
            ? rawResult.CheDoGui.Trim()
            : (rawResult.TrangThai == TrangThaiGuiEmail.GuiThu ? "MoPhong" : cheDoGuiKhoiTao);

        try
        {
            await _nhatKyRepo.CapNhatTrangThaiGuiAsync(
                nhatKyId,
                rawResult.TrangThai,
                rawResult.ThongBaoChiTiet,
                rawResult.MaPhanHoiDichVu,
                actualEmail,
                actualCheDoGui);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật nhật ký audit sau khi dịch vụ email đã tiếp nhận.");
            canhBaoAudit = "[Cảnh báo] Dịch vụ email đã tiếp nhận nhưng lỗi cập nhật nhật ký audit CSDL. Tuyệt đối KHÔNG tự động gửi lại để tránh trùng lặp email cho khách hàng.";
        }

        string thongBao = rawResult.TrangThai switch
        {
            TrangThaiGuiEmail.GuiThu => "[Chế độ mô phỏng - Sandbox] Đã ghi nhận yêu cầu gửi thử nghiệm thành công (không phát thư thật ra ngoài Internet).",
            TrangThaiGuiEmail.TiepNhan => $"Dịch vụ email đã tiếp nhận yêu cầu gửi. Mã phản hồi: {rawResult.MaPhanHoiDichVu ?? "N/A"}. Lưu ý: Dịch vụ xác nhận đã tiếp nhận phát thư, không bảo đảm người nhận đã đọc.",
            TrangThaiGuiEmail.ThatBai => $"Gửi email không thành công: {rawResult.ThongBaoChiTiet}",
            TrangThaiGuiEmail.ChuaRoKetQua => "Dịch vụ đã phát lệnh nhưng chưa rõ kết quả cuối cùng. Hệ thống không tự động gửi lại.",
            _ => "Dịch vụ đã xử lý nhưng chưa rõ kết quả phát thư cuối cùng."
        };

        if (!string.IsNullOrEmpty(canhBaoAudit))
        {
            thongBao = $"{thongBao} {canhBaoAudit}";
        }

        return new KetQuaGuiEmailDto
        {
            ThanhCong = rawResult.ThanhCong,
            ThongBao = thongBao,
            TrangThai = rawResult.TrangThai,
            NhatKyId = nhatKyId,
            CanhBaoAudit = canhBaoAudit
        };
    }
}
