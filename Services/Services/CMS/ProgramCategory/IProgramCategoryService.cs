using Common;
using DariaCMS.Common;
using SharedModels.Dtos.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.ProgramCategory
{
    public interface IProgramCategoryService
    {
        Task<ResponseModel<ProgramCategorySelectDto>> CreateAsync(int gymId, int userId, ProgramCategoryDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, ProgramCategoryDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel<PagedResult<ProgramCategorySelectDto>>> GetListAsync(int gymId, int userId, string? q, Pageres pager, CancellationToken cancellationToken);
    }
}
