namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Cấu hình tài khoản thụ hưởng thanh toán qua mã VietQR
/// </summary>
public class VietQrSettings
{
    public string BankId { get; set; } = "ICB";
    public string BankBin { get; set; } = "970415";
    public string BankName { get; set; } = "VietinBank";
    public string AccountNumber { get; set; } = "107879627729";
    public string AccountName { get; set; } = "LE ANH DIEM NHU";
    public string Template { get; set; } = "compact2";
}
