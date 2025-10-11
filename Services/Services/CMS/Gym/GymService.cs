using Common;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Enums;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Services.Services.CMS.Gym
{
    public class GymService : IScopedDependency, IGymService
    {
        private readonly IRepository<Entities.Gym> _gymRepo;
        private readonly IRepository<Entities.GymUser> _gymUserRepo;
        private readonly IMapper _mapper;

        public GymService(
            IRepository<Entities.Gym> gymRepo,
            IRepository<Entities.GymUser> gymUserRepo,
            IMapper mapper)
        {
            _gymRepo = gymRepo;
            _gymUserRepo = gymUserRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel> UpdateBasicInfoAsync(int gymId, int managerUserId, UpdateGymBasicDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerUserId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var gym = await _gymRepo.Table.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel(false, "Gym not found");

            gym.Title = dto.Title;
            gym.LogoUrl = dto.LogoUrl;

            await _gymRepo.UpdateAsync(gym, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> UpdateAddressAsync(int gymId, int managerUserId, UpdateGymAddressDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerUserId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var gym = await _gymRepo.Table.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel(false, "Gym not found");

            gym.ContactUsPhoneNumber = dto.ContactUsPhoneNumber;
            gym.Phone = dto.Phone;
            gym.Lat = dto.Lat;
            gym.Lng = dto.Lng;

            await _gymRepo.UpdateAsync(gym, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> UpdateSocialLinksAsync(int gymId, int managerUserId, UpdateGymSocialDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerUserId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var gym = await _gymRepo.Table.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel(false, "Gym not found");

            gym.InstagramLink = dto.InstagramLink;
            gym.TelegramLink = dto.TelegramLink;
            gym.EitaaLink = dto.EitaaLink;
            gym.BaleLink = dto.BaleLink;

            await _gymRepo.UpdateAsync(gym, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<GymSelectDto>> GetByIdAsync(int gymId, int userId, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<GymSelectDto>(false, null, "Access denied");

            var model = await _gymRepo.TableNoTracking
                .Where(g => g.Id == gymId)
                .ProjectTo<GymSelectDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
                return new ResponseModel<GymSelectDto>(false, null, "Not found");

            return new ResponseModel<GymSelectDto>(true, model);
        }
    }
}
