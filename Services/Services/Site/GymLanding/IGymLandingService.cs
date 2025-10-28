using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.Site.GymLanding
{
    public interface IGymLandingService : IScopedDependency
    {
        Task<List<GymLandingListItemDto>> GetLatestGymsAsync(int count, CancellationToken cancellationToken);
    }
}
