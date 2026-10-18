using System.Text;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using DuAn_BanHangVaCongNoKhachHang.Pages;
using Xunit;

namespace BanHang.Tests;

public class SanPhamHinhAnhVaDonGiaTests
{
    [Theory]
    [InlineData(90000000.00, "90000000", 0)]
    [InlineData(1500.50, "1500.5", 1)]
    [InlineData(1500.25, "1500.25", 2)]
    [InlineData(0.00, "0", 0)]
    [InlineData(123456789.00, "123456789", 0)]
    public void Test01_ChuanHoaDonGia_BoSoKhongThua_GiuNguyenPhanLe(decimal input, string expectedStr, int expectedScale)
    {
        var normalized = SanPhamHelper.ChuanHoaDonGia(input);

        Assert.Equal(input, normalized); // Giá trị số học không đổi
        Assert.Equal(expectedStr, normalized.ToString()); // Chuỗi hiển thị không có .00 thừa
        var scale = (decimal.GetBits(normalized)[3] >> 16) & 0xFF;
        Assert.Equal(expectedScale, scale);
    }

    [Fact]
    public void Test02_KiemTraDinhDangAnh_JpegHopLe_TraVeJpg()
    {
        // JPEG magic bytes: FF D8 FF E0 ...
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01 };
        var (isValid, ext, mime) = SanPhamHelper.KiemTraDinhDangAnh(jpegBytes);

        Assert.True(isValid);
        Assert.Equal(".jpg", ext);
        Assert.Equal("image/jpeg", mime);
    }

    [Fact]
    public void Test03_KiemTraDinhDangAnh_PngHopLe_TraVePng()
    {
        // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A ...
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48 };
        var (isValid, ext, mime) = SanPhamHelper.KiemTraDinhDangAnh(pngBytes);

        Assert.True(isValid);
        Assert.Equal(".png", ext);
        Assert.Equal("image/png", mime);
    }

    [Fact]
    public void Test04_KiemTraDinhDangAnh_WebpHopLe_TraVeWebp()
    {
        // WebP header: RIFF (4 bytes), Size (4 bytes), WEBP (4 bytes)
        var webpBytes = new byte[16];
        // 'R', 'I', 'F', 'F'
        webpBytes[0] = 0x52; webpBytes[1] = 0x49; webpBytes[2] = 0x46; webpBytes[3] = 0x46;
        // Size placeholder
        webpBytes[4] = 0x20; webpBytes[5] = 0x00; webpBytes[6] = 0x00; webpBytes[7] = 0x00;
        // 'W', 'E', 'B', 'P'
        webpBytes[8] = 0x57; webpBytes[9] = 0x45; webpBytes[10] = 0x42; webpBytes[11] = 0x50;

        var (isValid, ext, mime) = SanPhamHelper.KiemTraDinhDangAnh(webpBytes);

        Assert.True(isValid);
        Assert.Equal(".webp", ext);
        Assert.Equal("image/webp", mime);
    }

    [Fact]
    public void Test05_KiemTraDinhDangAnh_TepGiaMaoVaRong_TuChoi()
    {
        // 1. Text file giả mạo đuôi .jpg
        var textBytes = Encoding.UTF8.GetBytes("Day la tep van ban plain text gia mao hinh anh");
        var (isValidText, _, _) = SanPhamHelper.KiemTraDinhDangAnh(textBytes);
        Assert.False(isValidText);

        // 2. Tệp rỗng hoặc null hoặc quá ngắn (< 12 bytes)
        Assert.False(SanPhamHelper.KiemTraDinhDangAnh(null).IsValid);
        Assert.False(SanPhamHelper.KiemTraDinhDangAnh(Array.Empty<byte>()).IsValid);
        Assert.False(SanPhamHelper.KiemTraDinhDangAnh(new byte[] { 0xFF, 0xD8 }).IsValid);

        // 3. Tệp RIFF nhưng là file WAV (không phải WEBP)
        var wavBytes = new byte[16];
        wavBytes[0] = 0x52; wavBytes[1] = 0x49; wavBytes[2] = 0x46; wavBytes[3] = 0x46; // RIFF
        wavBytes[8] = 0x57; wavBytes[9] = 0x41; wavBytes[10] = 0x56; wavBytes[11] = 0x45; // WAVE
        var (isValidWav, _, _) = SanPhamHelper.KiemTraDinhDangAnh(wavBytes);
        Assert.False(isValidWav);
    }

    [Fact]
    public async Task Test06_CapNhatSanPham_KhongDoiHinhAnh_GiuNguyenHinhAnhCu()
    {
        // Giả lập repository cập nhật sản phẩm
        string? hinhAnhDaLuu = null;
        var fakeRepo = new FakeSanPhamRepositoryForImageTest
        {
            OnCapNhat = req => hinhAnhDaLuu = req.HinhAnh
        };

        var useCase = new CapNhatSanPhamUseCase(fakeRepo, FakeCurrentUserService.KeToanCongNo());

        const string oldImage = "/images/sanpham/sp_cu_da_co.jpg";
        var request = new CapNhatSanPhamRequest
        {
            SanPhamId = 10,
            MaSanPham = "SP010",
            TenSanPham = "Sản phẩm test ảnh",
            DonViTinh = "Cái",
            DonGia = 1000000,
            HinhAnh = oldImage, // Giữ ảnh cũ khi sửa không chọn ảnh mới
            TrangThai = true
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.Equal(oldImage, hinhAnhDaLuu);
    }

    private class FakeSanPhamRepositoryForImageTest : ISanPhamRepository
    {
        public Action<CapNhatSanPhamRequest>? OnCapNhat { get; set; }

        public Task<CapNhatSanPhamResult> CapNhatSanPhamAsync(CapNhatSanPhamRequest request)
        {
            OnCapNhat?.Invoke(request);
            return Task.FromResult(new CapNhatSanPhamResult { ThanhCong = true });
        }

        public Task<DoiTrangThaiSanPhamResult> DoiTrangThaiSanPhamAsync(int sanPhamId, bool trangThaiMoi) => throw new NotImplementedException();
        public Task<bool> KiemTraTrungMaSanPhamAsync(string maSanPham) => Task.FromResult(false);
        public Task<string> LayMaSanPhamTiepTheoAsync() => Task.FromResult("SP999");
        public Task<TaoSanPhamResult> ThemSanPhamAsync(TaoSanPhamRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync() => throw new NotImplementedException();
        public Task<IEnumerable<SanPham>> LayDanhSachTatCaSanPhamAsync() => throw new NotImplementedException();
        public Task<SanPham?> LaySanPhamTheoIdAsync(int sanPhamId) => Task.FromResult<SanPham?>(new SanPham { SanPhamId = sanPhamId, MaSanPham = "SP010", TenSanPham = "Test", DonViTinh = "Cái", DonGia = 1000000 });
    }
}
