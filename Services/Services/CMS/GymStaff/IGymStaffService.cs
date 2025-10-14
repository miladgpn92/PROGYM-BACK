using System.Threading;
using System.Threading.Tasks;
using Common;
using DariaCMS.Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.GymStaff
{
    public interface IGymStaffService : IScopedDependency
    {
        Task<ResponseModel<int>> CreateAsync(int gymId, int managerId, GymStaffCreateDto dto, CancellationToken cancellationToken);

        Task<ResponseModel<PagedResult<GymStaffSelectDto>>> GetListAsync(int gymId, int managerId, string search, Pageres pager, CancellationToken cancellationToken);

        Task<ResponseModel<GymStaffSelectDto>> GetByIdAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken);

        Task<ResponseModel> UpdateAsync(int gymId, int managerId, int userId, GymStaffUpdateDto dto, CancellationToken cancellationToken);

        Task<ResponseModel> DeleteAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken);
    }
}
