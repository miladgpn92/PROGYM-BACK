using System.Threading;
using System.Threading.Tasks;
using SharedModels.Dtos.Dashboard;
using Common;

namespace Services.Services.CMS.Dashboard
{
    public interface IDashboardService : IScopedDependency
    {
        Task<ResponseModel<ManagerCountsDto>> GetManagerCountsAsync(int gymId, int managerUserId, CancellationToken cancellationToken);
    }
}
