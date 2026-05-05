namespace Neurithm.Web.Services;

public interface IClientLogService
{
    Task WriteAsync(string category, string message);
    Task<IReadOnlyList<string>> ReadRecentAsync(int count = 20);
}
