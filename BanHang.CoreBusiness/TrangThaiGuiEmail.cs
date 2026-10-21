namespace BanHang.CoreBusiness;

/// <summary>
/// Trạng thái gửi của email nhắc nợ trong nhật ký hệ thống
/// </summary>
public enum TrangThaiGuiEmail
{
    /// <summary>
    /// Đang xử lý / Đã tiếp nhận yêu cầu và bắt đầu gửi (In-flight / Processing)
    /// </summary>
    DangXuLy = 0,

    /// <summary>
    /// Gửi thử / Giả lập trong môi trường phát triển & kiểm thử (Simulated)
    /// </summary>
    GuiThu = 1,

    /// <summary>
    /// Đã được dịch vụ tiếp nhận xử lý gửi đi (Service Accepted)
    /// </summary>
    TiepNhan = 2,

    /// <summary>
    /// Thất bại do lỗi kết nối, địa chỉ email từ chối, hoặc sự cố dịch vụ
    /// </summary>
    ThatBai = 3,

    /// <summary>
    /// Chưa rõ kết quả (chưa nhận được phản hồi xác nhận từ dịch vụ)
    /// </summary>
    ChuaRoKetQua = 4
}
