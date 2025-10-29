using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using DariaCMS.Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.Site.GymLanding
{
    public interface IGymLandingService : IScopedDependency
    {
        Task<List<GymLandingListItemDto>> GetLatestGymsAsync(int count, CancellationToken cancellationToken);
        Task<GymLandingDetailDto?> GetGymBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<PagedResult<GymLandingListItemDto>> GetGymsAsync(Pageres pager, CancellationToken cancellationToken);
    }
}
