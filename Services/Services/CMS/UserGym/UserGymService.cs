using AutoMapper;
using Common;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.UserGym
{
    public class UserGymService : IScopedDependency, IUserGymService
    {
        private readonly IRepository<GymUser> mainRepository;
        private readonly IMapper mapper;
        public UserGymService(IRepository<Entities.GymUser> MainRepository, IMapper Mapper)
        {
            mainRepository = MainRepository;

            mapper = Mapper;
        }
        public async Task<ResponseModel<List<UserGymDto>>> GetUserInfo(int UserId, CancellationToken cancellationToken)
        {

            var res = await mainRepository.TableNoTracking.Include(a =>a.Gym).Where(a=>a.UserId == UserId).ToListAsync();
            if (res.Count > 0)
            { var data = mapper.Map<List<UserGymDto>>(res);
                return new ResponseModel<List<UserGymDto>>(true, data);
              
            }
            else {
                 return new ResponseModel<List<UserGymDto>>(false, null, "اطلاعاتی یافت نشد");
            }
         
        }

        public async Task<ResponseModel<int>> GetGymUserCount(int gymId, int managerId, CancellationToken cancellationToken)
        {
            // Verify the manager is linked to this gym (authorization gate)
            var isManagerLinked = await mainRepository.TableNoTracking
                .AnyAsync(g => g.GymId == gymId && g.UserId == managerId, cancellationToken);

            if (!isManagerLinked)
                return new ResponseModel<int>(false, 0, "دسترسی مجاز نیست");

            // Count all users in the gym
            var count = await mainRepository.TableNoTracking
                .CountAsync(g => g.GymId == gymId, cancellationToken);

            return new ResponseModel<int>(true, count);
        }
    }
}
