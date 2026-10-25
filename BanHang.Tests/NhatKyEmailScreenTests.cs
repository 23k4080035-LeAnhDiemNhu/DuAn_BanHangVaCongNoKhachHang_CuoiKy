using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.NhatKyEmailScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Xunit;

namespace BanHang.Tests;

public class NhatKyEmailScreenTests
{
    private class FakeCurrentUserService : ICurrentUserService
    {
        private readonly string? _userName;
        private readonly string[] _roles;

        public FakeCurrentUserService(string? userName, params string[] roles)
        {
            _userName = userName;
            _roles = roles;
        }

        public Task<string?> GetTenDangNhapAsync() => Task.FromResult(_userName);

        public Task<string?> GetHoTenAsync() => Task.FromResult<string?>(_userName);

        public Task<string?> GetVaiTroAsync() => Task.FromResult<string?>(_roles.FirstOrDefault());

        public Task<bool> IsInRoleAsync(string role) => Task.FromResult(_roles.Contains(role, StringComparer.OrdinalIgnoreCase));

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(!string.IsNullOrEmpty(_userName));

        public static FakeCurrentUserService KeToan(string user = "ketoan01") => new(user, VaiTroConstants.KeToanCongNo);
        public static FakeCurrentUserService NhanVienBanHang(string user = "nvbh01") => new(user, VaiTroConstants.NhanVienBanHang);
    }

    private class MockRepo : INhatKyEmailRepository
    {
        public List<NhatKyEmailNhacNo> Data { get; } = new();

        public Task<int> LuuNhatKyAsync(NhatKyEmailNhacNo nhatKy)
        {
            nhatKy.NhatKyId = Data.Count + 1;
            Data.Add(nhatKy);
            return Task.FromResult(nhatKy.NhatKyId);
        }

        public Task<int> GhiNhanYeuCauGuiAsync(NhatKyEmailNhacNo nhatKy) => LuuNhatKyAsync(nhatKy);

        public Task CapNhatTrangThaiGuiAsync(
            int nhatKyId,
            TrangThaiGuiEmail trangThai,
            string? chiTiet,
            string? maPhanHoi,
            string? emailNhanThucTe = null,
            string? cheDoGui = null)
        {
            var item = Data.FirstOrDefault(x => x.NhatKyId == nhatKyId);
            if (item != null)
            {
                item.TrangThaiGui = trangThai;
                item.ChiTietTrangThai = chiTiet;
                item.MaPhanHoiDichVu = maPhanHoi;
                if (emailNhanThucTe != null) item.EmailNhanThucTe = emailNhanThucTe;
                if (cheDoGui != null) item.CheDoGui = cheDoGui;
            }
            return Task.CompletedTask;
        }

        public Task<NhatKyEmailNhacNo?> LayTheoIdempotencyKeyAsync(string idempotencyKey) =>
            Task.FromResult(Data.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

        public Task<IEnumerable<NhatKyEmailNhacNo>> LayLichSuTheoKhachHangAsync(int khachHangId) =>
            Task.FromResult<IEnumerable<NhatKyEmailNhacNo>>(Data.Where(x => x.KhachHangId == khachHangId).ToList());

        public Task<(IEnumerable<NhatKyEmailNhacNo> DanhSach, int TongSoBanGhi)> LayDanhSachPhanTrangAsync(
            string? tuKhoa,
            DateTime? tuNgay,
            DateTime? denNgay,
            TrangThaiGuiEmail? trangThaiGui,
            string? nguonSoan,
            string? cheDoGui,
            int pageIndex,
            int pageSize)
        {
            var query = Data.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string k = tuKhoa.Trim();
                query = query.Where(n =>
                    (n.MaKhachHang?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.TenKhachHang?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.EmailNguoiNhan?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.EmailNhanThucTe?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    n.NhatKyId.ToString() == k);
            }

            if (tuNgay.HasValue) query = query.Where(n => n.ThoiDiemGui >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(n => n.ThoiDiemGui <= denNgay.Value.Date.AddDays(1).AddTicks(-1));
            if (trangThaiGui.HasValue) query = query.Where(n => n.TrangThaiGui == trangThaiGui.Value);
            if (!string.IsNullOrWhiteSpace(nguonSoan)) query = query.Where(n => string.Equals(n.NguonSoan, nguonSoan.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(cheDoGui)) query = query.Where(n => string.Equals(n.CheDoGui, cheDoGui.Trim(), StringComparison.OrdinalIgnoreCase));

            var ordered = query.OrderByDescending(n => n.ThoiDiemGui).ThenByDescending(n => n.NhatKyId).ToList();
            int total = ordered.Count;
            int skip = Math.Max(0, (pageIndex - 1) * pageSize);
            var paged = ordered.Skip(skip).Take(pageSize).ToList();

            return Task.FromResult(((IEnumerable<NhatKyEmailNhacNo>)paged, total));
        }

        public Task<NhatKyEmailNhacNo?> LayTheoIdAsync(int nhatKyId) =>
            Task.FromResult(Data.FirstOrDefault(x => x.NhatKyId == nhatKyId));
    }

    private static MockRepo TaoDuLieuMau()
    {
        var repo = new MockRepo();
        repo.Data.Add(new NhatKyEmailNhacNo
        {
            NhatKyId = 1,
            KhachHangId = 101,
            MaKhachHang = "KH001",
            TenKhachHang = "Công ty TNHH Dệt May An Phát",
            EmailNguoiNhan = "ketoan@anphat.vn",
            EmailNhanThucTe = "ketoan@anphat.vn",
            NguoiDuyet = "ktcn_lan",
            ThoiDiemDuyet = new DateTime(2026, 10, 20, 9, 30, 0),
            ThoiDiemGui = new DateTime(2026, 10, 20, 9, 31, 0),
            NgayChotBaoCao = new DateTime(2026, 10, 20),
            TieuDe = "Thông báo công nợ tháng 10/2026",
            NoiDung = "Kính gửi quý khách...",
            GiongVan = GiongVanEmail.TieuChuan,
            TrangThaiGui = TrangThaiGuiEmail.TiepNhan,
            NguonSoan = "AI",
            CheDoGui = "GuiThat",
            IdempotencyKey = "SEND-AP001",
            PayloadHash = "hash-ap001",
            MaPhanHoiDichVu = "SMTP-250-OK"
        });

        repo.Data.Add(new NhatKyEmailNhacNo
        {
            NhatKyId = 2,
            KhachHangId = 102,
            MaKhachHang = "KH002",
            TenKhachHang = "Đại lý Bánh kẹo Hương Sen",
            EmailNguoiNhan = "huongsen@gmail.com",
            EmailNhanThucTe = "test_ketoan@gmail.com",
            NguoiDuyet = "ktcn_lan",
            ThoiDiemDuyet = new DateTime(2026, 10, 22, 14, 0, 0),
            ThoiDiemGui = new DateTime(2026, 10, 22, 14, 1, 0),
            NgayChotBaoCao = new DateTime(2026, 10, 22),
            TieuDe = "Nhắc nợ quá hạn kỳ 2 tháng 10",
            NoiDung = "Kính gửi Hương Sen...",
            GiongVan = GiongVanEmail.KhanTruong,
            TrangThaiGui = TrangThaiGuiEmail.ThatBai,
            ChiTietTrangThai = "Lỗi kết nối máy chủ SMTP port 587 (Timeout 10s)",
            NguonSoan = "MauCoSan",
            CheDoGui = "TestRedirect",
            IdempotencyKey = "SEND-HS002"
        });

        repo.Data.Add(new NhatKyEmailNhacNo
        {
            NhatKyId = 3,
            KhachHangId = 103,
            MaKhachHang = "KH003",
            TenKhachHang = "Siêu thị Miền Trung",
            EmailNguoiNhan = "mientrung@mart.vn",
            EmailNhanThucTe = null,
            NguoiDuyet = "ktcn_hung",
            ThoiDiemDuyet = new DateTime(2026, 10, 25, 8, 10, 0),
            ThoiDiemGui = new DateTime(2026, 10, 25, 8, 11, 0),
            NgayChotBaoCao = new DateTime(2026, 10, 25),
            TieuDe = "Đối soát định kỳ",
            NoiDung = "Kính gửi Siêu thị...",
            GiongVan = GiongVanEmail.NheNhang,
            TrangThaiGui = TrangThaiGuiEmail.DangXuLy,
            NguonSoan = "AI",
            CheDoGui = "MoPhong"
        });

        return repo;
    }

    [Fact]
    public async Task XemDanhSach_VaiTroNhanVienBanHang_BiTuChoiTruyCap()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemDanhSachNhatKyEmailUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(new BoLocNhatKyEmailDto()));
    }

    [Fact]
    public async Task XemChiTiet_VaiTroNhanVienBanHang_BiTuChoiTruyCap()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemChiTietNhatKyEmailUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(1));
    }

    [Fact]
    public async Task XemDanhSach_KeToanCongNo_LayThanhCongVaPhanTrangChuan()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemDanhSachNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        var filter = new BoLocNhatKyEmailDto { PageIndex = 1, PageSize = 2 };
        var result = await useCase.ExecuteAsync(filter);

        Assert.Equal(3, result.TongSoBanGhi);
        Assert.Equal(2, result.DanhSach.Count);
        Assert.Equal(2, result.TongSoTrang);
        Assert.False(result.CoTrangTruoc);
        Assert.True(result.CoTrangSau);
        // Sắp xếp mặc định: ThoiDiemGui mới nhất trước
        Assert.Equal(3, result.DanhSach[0].NhatKyId);
        Assert.Equal(2, result.DanhSach[1].NhatKyId);
    }

    [Fact]
    public async Task XemDanhSach_LocTheoTuKhoa_TraVeDungBanGhiKhop()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemDanhSachNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        // Tìm theo tên khách hàng
        var res1 = await useCase.ExecuteAsync(new BoLocNhatKyEmailDto { TuKhoa = "An Phát" });
        Assert.Single(res1.DanhSach);
        Assert.Equal("KH001", res1.DanhSach[0].MaKhachHang);

        // Tìm theo mã ID
        var res2 = await useCase.ExecuteAsync(new BoLocNhatKyEmailDto { TuKhoa = "2" });
        Assert.Single(res2.DanhSach);
        Assert.Equal("KH002", res2.DanhSach[0].MaKhachHang);

        // Tìm theo email
        var res3 = await useCase.ExecuteAsync(new BoLocNhatKyEmailDto { TuKhoa = "mientrung@mart.vn" });
        Assert.Single(res3.DanhSach);
        Assert.Equal("KH003", res3.DanhSach[0].MaKhachHang);
    }

    [Fact]
    public async Task XemDanhSach_LocTheoTrangThai_TraVeDungBanGhi()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemDanhSachNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        var res = await useCase.ExecuteAsync(new BoLocNhatKyEmailDto { TrangThaiGui = TrangThaiGuiEmail.ThatBai });
        Assert.Single(res.DanhSach);
        Assert.Equal(2, res.DanhSach[0].NhatKyId);
    }

    [Fact]
    public async Task XemDanhSach_LocTheoKhoangNgay_TraVeDung()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemDanhSachNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        var res = await useCase.ExecuteAsync(new BoLocNhatKyEmailDto
        {
            TuNgay = new DateTime(2026, 10, 21),
            DenNgay = new DateTime(2026, 10, 23)
        });

        Assert.Single(res.DanhSach);
        Assert.Equal(2, res.DanhSach[0].NhatKyId);
    }

    [Fact]
    public async Task XemChiTiet_IdHopLe_TraVeDayDuThongTinSnapshot()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemChiTietNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        var detail = await useCase.ExecuteAsync(1);

        Assert.NotNull(detail);
        Assert.Equal(1, detail.NhatKyId);
        Assert.Equal("KH001", detail.MaKhachHang);
        Assert.Equal("ktcn_lan", detail.NguoiDuyet);
        Assert.Equal(new DateTime(2026, 10, 20), detail.NgayChotBaoCao);
        Assert.Equal("SEND-AP001", detail.IdempotencyKey);
        Assert.Equal("hash-ap001", detail.PayloadHash);
        Assert.Equal("SMTP-250-OK", detail.MaPhanHoiDichVu);
    }

    [Fact]
    public async Task XemChiTiet_IdKhongTonTai_TraVeNull()
    {
        var repo = TaoDuLieuMau();
        var useCase = new XemChiTietNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());

        var detail = await useCase.ExecuteAsync(999);
        Assert.Null(detail);
    }

    [Fact]
    public async Task XemChiTiet_LamSachThongBaoKyThuat_KhongChuaStackTraceVaSecret()
    {
        var repo = TaoDuLieuMau();
        repo.Data[1].ChiTietTrangThai = "Lỗi xác thực SMTP: Password=mysecret123; Server=smtp.gmail.com   at System.Net.Mail.SmtpClient.Send() --- End of stack trace ---";

        var useCase = new XemChiTietNhatKyEmailUseCase(repo, FakeCurrentUserService.KeToan());
        var detail = await useCase.ExecuteAsync(2);

        Assert.NotNull(detail);
        Assert.DoesNotContain("mysecret123", detail.ChiTietTrangThai);
        Assert.DoesNotContain("at System.Net.Mail", detail.ChiTietTrangThai);
        Assert.Contains("Password=***", detail.ChiTietTrangThai);
    }

    [Theory]
    [InlineData(TrangThaiGuiEmail.TiepNhan, "SMTP đã tiếp nhận")]
    [InlineData(TrangThaiGuiEmail.ThatBai, "Gửi thất bại")]
    [InlineData(TrangThaiGuiEmail.ChuaRoKetQua, "Chưa rõ kết quả")]
    [InlineData(TrangThaiGuiEmail.DangXuLy, "Đang xử lý")]
    public void NhatKyEmailItemDto_AnhXaTrangThai_DungVanBanQuyDinh(TrangThaiGuiEmail status, string expectedText)
    {
        var dto = new NhatKyEmailItemDto { TrangThaiGui = status };
        Assert.Equal(expectedText, dto.TenTrangThaiHienThi);
        Assert.DoesNotContain("người nhận đã nhận", dto.TenTrangThaiHienThi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("đã đọc", dto.TenTrangThaiHienThi, StringComparison.OrdinalIgnoreCase);
    }
}
