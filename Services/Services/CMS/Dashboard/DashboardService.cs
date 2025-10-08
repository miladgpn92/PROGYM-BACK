using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Dashboard;
using Common.Enums;
using Entities;
using Common;

namespace Services.Services.CMS.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<Practice> _practiceRepo;
        private readonly IRepository<Program> _programRepo;

        public DashboardService(
            IRepository<GymUser> gymUserRepo,
            IRepository<Practice> practiceRepo,
            IRepository<Program> programRepo)
        {
            _gymUserRepo = gymUserRepo;
            _practiceRepo = practiceRepo;
            _programRepo = programRepo;
        }

        public async Task<ResponseModel<ManagerCountsDto>> GetManagerCountsAsync(int gymId, int managerUserId, CancellationToken cancellationToken)
        {
            var managerRole = UsersRole.manager;
            var athleteRole = UsersRole.athlete;

            // Validate that the requested gym is managed by this manager
            var isManagerOfGym = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerUserId && gu.Role == managerRole, cancellationToken);

            if (!isManagerOfGym)
            {
                return new ResponseModel<ManagerCountsDto>(false, null, "شما مدیر این باشگاه نیستید!");
            }

            // Users (athletes) in the specified gym
            var usersCount = await _gymUserRepo.TableNoTracking
                .Where(gu => gu.GymId == gymId && gu.Role == athleteRole)
                .Select(gu => gu.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Practices created by manager
            var practicesCount = await _practiceRepo.TableNoTracking
                .Where(p => p.UserId == managerUserId)
                .CountAsync(cancellationToken);

            // Programs owned or submitted by manager (no gym relation in schema)
            var programsCount = await _programRepo.TableNoTracking
                .Where(p => p.OwnerId == managerUserId || p.SubmitterUserId == managerUserId)
                .CountAsync(cancellationToken);

            var payload = new ManagerCountsDto
            {
                Users = usersCount,
                Practices = practicesCount,
                Programs = programsCount
            };

            return new ResponseModel<ManagerCountsDto>(true, payload);
        }
    }
}
