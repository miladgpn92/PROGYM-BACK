using Common;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.Practices
{
    public interface IPracticeService
    {
        Task<ResponseModel<PracticeSelectDto>> CreateAsync(int gymId, int userId, PracticeDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, PracticeDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel<List<PracticeSelectDto>>> GetListAsync(int gymId, int userId, string q, CancellationToken cancellationToken);
        Task<ResponseModel<PracticeSelectDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
    }
}
