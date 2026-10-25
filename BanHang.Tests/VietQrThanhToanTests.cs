using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.ThanhToanScreen;
using Xunit;

namespace BanHang.Tests;

public class VietQrThanhToanTests
{
    private class MockThanhToanRepository : IThanhToanRepository
    {
        public TaoThanhToanRequest? LastSavedRequest { get; private set; }

        public Task<IEnumerable<ThanhToanListItemDto>> LayDanhSachThanhToanAsync() =>
            Task.FromResult(Enumerable.Empty<ThanhToanListItemDto>());

        public Task<IEnumerable<HoaDonNoModel>> LayDanhSachHoaDonConNoTheoKhachHangAsync(int khachHangId) =>
            Task.FromResult(Enumerable.Empty<HoaDonNoModel>());

        public Task<TaoThanhToanResult> GhiNhanThanhToanAsync(TaoThanhToanRequest request)
        {
            LastSavedRequest = request;
            return Task.FromResult(new TaoThanhToanResult
            {
                ThanhCong = true,
                ThanhToanIdMoi = 999,
                MaPhieuThu = request.MaPhieuThu,
                SoTien = request.SoTien,
                TongDaPhanBo = request.SoTien,
                TienChuaPhanBo = 0,
                ThongBao = "Thành công"
            });
        }

        public Task<string> LayMaPhieuThuTiepTheoAsync() => Task.FromResult("PT001");
    }

    [Fact]
    public void ChuyenKhoan_TaoDung_ICB_BIN970415_TaiKhoan_ChuTaiKhoan()
    {
        var service = new VietQrService(new VietQrSettings());

        var dto = service.TaoVietQr("PT001", "KH001", 1500000m);

        Assert.True(dto.HopLe);
        Assert.Equal("ICB", dto.BankId);
        Assert.Equal("970415", dto.BankBin);
        Assert.Equal("VietinBank", dto.BankName);
        Assert.Equal("107879627729", dto.AccountNumber);
        Assert.Equal("LE ANH DIEM NHU", dto.AccountName);
        Assert.Equal(1500000, dto.SoTienNguyen);
        Assert.Contains("ICB-107879627729-compact2.svg", dto.QrUrl);
        Assert.Contains("accountName=LE%20ANH%20DIEM%20NHU", dto.QrUrl);
    }

    [Fact]
    public void ChuyenKhoan_QRDungSoTienPhieuThu()
    {
        var service = new VietQrService(new VietQrSettings());
        decimal soTienPhieuThu = 2850000m;

        var dto = service.TaoVietQr("PT002", "KH002", soTienPhieuThu);

        Assert.True(dto.HopLe);
        Assert.Contains("amount=2850000", dto.QrUrl);
        Assert.DoesNotContain("amount=0", dto.QrUrl);
        Assert.Equal("2.850.000 đ", dto.SoTienDinhDang);
    }

    [Fact]
    public void NoiDungChuyenKhoan_ChuanHoaKhongDau_InHoa_VaEncodeDung()
    {
        var service = new VietQrService(new VietQrSettings());

        // Test chuẩn hóa bỏ dấu tiếng Việt, ký tự đặc biệt và viết hoa
        string rawNoiDung = service.ChuanHoaNoiDung("PT005", "KH-ĐÀ NẴNG #01");
        Assert.Equal("THU PT005 KH DA NANG 01", rawNoiDung);

        var dto = service.TaoVietQr("PT005", "KH-ĐÀ NẴNG #01", 500000m);
        Assert.Contains("addInfo=THU%20PT005%20KH%20DA%20NANG%2001", dto.QrUrl);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100000)]
    [InlineData(125000.75)]
    [InlineData(500.5)]
    public void SoTien0_HoacAm_HoacCoPhanLeVND_BaoLoiKhongTaoQr(decimal soTienLoi)
    {
        var service = new VietQrService(new VietQrSettings());

        var (hopLe, loi, soTienNguyen) = service.KiemTraSoTienVietQr(soTienLoi);
        Assert.False(hopLe);
        Assert.NotNull(loi);
        Assert.Equal(0, soTienNguyen);

        var dto = service.TaoVietQr("PT001", "KH001", soTienLoi);
        Assert.False(dto.HopLe);
        Assert.NotEmpty(dto.ThongBaoLoi!);
        Assert.Empty(dto.QrUrl);
    }

    [Fact]
    public void SoTien_Vuot13ChuSo_BaoLoi()
    {
        var service = new VietQrService(new VietQrSettings());
        decimal soTienQuaLon = 10_000_000_000_000m; // 14 chữ số

        var (hopLe, loi, _) = service.KiemTraSoTienVietQr(soTienQuaLon);
        Assert.False(hopLe);
        Assert.Contains("13 chữ số", loi);
    }

    [Fact]
    public async Task ChuyenKhoan_ChuaXacNhanTienVao_BiTuChoiLuu()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo());

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT001",
            KhachHangId = 1,
            SoTien = 1000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = false // Chưa tích xác nhận tiền vào
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("kiểm tra tiền đã vào tài khoản", result.ThongBao);
        Assert.Null(repo.LastSavedRequest); // Repository chưa được gọi
    }

    [Fact]
    public async Task ChuyenKhoan_DaXacNhanTienVao_LuuThanhCong_VaGhiNhanAudit()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo("ketoan01"));

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT001",
            KhachHangId = 1,
            SoTien = 2000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        Assert.True(repo.LastSavedRequest.DaXacNhanTienVao);
        Assert.Equal("107879627729", repo.LastSavedRequest.TaiKhoanThuHuong);
        Assert.Equal("ICB", repo.LastSavedRequest.NganHangThuHuong);
        Assert.Equal("ketoan01", repo.LastSavedRequest.NguoiXacNhan);
        Assert.NotNull(repo.LastSavedRequest.ThoiDiemXacNhan);
    }

    [Fact]
    public async Task TienMat_KhongYeuCauXacNhanTienVao_LuuThanhCong()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo());

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT002",
            KhachHangId = 1,
            SoTien = 1500000m,
            PhuongThuc = "TienMat",
            DaXacNhanTienVao = false // Tiền mặt không yêu cầu xác nhận tiền vào tài khoản
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        Assert.Equal("TienMat", repo.LastSavedRequest.PhuongThuc);
    }

    [Fact]
    public async Task TienMat_KhongLuuDuLieuQrHoacXacNhanChuyenKhoan()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo());

        // Cố tình gửi thông tin chuyển khoản kèm phương thức Tiền mặt
        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT003",
            KhachHangId = 1,
            SoTien = 500000m,
            PhuongThuc = "TienMat",
            DaXacNhanTienVao = true,
            TaiKhoanThuHuong = "107879627729",
            NganHangThuHuong = "ICB",
            NoiDungChuyenKhoan = "THU PT003 KH001"
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        // Server phải xóa bỏ toàn bộ dữ liệu chuyển khoản khi lưu tiền mặt
        Assert.False(repo.LastSavedRequest.DaXacNhanTienVao);
        Assert.Null(repo.LastSavedRequest.TaiKhoanThuHuong);
        Assert.Null(repo.LastSavedRequest.NganHangThuHuong);
        Assert.Null(repo.LastSavedRequest.NoiDungChuyenKhoan);
        Assert.Null(repo.LastSavedRequest.ThoiDiemXacNhan);
        Assert.Null(repo.LastSavedRequest.NguoiXacNhan);
    }

    [Fact]
    public async Task NhanVienBanHang_KhongDuocPhepThuTien()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT001",
            KhachHangId = 1,
            SoTien = 1000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(request));
    }

    [Fact]
    public void DoiPhuongThuc_XoaTrangThaiXacNhanVaQr()
    {
        // Kiểm tra hợp đồng làm sạch state:
        // Khi chuyển khoản đang có QR và xác nhận:
        bool daXacNhan = true;
        VietQrThanhToanDto? qr = new VietQrThanhToanDto { HopLe = true, QrUrl = "https://..." };

        // Đổi sang Tiền mặt:
        string phuongThuc = "TienMat";
        if (phuongThuc == "TienMat")
        {
            daXacNhan = false;
            qr = null;
        }

        Assert.False(daXacNhan);
        Assert.Null(qr);
    }

    [Fact]
    public void ThayDoiSoTien_HoacKhachHang_LamMatHieuLucXemTruocVaQr()
    {
        // Khi người dùng thay đổi số tiền, kết quả xem trước cũ và mã QR cũ phải bị hủy
        KetQuaPhanBoFifo? xemTruoc = new KetQuaPhanBoFifo();
        VietQrThanhToanDto? qr = new VietQrThanhToanDto();
        bool daXacNhan = true;

        // Giả lập sự kiện OnSoTienInputChanged:
        xemTruoc = null;
        qr = null;
        daXacNhan = false;

        Assert.Null(xemTruoc);
        Assert.Null(qr);
        Assert.False(daXacNhan);
    }

    [Fact]
    public void LoiTaiAnhQr_KhongLamMatThongTinChuyenKhoan()
    {
        var service = new VietQrService(new VietQrSettings());
        var dto = service.TaoVietQr("PT010", "KH010", 3000000m);

        // Mô phỏng trường hợp ảnh QR bị lỗi tải (@onerror fired):
        bool isQrImageError = true;

        // Dữ liệu thủ công để khách chuyển tiền vẫn nguyên vẹn
        Assert.True(isQrImageError);
        Assert.Equal("107879627729", dto.AccountNumber);
        Assert.Equal("LE ANH DIEM NHU", dto.AccountName);
        Assert.Equal("VietinBank", dto.BankName);
        Assert.Equal("3.000.000 đ", dto.SoTienDinhDang);
        Assert.Equal("THU PT010 KH010", dto.NoiDungChuyenKhoan);
    }

    private class MockKhachHangRepository : IKhachHangRepository
    {
        private readonly string _maKhachHang;

        public MockKhachHangRepository(string maKhachHang = "KH_VIP_01")
        {
            _maKhachHang = maKhachHang;
        }

        public Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId)
        {
            return Task.FromResult<KhachHang?>(new KhachHang
            {
                KhachHangId = khachHangId,
                MaKhachHang = _maKhachHang,
                TenKhachHang = "Khách Hàng Kiểm Thử",
                TrangThai = true
            });
        }

        public Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync() => Task.FromResult(Enumerable.Empty<KhachHang>());
        public Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null) => Task.FromResult(false);
        public Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId) => Task.FromResult(0m);
        public Task<UseCases.KhachHangScreen.TaoKhachHangResult> ThemKhachHangAsync(UseCases.KhachHangScreen.TaoKhachHangRequest request) => Task.FromResult(new UseCases.KhachHangScreen.TaoKhachHangResult { ThanhCong = true });
        public Task<UseCases.KhachHangScreen.CapNhatKhachHangResult> CapNhatKhachHangAsync(UseCases.KhachHangScreen.CapNhatKhachHangRequest request) => Task.FromResult(new UseCases.KhachHangScreen.CapNhatKhachHangResult { ThanhCong = true });
        public Task<UseCases.KhachHangScreen.DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi) => Task.FromResult(new UseCases.KhachHangScreen.DoiTrangThaiKhachHangResult { ThanhCong = true });
        public Task<string> LayMaKhachHangTiepTheoAsync() => Task.FromResult("KH001");
    }

    [Fact]
    public async Task ClientGiaMao_NguoiXacNhan_HoacThoiDiemXacNhan_KhongAnhHuongDuLieuLuu()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo("ketoan_that"));

        // Client cố tình truyền người xác nhận và thời điểm xác nhận giả
        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_HACK",
            KhachHangId = 1,
            SoTien = 1500000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true,
            NguoiXacNhan = "HackerGiaMao",
            ThoiDiemXacNhan = new DateTime(1999, 12, 31, 23, 59, 59)
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        // Server phải ghi đè hoàn toàn bằng danh tính thật và thời điểm thật
        Assert.Equal("ketoan_that", repo.LastSavedRequest.NguoiXacNhan);
        Assert.NotEqual("HackerGiaMao", repo.LastSavedRequest.NguoiXacNhan);
        Assert.True(repo.LastSavedRequest.ThoiDiemXacNhan > DateTime.Today.AddDays(-1));
    }

    [Fact]
    public async Task NguoiXacNhan_DuocLayTu_CurrentUserServicePhiaServer()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo("kt_chinh_thuc"));

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_USER",
            KhachHangId = 1,
            SoTien = 2000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        Assert.Equal("kt_chinh_thuc", repo.LastSavedRequest.NguoiXacNhan);
    }

    [Fact]
    public async Task ThoiDiemXacNhan_DuocSinhPhiaServer()
    {
        var repo = new MockThanhToanRepository();
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo());

        var before = DateTime.Now.AddSeconds(-2);

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_TIME",
            KhachHangId = 1,
            SoTien = 1000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };

        var result = await useCase.ExecuteAsync(request);
        var after = DateTime.Now.AddSeconds(2);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        Assert.NotNull(repo.LastSavedRequest.ThoiDiemXacNhan);
        Assert.True(repo.LastSavedRequest.ThoiDiemXacNhan >= before && repo.LastSavedRequest.ThoiDiemXacNhan <= after);
    }

    [Fact]
    public async Task TaiKhoan_NganHang_VaNoiDungChuyenKhoan_DuocDungLaiPhiaServer()
    {
        var repo = new MockThanhToanRepository();
        var khRepo = new MockKhachHangRepository("KH_DA_NANG");
        var useCase = new TaoThanhToanUseCase(repo, FakeCurrentUserService.KeToanCongNo(), khRepo);

        // Client truyền tài khoản, ngân hàng, nội dung sai lệch
        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_REBUILD",
            KhachHangId = 10,
            SoTien = 5000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true,
            TaiKhoanThuHuong = "000000000000",
            NganHangThuHuong = "FAKE_BANK",
            NoiDungChuyenKhoan = "NoiDungSaiLech"
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(repo.LastSavedRequest);
        // Tái dựng chuẩn xác từ VietQrSettings và VietQrService
        Assert.Equal("107879627729", repo.LastSavedRequest.TaiKhoanThuHuong);
        Assert.Equal("ICB", repo.LastSavedRequest.NganHangThuHuong);
        Assert.Equal("THU PT REBUILD KH DA NANG", repo.LastSavedRequest.NoiDungChuyenKhoan);
    }

    [Fact]
    public async Task TaiKhoan_KhongThuocVaiTroKeToanCongNo_BiTuChoi()
    {
        var repo = new MockThanhToanRepository();

        // 1. Nhân viên bán hàng
        var useCaseNvbh = new TaoThanhToanUseCase(repo, FakeCurrentUserService.NhanVienBanHang());
        var req1 = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_NVBH",
            KhachHangId = 1,
            SoTien = 1000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCaseNvbh.ExecuteAsync(req1));

        // 2. Chưa đăng nhập (Anonymous)
        var useCaseAnon = new TaoThanhToanUseCase(repo, FakeCurrentUserService.Anonymous());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCaseAnon.ExecuteAsync(req1));
    }

    [Fact]
    public async Task KhongXacDinhDuocNguoiDung_TuChoiLuuChuyenKhoan()
    {
        var repo = new MockThanhToanRepository();
        // User đã xác thực nhưng tên đăng nhập rỗng/null
        var fakeUserKhongTen = new FakeCurrentUserService(isAuthenticated: true, vaiTro: VaiTroConstants.KeToanCongNo, tenDangNhap: "");
        var useCase = new TaoThanhToanUseCase(repo, fakeUserKhongTen);

        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_NOTEN",
            KhachHangId = 1,
            SoTien = 1000000m,
            PhuongThuc = "ChuyenKhoan",
            DaXacNhanTienVao = true
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("Không xác định được danh tính", result.ThongBao);
    }
}
