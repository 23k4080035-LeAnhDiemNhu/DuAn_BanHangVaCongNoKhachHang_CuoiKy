using System.Threading.Tasks;
using DuAn_BanHangVaCongNoKhachHang.Components;
using Microsoft.AspNetCore.Components;
using Xunit;

#pragma warning disable BL0005

namespace BanHang.Tests.Components
{
    public class ComponentLogicTests
    {
        [Fact]
        public async Task ModalXacNhan_KhiKhongDangXuLy_GoiCallbackXacNhan()
        {
            // Arrange
            var modal = new ModalXacNhan();
            modal.DangXuLy = false;
            bool xacNhanDaGoi = false;
            modal.OnXacNhan = EventCallback.Factory.Create(this, () => xacNhanDaGoi = true);

            // Act
            await modal.XuLyXacNhanAsync();

            // Assert
            Assert.True(xacNhanDaGoi, "Callback OnXacNhan phải được gọi khi DangXuLy = false.");
        }

        [Fact]
        public async Task ModalXacNhan_KhiDangXuLy_ChongBamLap_KhongGoiCallbackXacNhan()
        {
            // Arrange
            var modal = new ModalXacNhan();
            modal.DangXuLy = true; // Đang lưu dữ liệu
            bool xacNhanDaGoi = false;
            modal.OnXacNhan = EventCallback.Factory.Create(this, () => xacNhanDaGoi = true);

            // Act
            await modal.XuLyXacNhanAsync();

            // Assert
            Assert.False(xacNhanDaGoi, "Callback OnXacNhan KHÔNG được gọi khi DangXuLy = true để chống bấm lặp.");
        }

        [Fact]
        public async Task ModalXacNhan_KhiKhongDangXuLy_GoiCallbackHuy()
        {
            // Arrange
            var modal = new ModalXacNhan();
            modal.DangXuLy = false;
            bool huyDaGoi = false;
            modal.OnHuy = EventCallback.Factory.Create(this, () => huyDaGoi = true);

            // Act
            await modal.XuLyHuyAsync();

            // Assert
            Assert.True(huyDaGoi, "Callback OnHuy phải được gọi khi DangXuLy = false.");
        }

        [Fact]
        public async Task ModalXacNhan_KhiDangXuLy_KhongGoiCallbackHuy()
        {
            // Arrange
            var modal = new ModalXacNhan();
            modal.DangXuLy = true;
            bool huyDaGoi = false;
            modal.OnHuy = EventCallback.Factory.Create(this, () => huyDaGoi = true);

            // Act
            await modal.XuLyHuyAsync();

            // Assert
            Assert.False(huyDaGoi, "Callback OnHuy KHÔNG được gọi khi DangXuLy = true.");
        }

        [Fact]
        public async Task TrangThaiDuLieu_KhiGoiXuLyThuLai_GoiCallbackOnThuLai()
        {
            // Arrange
            var khung = new TrangThaiDuLieu();
            bool thuLaiDaGoi = false;
            khung.OnThuLai = EventCallback.Factory.Create(this, () => thuLaiDaGoi = true);

            // Act
            await khung.XuLyThuLaiAsync();

            // Assert
            Assert.True(thuLaiDaGoi, "Callback OnThuLai phải được gọi khi người dùng nhấn Thử lại.");
        }

        [Theory]
        [InlineData("active", "badge-active")]
        [InlineData("selling", "badge-active")]
        [InlineData("paid", "badge-active")]
        [InlineData("no-debt", "badge-active")]
        [InlineData("has-debt", "badge-has-debt")]
        [InlineData("partial", "badge-has-debt")]
        [InlineData("over-limit", "badge-over-limit")]
        [InlineData("danger", "badge-over-limit")]
        [InlineData("locked", "badge-locked")]
        [InlineData("stopped", "badge-locked")]
        [InlineData("unpaid", "badge-locked")]
        public void BadgeTrangThai_AnhXaDungLoaiTrangThai(string loai, string expectedClass)
        {
            // Arrange
            var badge = new BadgeTrangThai { Loai = loai };

            // Act
            var actualClass = badge.LayCssClass();

            // Assert
            Assert.Equal(expectedClass, actualClass);
        }

        [Fact]
        public void BadgeTrangThai_UuTienCssClassTuyChinh_NeuDuocChiDinh()
        {
            // Arrange
            var badge = new BadgeTrangThai
            {
                Loai = "active",
                CssClass = "custom-badge-style"
            };

            // Act
            var actualClass = badge.LayCssClass();

            // Assert
            Assert.Equal("custom-badge-style", actualClass);
        }
    }
}
