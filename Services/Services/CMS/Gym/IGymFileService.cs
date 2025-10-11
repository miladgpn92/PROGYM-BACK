using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.Gym
{
    public interface IGymFileService : IScopedDependency
    {
        Task<ResponseModel<GymFilePagedResult>> GetListAsync(int gymId, int userId, GymFileListRequest request, CancellationToken cancellationToken);

        Task<ResponseModel<List<GymFileDto>>> UploadAsync(int gymId, int userId, GymFileUploadRequest request, CancellationToken cancellationToken);

        Task<ResponseModel> DeleteAsync(int gymId, int userId, GymFileDeleteRequest request, CancellationToken cancellationToken);
    }
}
