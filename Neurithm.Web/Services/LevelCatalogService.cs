using System.Net.Http.Json;
using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public sealed class LevelCatalogService : ILevelCatalogService
{
    private readonly HttpClient _httpClient;

    public LevelCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<LevelSummary>> GetLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _httpClient.GetFromJsonAsync<List<LevelSummary>>("data/levels/levels-index.json", cancellationToken);
        return levels ?? [];
    }
}
