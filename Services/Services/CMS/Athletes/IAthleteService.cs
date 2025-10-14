using System.Threading;
using System.Threading.Tasks;
using Common;
using DariaCMS.Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.Athletes
{
    public interface IAthleteService
    {
        Task<ResponseModel<int>> CreateOrJoinAsync(int gymId, int managerId, AthleteCreateDto dto, CancellationToken cancellationToken);
        Task<ResponseModel<PagedResult<AthleteSelectDto>>> GetListAsync(int gymId, int managerId, string q, Pageres pager, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateAsync(int gymId, int managerId, int userId, AthleteUpdateDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken);
        Task<ResponseModel<AthleteDetailDto>> GetByIdAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken);
    }
}
