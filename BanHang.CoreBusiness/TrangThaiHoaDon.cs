namespace BanHang.CoreBusiness;

/// <summary>
/// Trạng thái thanh toán của hóa đơn
/// </summary>
public enum TrangThaiHoaDon
{
    /// <summary>
    /// Chưa thanh toán đồng nào (Đã thanh toán = 0)
    /// </summary>
    ChuaThanhToan = 0,

    /// <summary>
    /// Đã trả một phần tiền nhưng chưa hết nợ
    /// </summary>
    ThanhToanMotPhan = 1,

    /// <summary>
    /// Đã thanh toán đầy đủ 100% hóa đơn
    /// </summary>
    DaThanhToan = 2
}
