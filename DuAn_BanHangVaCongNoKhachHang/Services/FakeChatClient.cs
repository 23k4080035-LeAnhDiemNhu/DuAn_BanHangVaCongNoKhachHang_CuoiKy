using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Fake/Mock IChatClient phục vụ kiểm thử và hoạt động khi chưa cấu hình AI provider thật.
/// Đảm bảo tính độc lập, không phụ thuộc internet và không tốn phí.
/// </summary>
public class FakeChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("FakeChatClient");

    public bool SimulateError { get; set; } = false;

    public string ErrorMessage { get; set; } = "Mô phỏng lỗi AI client phản hồi thất bại.";

    public int DelayMilliseconds { get; set; } = 0;

    public string? CustomResponseText { get; set; }

    public List<ChatMessage> LastChatMessages { get; } = new();

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (DelayMilliseconds > 0)
        {
            await Task.Delay(DelayMilliseconds, cancellationToken);
        }

        if (SimulateError)
        {
            throw new InvalidOperationException(ErrorMessage);
        }

        LastChatMessages.Clear();
        LastChatMessages.AddRange(chatMessages);

        string text;
        if (!string.IsNullOrWhiteSpace(CustomResponseText))
        {
            text = CustomResponseText;
        }
        else
        {
            var fullPrompt = string.Join("\n", System.Linq.Enumerable.Select(chatMessages, m => m.Text));
            string greeting = "Kính gửi Quý khách hàng,";
            var matchKh = System.Text.RegularExpressions.Regex.Match(fullPrompt, @"Khách hàng nhận thư:\s*([^\r\n(]+)");
            if (matchKh.Success)
            {
                greeting = $"Kính gửi Quý khách hàng {matchKh.Groups[1].Value.Trim()},";
            }

            if (fullPrompt.Contains("Khẩn trương"))
            {
                text = $"TIÊU ĐỀ: [KHẨN] THÔNG BÁO CÔNG NỢ QUÁ HẠN VÀ ĐỀ NGHỊ THANH TOÁN\n\n{greeting}\n\nBộ phận kế toán đề nghị Quý khách hàng khẩn trương kiểm tra và ưu tiên thanh toán ngay các khoản nợ quá hạn lâu ngày dưới đây để đảm bảo hạn mức không bị gián đoạn.\n\nTrân trọng!";
            }
            else if (fullPrompt.Contains("Nhẹ nhàng"))
            {
                text = $"TIÊU ĐỀ: THÔNG BÁO ĐỐI SOÁT CÔNG NỢ ĐỊNH KỲ\n\n{greeting}\n\nKính gửi Quý khách bảng đối soát công nợ định kỳ. Rất mong Quý khách phối hợp kiểm tra và thanh toán các khoản sắp đến hạn khi thuận tiện.\n\nTrân trọng cảm ơn sự hợp tác của Quý khách!";
            }
            else
            {
                text = $"TIÊU ĐỀ: THÔNG BÁO CÔNG NỢ VÀ ĐỀ NGHỊ THANH TOÁN\n\n{greeting}\n\nBộ phận kế toán xin gửi thông báo chi tiết công nợ đến ngày chốt đối soát. Kính đề nghị Quý khách kiểm tra và phối hợp thanh toán các khoản đến hạn.\n\nTrân trọng!";
            }
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(chatMessages, options, cancellationToken);
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = new List<AIContent> { new TextContent(response.Text) }
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(FakeChatClient) ? this : null;
    }

    public void Dispose()
    {
    }
}
