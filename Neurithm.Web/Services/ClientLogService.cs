using Microsoft.JSInterop;

namespace Neurithm.Web.Services;

public sealed class ClientLogService : IClientLogService
{
    private readonly IJSRuntime _jsRuntime;

    public ClientLogService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Task WriteAsync(string category, string message)
    {
        return _jsRuntime.InvokeVoidAsync("neurithmClientLogs.write", category, message).AsTask();
    }

    public async Task<IReadOnlyList<string>> ReadRecentAsync(int count = 20)
    {
        var logs = await _jsRuntime.InvokeAsync<string[]>("neurithmClientLogs.readRecent", count);
        return logs;
    }
}
