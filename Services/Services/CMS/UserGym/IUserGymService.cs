using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.UserGym
{
    public interface IUserGymService
    {
        Task<ResponseModel<List<UserGymDto>>> GetUserInfo(int UserId, CancellationToken cancellationToken);

        // Returns the number of users assigned to a gym. ManagerId is used for access validation.
        Task<ResponseModel<int>> GetGymUserCount(int gymId, int managerId, CancellationToken cancellationToken);

        // Returns all users assigned to the specified gym.
        Task<ResponseModel<List<GymUserListItemDto>>> GetGymUsersAsync(int gymId, CancellationToken cancellationToken);

        // Creates or updates a user and assigns them to the gym with the selected role.
        Task<ResponseModel> AddUserToGymAsync(int gymId, GymUserCreateDto dto, CancellationToken cancellationToken);
    }
}
