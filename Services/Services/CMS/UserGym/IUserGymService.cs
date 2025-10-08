using Common;
using SharedModels.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.UserGym
{
    public interface IUserGymService 
    {
        Task<ResponseModel<List<UserGymDto>>> GetUserInfo(int UserId, CancellationToken cancellationToken);

        // Returns the number of users assigned to a gym. ManagerId is used for access validation.
        Task<ResponseModel<int>> GetGymUserCount(int gymId, int managerId, CancellationToken cancellationToken);
    }
}
