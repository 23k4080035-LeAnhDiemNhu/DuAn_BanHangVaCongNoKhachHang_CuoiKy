namespace BanHang.CoreBusiness;

/// <summary>
/// Ngoại lệ ném ra khi có vi phạm quy tắc nghiệp vụ trong hệ thống
/// (Ví dụ: Vượt hạn mức công nợ, xuất hàng khi tồn kho không đủ, thanh toán số tiền âm,...)
/// </summary>
public class QuyTacNghiepVuException : Exception
{
    public QuyTacNghiepVuException()
    {
    }

    public QuyTacNghiepVuException(string message) 
        : base(message)
    {
    }

    public QuyTacNghiepVuException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
