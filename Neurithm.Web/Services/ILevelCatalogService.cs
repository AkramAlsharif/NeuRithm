using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public interface ILevelCatalogService
{
    Task<IReadOnlyList<LevelSummary>> GetLevelsAsync(CancellationToken cancellationToken = default);
}
