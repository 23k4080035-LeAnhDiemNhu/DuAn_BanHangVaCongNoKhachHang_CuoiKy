namespace BanHang.CoreBusiness;

/// <summary>
/// Các lựa chọn giọng văn cho email nhắc nợ tự động
/// </summary>
public enum GiongVanEmail
{
    /// <summary>
    /// Nhẹ nhàng, thăm hỏi thiện chí, phù hợp khoản nợ mới đến hạn hoặc khách hàng truyền thống
    /// </summary>
    NheNhang = 1,

    /// <summary>
    /// Tiêu chuẩn, chuẩn mực công văn thương mại trang trọng
    /// </summary>
    TieuChuan = 2,

    /// <summary>
    /// Khẩn trương, nghiêm túc, nhấn mạnh nghĩa vụ hợp đồng (vẫn giữ phong thái chuyên nghiệp, không đe dọa)
    /// </summary>
    KhanTruong = 3
}
