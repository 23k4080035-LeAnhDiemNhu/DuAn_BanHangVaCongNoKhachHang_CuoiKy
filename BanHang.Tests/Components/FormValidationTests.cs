using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DuAn_BanHangVaCongNoKhachHang.Pages;
using Xunit;

namespace BanHang.Tests.Components
{
    public class FormValidationTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        // ==========================================
        // 1. KhachHang Form Validation
        // ==========================================
        [Fact]
        public void FormKhachHangModel_DuLieuHopLe_ValidationThanhCong()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                MaKhachHang = "KH001",
                TenKhachHang = "Công ty ABC",
                SoDienThoai = "0901234567",
                Email = "abc@company.com",
                DiaChi = "123 Đường Số 1",
                HanMucCongNo = 50000000m
            };

            var errors = ValidateModel(model);
            Assert.Empty(errors);
        }

        [Fact]
        public void FormKhachHangModel_ThieuTenKhachHang_BaoLoiBatBuoc()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "", // Bỏ trống
                HanMucCongNo = 10000000m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.TenKhachHang)));
        }

        [Fact]
        public void FormKhachHangModel_EmailKhongHopLe_BaoLoiDinhDang()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "Khách Hàng A",
                Email = "invalid-email-format",
                HanMucCongNo = 10000000m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.Email)));
        }

        [Fact]
        public void FormKhachHangModel_HanMucAm_BaoLoiKhoangGiaTri()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "Khách Hàng A",
                HanMucCongNo = -5000m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.HanMucCongNo)));
        }

        [Fact]
        public void FormKhachHangModel_MaKhachHangDeTrong_HopLeViSinhTuDong()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                MaKhachHang = "", // Không áp Required
                TenKhachHang = "Khách Hàng A",
                HanMucCongNo = 10000000m
            };

            var errors = ValidateModel(model);
            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.MaKhachHang)));
        }

        [Fact]
        public void FormKhachHangModel_SoDienThoai_KhongSietDinhDangPhone_ChiKiemTraDoDai()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "Khách Hàng A",
                SoDienThoai = "028.3822.1234", // Định dạng có dấu chấm không theo chuẩn Phone quốc tế vẫn hợp lệ vì CSDL chỉ kiểm tra StringLength(20)
                HanMucCongNo = 10000000m
            };

            var errors = ValidateModel(model);
            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.SoDienThoai)));
        }

        [Fact]
        public void FormKhachHangModel_SoNgayNo_HopLe_KhongLoi()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "Khách Hàng A",
                HanMucCongNo = 10000000m,
                SoNgayNo = 30
            };

            var errors = ValidateModel(model);
            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.SoNgayNo)));
        }

        [Fact]
        public void FormKhachHangModel_SoNgayNo_Am_BaoLoi()
        {
            var model = new KhachHangPage.FormKhachHangModel
            {
                TenKhachHang = "Khách Hàng A",
                HanMucCongNo = 10000000m,
                SoNgayNo = -5
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.SoNgayNo)));
        }


        // ==========================================
        // 2. SanPham Form Validation
        // ==========================================
        [Fact]
        public void FormSanPhamModel_DuLieuHopLe_ValidationThanhCong()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                MaSanPham = "SP001",
                TenSanPham = "Chuột không dây Logitech",
                DonViTinh = "Cái",
                DonGia = 250000m,
                SoLuongTon = 50,
                MoTa = "Mô tả sản phẩm"
            };

            var errors = ValidateModel(model);
            Assert.Empty(errors);
        }

        [Fact]
        public void FormSanPhamModel_ThieuTenSanPham_BaoLoiBatBuoc()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                TenSanPham = "",
                DonViTinh = "Cái",
                DonGia = 100000m,
                SoLuongTon = 10
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SanPhamPage.FormSanPhamModel.TenSanPham)));
        }

        [Fact]
        public void FormSanPhamModel_DonGiaBangKhong_HopLe()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                TenSanPham = "Bàn phím cơ",
                DonViTinh = "Cái",
                DonGia = 0m, // 0 là giá trị hợp lệ
                SoLuongTon = 10
            };

            var errors = ValidateModel(model);
            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(SanPhamPage.FormSanPhamModel.DonGia)));
        }

        [Fact]
        public void FormSanPhamModel_DonGiaAm_BaoLoiKhongDuocAm()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                TenSanPham = "Bàn phím cơ",
                DonViTinh = "Cái",
                DonGia = -50000m,
                SoLuongTon = 10
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SanPhamPage.FormSanPhamModel.DonGia)));
        }

        [Fact]
        public void FormSanPhamModel_HinhAnhQua300KyTu_BaoLoiDoDai()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                TenSanPham = "Tai nghe Gaming",
                DonViTinh = "Cái",
                DonGia = 150000m,
                HinhAnh = new string('a', 301) // Vượt quá 300 ký tự theo schema CSDL
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SanPhamPage.FormSanPhamModel.HinhAnh)));
        }

        [Fact]
        public void FormSanPhamModel_SoLuongTonAm_BaoLoiKhoangGiaTri()
        {
            var model = new SanPhamPage.FormSanPhamModel
            {
                TenSanPham = "Bàn phím cơ",
                DonViTinh = "Cái",
                DonGia = 500000m,
                SoLuongTon = -1
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SanPhamPage.FormSanPhamModel.SoLuongTon)));
        }

        // ==========================================
        // 3. TaoHoaDon Form Validation
        // ==========================================
        [Fact]
        public void FormTaoHoaDonModel_DuLieuHopLe_ValidationThanhCong()
        {
            var model = new TaoHoaDonPage.FormTaoHoaDonModel
            {
                KhachHangId = 1,
                NgayLap = DateTime.Today,
                HanThanhToan = DateTime.Today.AddDays(15),
                GhiChu = "Giao hàng giờ hành chính"
            };

            var errors = ValidateModel(model);
            Assert.Empty(errors);
        }

        [Fact]
        public void FormTaoHoaDonModel_ChuaChonKhachHang_BaoLoiBatBuoc()
        {
            var model = new TaoHoaDonPage.FormTaoHoaDonModel
            {
                KhachHangId = 0,
                NgayLap = DateTime.Today,
                HanThanhToan = DateTime.Today.AddDays(30)
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoHoaDonPage.FormTaoHoaDonModel.KhachHangId)));
        }

        [Fact]
        public void FormTaoHoaDonModel_ChuaChonHanThanhToan_BaoLoiBatBuoc()
        {
            var model = new TaoHoaDonPage.FormTaoHoaDonModel
            {
                KhachHangId = 1,
                NgayLap = DateTime.Today,
                HanThanhToan = null // Bắt buộc chọn, không mặc định
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoHoaDonPage.FormTaoHoaDonModel.HanThanhToan)));
        }

        // ==========================================
        // 4. TaoThanhToan Form Validation
        // ==========================================
        [Fact]
        public void FormTaoThanhToanModel_DuLieuHopLe_ValidationThanhCong()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 2,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "ChuyenKhoan",
                SoTien = 15000000m,
                GhiChu = "Khách chuyển khoản BIDV"
            };

            var errors = ValidateModel(model);
            Assert.Empty(errors);
        }

        [Fact]
        public void FormTaoThanhToanModel_ChuaChonKhachHang_BaoLoiBatBuoc()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 0,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "ChuyenKhoan",
                SoTien = 5000000m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoThanhToanPage.FormTaoThanhToanModel.KhachHangId)));
        }

        [Fact]
        public void FormTaoThanhToanModel_SoTienBangKhongHoacAm_BaoLoiLonHonKhong()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 1,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "TienMat",
                SoTien = 0m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoThanhToanPage.FormTaoThanhToanModel.SoTien)));
        }

        [Fact]
        public void FormTaoThanhToanModel_SoTienAm_BaoLoiLonHonKhong()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 1,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "TienMat",
                SoTien = -5000m
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoThanhToanPage.FormTaoThanhToanModel.SoTien)));
        }

        [Fact]
        public void FormTaoThanhToanModel_SoTienQua2ChuSoThapPhan_BaoLoiToiDa2ChuSoThapPhan()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 1,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "ChuyenKhoan",
                SoTien = 100000.125m // 3 chữ số thập phân
            };

            var errors = ValidateModel(model);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaoThanhToanPage.FormTaoThanhToanModel.SoTien)) &&
                                         e.ErrorMessage != null && e.ErrorMessage.Contains("2 chữ số thập phân"));
        }

        [Fact]
        public void FormTaoThanhToanModel_LoiDinhDangSoTien_ChiBaoLoiKhongHopLe_KhongBaoLoiLonHonKhong()
        {
            var model = new TaoThanhToanPage.FormTaoThanhToanModel
            {
                KhachHangId = 1,
                NgayThanhToan = DateTime.Today,
                PhuongThuc = "ChuyenKhoan",
                SoTien = 0m,
                LoiDinhDangSoTien = "Số tiền thanh toán không hợp lệ."
            };

            var errors = ValidateModel(model);
            var soTienErrors = new List<ValidationResult>();
            foreach (var err in errors)
            {
                foreach (var member in err.MemberNames)
                {
                    if (member == nameof(TaoThanhToanPage.FormTaoThanhToanModel.SoTien))
                    {
                        soTienErrors.Add(err);
                        break;
                    }
                }
            }

            Assert.Single(soTienErrors);
            Assert.Equal("Số tiền thanh toán không hợp lệ.", soTienErrors[0].ErrorMessage);
        }

        [Theory]
        [InlineData("abc", 0, "Số tiền thanh toán không hợp lệ.")]
        [InlineData("10a", 0, "Số tiền thanh toán không hợp lệ.")]
        [InlineData("0", 0, "Số tiền thanh toán phải lớn hơn 0.")]
        [InlineData("-50", -50, "Số tiền thanh toán phải lớn hơn 0.")]
        [InlineData("100.125", 100.125, "Số tiền thanh toán chỉ được phép có tối đa 2 chữ số thập phân.")]
        [InlineData("100,125", 100.125, "Số tiền thanh toán chỉ được phép có tối đa 2 chữ số thập phân.")]
        [InlineData("50000", 50000, null)]
        [InlineData("50000.5", 50000.5, null)]
        public void TaoThanhToanPage_KiemTraSoTienThanhToan_CacTruongHop(string input, decimal expectedVal, string? expectedLoi)
        {
            var (val, loi) = TaoThanhToanPage.KiemTraSoTienThanhToan(input);
            Assert.Equal(expectedVal, val);
            Assert.Equal(expectedLoi, loi);
        }

        // ==========================================
        // 5. TaoHoaDon Line Quantity Validation
        // ==========================================
        [Theory]
        [InlineData("1.5")]
        [InlineData("2,5")]
        [InlineData("0.1")]
        public void TaoHoaDonPage_KiemTraSoLuong_SoThapPhan_BaoLoiSoNguyen(string input)
        {
            var (soLuong, loi) = TaoHoaDonPage.KiemTraSoLuongDongHang(input);
            Assert.Equal(0, soLuong);
            Assert.Equal("Số lượng phải là số nguyên.", loi);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("-10")]
        public void TaoHoaDonPage_KiemTraSoLuong_SoKhongVaAm_BaoLoiLonHonKhong(string input)
        {
            var (_, loi) = TaoHoaDonPage.KiemTraSoLuongDongHang(input);
            Assert.Equal("Số lượng phải lớn hơn 0.", loi);
        }

        [Fact]
        public void TaoHoaDonPage_KiemTraSoLuong_KyTuChu_BaoLoiSoNguyen()
        {
            var (soLuong, loi) = TaoHoaDonPage.KiemTraSoLuongDongHang("abc");
            Assert.Equal(0, soLuong);
            Assert.Equal("Số lượng phải là số nguyên.", loi);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("50", 50)]
        [InlineData("1000", 1000)]
        public void TaoHoaDonPage_KiemTraSoLuong_SoNguyenDuong_HopLe(string input, int expectedSl)
        {
            var (soLuong, loi) = TaoHoaDonPage.KiemTraSoLuongDongHang(input);
            Assert.Null(loi);
            Assert.Equal(expectedSl, soLuong);
        }
    }
}
