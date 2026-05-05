using System.Net.Http.Json;
using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public sealed class AvatarCatalogService : IAvatarCatalogService
{
    private readonly HttpClient _httpClient;

    public AvatarCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AvatarSummary>> GetAvatarsAsync(CancellationToken cancellationToken = default)
    {
        var avatars = await _httpClient.GetFromJsonAsync<List<AvatarSummary>>("data/avatars/avatars.json", cancellationToken);
        return avatars ?? [];
    }
}
