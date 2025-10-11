using Common;
using System.Threading;
using System.Threading.Tasks;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.Gym
{
    public interface IGymService : IScopedDependency
    {
        Task<ResponseModel> UpdateBasicInfoAsync(int gymId, int managerUserId, UpdateGymBasicDto dto, CancellationToken cancellationToken);

        Task<ResponseModel> UpdateAddressAsync(int gymId, int managerUserId, UpdateGymAddressDto dto, CancellationToken cancellationToken);

        Task<ResponseModel> UpdateSocialLinksAsync(int gymId, int managerUserId, UpdateGymSocialDto dto, CancellationToken cancellationToken);

        Task<ResponseModel<GymSelectDto>> GetByIdAsync(int gymId, int userId, CancellationToken cancellationToken);
    }
}
