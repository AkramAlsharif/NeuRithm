using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public interface IAvatarCatalogService
{
    Task<IReadOnlyList<AvatarSummary>> GetAvatarsAsync(CancellationToken cancellationToken = default);
}
