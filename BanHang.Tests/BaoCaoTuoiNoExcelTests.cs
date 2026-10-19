using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.Reports.Excel;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using ClosedXML.Excel;
using Xunit;

namespace BanHang.Tests;

public class BaoCaoTuoiNoExcelTests
{
    private class FakeBaoCaoTuoiNoRepoMock : IBaoCaoTuoiNoRepository
    {
        public DuLieuBaoCaoTuoiNoDto DuLieu { get; set; } = new()
        {
            RawData = new List<KhachHangHoaDonTuoiNoRawDto>(),
            TienThuChuaPhanBo = 0m
        };

        public Task<DuLieuBaoCaoTuoiNoDto> LayDuLieuBaoCaoAsync(DateTime ngayBaoCao)
        {
            return Task.FromResult(DuLieu);
        }

        public Task<IEnumerable<KhachHangHoaDonTuoiNoRawDto>> LayDuLieuTuoiNoRawAsync(DateTime ngayBaoCao)
        {
            return Task.FromResult(DuLieu.RawData);
        }

        public Task<decimal> LayTongTienThuChuaPhanBoAsync(DateTime ngayBaoCao)
        {
            return Task.FromResult(DuLieu.TienThuChuaPhanBo);
        }
    }

    private static BaoCaoTuoiNoResult TaoDuLieuMau(DateTime ngayChot)
    {
        return new BaoCaoTuoiNoResult
        {
            NgayBaoCao = ngayChot.Date,
            TienThuChuaPhanBo = 1500000m,
            DanhSach = new List<BaoCaoTuoiNoItem>
            {
                new()
                {
                    KhachHangId = 1,
                    MaKhachHang = "KH001",
                    TenKhachHang = "Công ty TNHH Ánh Dương",
                    SoDienThoai = "0901234567",
                    HanMucCongNo = 50000000m,
                    TuoiNo_0_30 = 10000000m,
                    TuoiNo_31_60 = 5000000m,
                    TuoiNo_61_90 = 0m,
                    TuoiNo_Tren_90 = 0m,
                    TongCongNo = 15000000m
                },
                new()
                {
                    KhachHangId = 2,
                    MaKhachHang = "KH002",
                    TenKhachHang = "=Công ty Cổ phần Sao Mai", // Test formula injection
                    SoDienThoai = "0987654321",
                    HanMucCongNo = 20000000m,
                    TuoiNo_0_30 = 0m,
                    TuoiNo_31_60 = 0m,
                    TuoiNo_61_90 = 8000000m,
                    TuoiNo_Tren_90 = 12000000m,
                    TongCongNo = 20000000m
                }
            }
        };
    }

    [Fact]
    public void Test01_ClosedXmlExporter_XuatFile_DocLaiDungCauTrucVaGiaTriSo()
    {
        var ngayChot = new DateTime(2026, 10, 15);
        var data = TaoDuLieuMau(ngayChot);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();

        var bytes = exporter.ExportBaoCaoTuoiNo(data);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("TuoiNoKhachHang");
        Assert.NotNull(ws);

        // 1. Tiêu đề
        Assert.Equal("BÁO CÁO PHÂN TÍCH TUỔI NỢ KHÁCH HÀNG", ws.Cell("A2").GetString());

        // 2. Ngày chốt báo cáo (DateTime cell)
        var dateVal = ws.Cell("B3").GetDateTime();
        Assert.Equal(ngayChot.Date, dateVal.Date);

        // 3. Freeze panes tại hàng 5
        Assert.True(ws.SheetView.SplitRow >= 5);

        // 4. Dòng dữ liệu khách hàng 1
        Assert.Equal(1, ws.Cell(6, 1).GetValue<int>());
        Assert.Equal("KH001", ws.Cell(6, 2).GetString());
        Assert.Equal("Công ty TNHH Ánh Dương", ws.Cell(6, 3).GetString());
        Assert.Equal(50000000m, ws.Cell(6, 4).GetValue<decimal>());
        Assert.Equal(10000000m, ws.Cell(6, 5).GetValue<decimal>());
        Assert.Equal(5000000m, ws.Cell(6, 6).GetValue<decimal>());
        Assert.Equal(0m, ws.Cell(6, 7).GetValue<decimal>());
        Assert.Equal(0m, ws.Cell(6, 8).GetValue<decimal>());
        Assert.Equal(15000000m, ws.Cell(6, 9).GetValue<decimal>());

        // 5. Khách hàng 2 (chống formula injection: bảo đảm cell là text thuần, không bị hiểu thành formula)
        Assert.Equal(2, ws.Cell(7, 1).GetValue<int>());
        Assert.Equal("KH002", ws.Cell(7, 2).GetString());
        Assert.False(ws.Cell(7, 3).HasFormula);
        Assert.Equal(XLDataType.Text, ws.Cell(7, 3).DataType);
        Assert.Contains("Công ty Cổ phần Sao Mai", ws.Cell(7, 3).GetString());
        Assert.Equal(20000000m, ws.Cell(7, 9).GetValue<decimal>());

        // 6. Hàng tổng cộng (Hàng 8)
        Assert.Equal("TỔNG CỘNG", ws.Cell(8, 1).GetString());
        Assert.Equal(70000000m, ws.Cell(8, 4).GetValue<decimal>()); // Tổng hạn mức
        Assert.Equal(10000000m, ws.Cell(8, 5).GetValue<decimal>()); // Tổng 0-30
        Assert.Equal(5000000m, ws.Cell(8, 6).GetValue<decimal>());  // Tổng 31-60
        Assert.Equal(8000000m, ws.Cell(8, 7).GetValue<decimal>());  // Tổng 61-90
        Assert.Equal(12000000m, ws.Cell(8, 8).GetValue<decimal>()); // Tổng Trên 90
        Assert.Equal(35000000m, ws.Cell(8, 9).GetValue<decimal>()); // Tổng công nợ

        // Tổng từng dòng khớp tổng các nhóm
        Assert.Equal(data.DanhSach[0].TongCongNo,
            data.DanhSach[0].TuoiNo_0_30 + data.DanhSach[0].TuoiNo_31_60 + data.DanhSach[0].TuoiNo_61_90 + data.DanhSach[0].TuoiNo_Tren_90);
        Assert.Equal(data.DanhSach[1].TongCongNo,
            data.DanhSach[1].TuoiNo_0_30 + data.DanhSach[1].TuoiNo_31_60 + data.DanhSach[1].TuoiNo_61_90 + data.DanhSach[1].TuoiNo_Tren_90);

        // 7. Tiền thu chưa phân bổ (Hàng 10)
        var unallocatedText = ws.Cell(10, 1).GetString();
        Assert.Contains("Tiền thu chưa phân bổ", unallocatedText);
        Assert.Contains("1,500,000", unallocatedText);
        Assert.Contains("15/10/2026", unallocatedText);
    }

    [Fact]
    public void Test02_ClosedXmlExporter_DuLieuTrong_TaoFileHopLe()
    {
        var ngayChot = new DateTime(2026, 10, 19);
        var data = new BaoCaoTuoiNoResult
        {
            NgayBaoCao = ngayChot,
            TienThuChuaPhanBo = 0m,
            DanhSach = new List<BaoCaoTuoiNoItem>()
        };

        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var bytes = exporter.ExportBaoCaoTuoiNo(data);

        Assert.NotNull(bytes);
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("TuoiNoKhachHang");
        Assert.NotNull(ws);

        // Hàng 6 có thông báo không có dữ liệu
        Assert.Contains("Không có dữ liệu công nợ", ws.Cell(6, 1).GetString());

        // Hàng tổng cộng (Hàng 7) tổng = 0
        Assert.Equal("TỔNG CỘNG", ws.Cell(7, 1).GetString());
        Assert.Equal(0m, ws.Cell(7, 9).GetValue<decimal>());
    }

    [Fact]
    public async Task Test03_XuatExcelUseCase_ChuaDangNhap_BiTuChoi()
    {
        var anonymousUser = FakeCurrentUserService.Anonymous();
        var repo = new FakeBaoCaoTuoiNoRepoMock();
        var xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(repo, anonymousUser);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var useCase = new XuatBaoCaoTuoiNoExcelUseCase(xemBaoCaoUseCase, exporter, anonymousUser);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(DateTime.Today, null));
    }

    [Fact]
    public async Task Test04_XuatExcelUseCase_SaiVaiTro_NhanVienBanHang_BiTuChoi()
    {
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();
        var repo = new FakeBaoCaoTuoiNoRepoMock();
        var xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(repo, nvbhUser);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var useCase = new XuatBaoCaoTuoiNoExcelUseCase(xemBaoCaoUseCase, exporter, nvbhUser);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(DateTime.Today, null));
    }

    [Fact]
    public async Task Test05_XuatExcelUseCase_KeToanCongNo_XuatThanhCong()
    {
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var repo = new FakeBaoCaoTuoiNoRepoMock();
        var xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(repo, ktcnUser);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var useCase = new XuatBaoCaoTuoiNoExcelUseCase(xemBaoCaoUseCase, exporter, ktcnUser);

        var ngayApDung = new DateTime(2026, 10, 15);
        var result = await useCase.ExecuteAsync(ngayApDung, null);

        Assert.True(result.ThanhCong);
        Assert.Equal("BaoCaoTuoiNo_20261015.xlsx", result.TenFile);
        Assert.NotNull(result.FileBytes);
        Assert.True(result.FileBytes.Length > 0);
    }

    [Fact]
    public async Task Test06_XuatExcelUseCase_DuLieuThayDoiSoVoiSnapshot_PhatHienVaTuChoi()
    {
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var repo = new FakeBaoCaoTuoiNoRepoMock
        {
            // Database server trả về số tiền chưa phân bổ là 500,000
            DuLieu = new DuLieuBaoCaoTuoiNoDto
            {
                TienThuChuaPhanBo = 500000m,
                RawData = new List<KhachHangHoaDonTuoiNoRawDto>()
            }
        };
        var xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(repo, ktcnUser);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var useCase = new XuatBaoCaoTuoiNoExcelUseCase(xemBaoCaoUseCase, exporter, ktcnUser);

        var ngayApDung = DateTime.Today;
        // Snapshot trên màn hình của client cũ (chưa phân bổ = 0)
        var snapshotCu = new BaoCaoTuoiNoResult
        {
            NgayBaoCao = ngayApDung,
            TienThuChuaPhanBo = 0m,
            DanhSach = new List<BaoCaoTuoiNoItem>()
        };

        var result = await useCase.ExecuteAsync(ngayApDung, snapshotCu);

        Assert.False(result.ThanhCong);
        Assert.True(result.DuLieuBiThayDoi);
        Assert.Contains("thay đổi so với bảng báo cáo", result.ThongBaoLoi);
    }

    [Fact]
    public async Task Test07_XuatExcelUseCase_NgayNhapChuaApDung_KhongAnhHuongNgayXuat()
    {
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var repo = new FakeBaoCaoTuoiNoRepoMock();
        var xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(repo, ktcnUser);
        var exporter = new ClosedXmlBaoCaoTuoiNoExcelExporter();
        var useCase = new XuatBaoCaoTuoiNoExcelUseCase(xemBaoCaoUseCase, exporter, ktcnUser);

        // Ngày đang áp dụng trên màn hình là 2026-10-15
        var ngayApDung = new DateTime(2026, 10, 15);

        // Giả sử client gọi ExecuteAsync với ngayApDung (chứ không phải ngày mới nhập)
        var result = await useCase.ExecuteAsync(ngayApDung, null);

        Assert.True(result.ThanhCong);
        Assert.Equal("BaoCaoTuoiNo_20261015.xlsx", result.TenFile);

        using var ms = new MemoryStream(result.FileBytes!);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("TuoiNoKhachHang");
        var dateInExcel = ws.Cell("B3").GetDateTime();
        Assert.Equal(new DateTime(2026, 10, 15), dateInExcel.Date);
    }
}
