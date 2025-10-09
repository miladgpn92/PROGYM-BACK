using Common;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.PracticeCategory
{
    public interface IPracticeCategoryService
    {
        Task<ResponseModel<PracticeCategorySelectDto>> CreateAsync(int gymId, int userId, PracticeCategoryDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, PracticeCategoryDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel<List<PracticeCategorySelectDto>>> GetListAsync(int gymId, int userId, string? q, CancellationToken cancellationToken);
    }
}
