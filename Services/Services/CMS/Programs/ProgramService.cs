using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.Programs
{
    public class ProgramService : IScopedDependency, IProgramService
    {
        private readonly IRepository<Entities.Program> _programRepo;
        private readonly IRepository<ProgramPractice> _programPracticeRepo;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<ApplicationUser> _userRepo;
        private readonly IRepository<Entities.Practice> _practiceRepo;
        private readonly IMapper _mapper;

        public ProgramService(
            IRepository<Entities.Program> programRepo,
            IRepository<ProgramPractice> programPracticeRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<ApplicationUser> userRepo,
            IRepository<Entities.Practice> practiceRepo,
            IMapper mapper)
        {
            _programRepo = programRepo;
            _programPracticeRepo = programPracticeRepo;
            _gymUserRepo = gymUserRepo;
            _userRepo = userRepo;
            _practiceRepo = practiceRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel<ProgramSelectDto>> CreateAsync(int gymId, int userId, ProgramDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<ProgramSelectDto>(false, null, "Access denied");

            int? ownerId = dto.OwnerId;
            if (ownerId.HasValue)
            {
                var ownerInGym = await _gymUserRepo.TableNoTracking.AnyAsync(x => x.GymId == gymId && x.UserId == ownerId.Value, cancellationToken);
                if (!ownerInGym)
                    return new ResponseModel<ProgramSelectDto>(false, null, "Owner not found in current gym");
            }

            // optional: validate practices existence
            if (dto.Practices != null && dto.Practices.Count > 0)
            {
                var practiceIds = dto.Practices.Where(p => p.PracticeId.HasValue).Select(p => p.PracticeId.Value).Distinct().ToList();
                var existsCount = await _practiceRepo.TableNoTracking.CountAsync(p => practiceIds.Contains(p.Id), cancellationToken);
                if (existsCount != practiceIds.Count)
                    return new ResponseModel<ProgramSelectDto>(false, null, "One or more practices are invalid");
            }

            var entity = dto.ToEntity(_mapper);
            entity.SubmitterUserId = userId;
            entity.OwnerId = ownerId; // may be null for Global programs
            // compute CountOfPractice strictly from provided practices before save
            entity.CountOfPractice = dto.Practices?.Count ?? 0;
            entity.CreateDate = System.DateTime.Now;

            await _programRepo.AddAsync(entity, cancellationToken);

            if (dto.Practices != null && dto.Practices.Count > 0)
            {
                foreach (var p in dto.Practices)
                {
                    // Validate typed data based on practice type
                    if (p.Type == PracticeType.Set)
                    {
                        if (!p.SetCount.HasValue || !p.MovementCount.HasValue || !p.Rest.HasValue)
                            return new ResponseModel<ProgramSelectDto>(false, null, "Invalid practice data for Set: require setCount, movementCount, rest");
                        if (p.SetCount <= 0 || p.MovementCount <= 0 || p.Rest < 0)
                            return new ResponseModel<ProgramSelectDto>(false, null, "Invalid values for Set");
                    }
                    else if (p.Type == PracticeType.Time)
                    {
                        if (!p.Duration.HasValue || !p.Rest.HasValue)
                            return new ResponseModel<ProgramSelectDto>(false, null, "Invalid practice data for Time: require duration, rest");
                        if (p.Duration <= 0 || p.Rest < 0)
                            return new ResponseModel<ProgramSelectDto>(false, null, "Invalid values for Time");
                    }

                    var pp = new ProgramPractice
                    {
                        ProgramId = entity.Id,
                        PracticeId = p.PracticeId ?? 0,
                        Type = p.Type,
                        SetCount = p.Type == PracticeType.Set ? p.SetCount : null,
                        MovementCount = p.Type == PracticeType.Set ? p.MovementCount : null,
                        Duration = p.Type == PracticeType.Time ? p.Duration : null,
                        Rest = p.Rest
                    };
                    await _programPracticeRepo.AddAsync(pp, cancellationToken);
                }
            }

            var model = await _programRepo.TableNoTracking
                .Where(x => x.Id == entity.Id)
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .ProjectTo<ProgramSelectDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            return new ResponseModel<ProgramSelectDto>(true, model);
        }

        public async Task<ResponseModel> DeletePracticeAsync(int gymId, int userId, int programPracticeId, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var pp = await _programPracticeRepo.Table.FirstOrDefaultAsync(x => x.Id == programPracticeId, cancellationToken);
            if (pp == null)
                return new ResponseModel(false, "Not found");

            await _programPracticeRepo.DeleteAsync(pp, cancellationToken);
            return new ResponseModel(true, "");
        }

        // practiceData is now strongly-typed via fields on ProgramPractice

        public async Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, ProgramDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _programRepo.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            entity.Title = dto.Title;
            entity.Type = dto.Type;
            // recompute CountOfPractice from current relations
            entity.CountOfPractice = await _programPracticeRepo.TableNoTracking.CountAsync(x => x.ProgramId == entity.Id, cancellationToken);

            if (dto.OwnerId.HasValue)
            {
                var newOwnerId = dto.OwnerId.Value;
                if (newOwnerId != entity.OwnerId)
                {
                    var ownerInGym = await _gymUserRepo.TableNoTracking.AnyAsync(x => x.GymId == gymId && x.UserId == newOwnerId, cancellationToken);
                    if (!ownerInGym)
                        return new ResponseModel(false, "Owner not found in current gym");
                    entity.OwnerId = newOwnerId;
                }
            }

            await _programRepo.UpdateAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _programRepo.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            await _programRepo.DeleteAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<List<ProgramSelectDto>>> GetListAsync(int gymId, int userId, string q, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<List<ProgramSelectDto>>(false, null, "Access denied");

            var query = _programRepo.TableNoTracking;
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.Contains(q));

            var list = await query
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .OrderByDescending(x => x.Id)
                .ProjectTo<ProgramSelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new ResponseModel<List<ProgramSelectDto>>(true, list);
        }

        public async Task<ResponseModel<ProgramDetailDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<ProgramDetailDto>(false, null, "Access denied");

            var item = await _programRepo.TableNoTracking
                .Where(x => x.Id == id)
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .Include(x => x.ProgramPractices)
                    .ThenInclude(pp => pp.Practice)
                .ProjectTo<ProgramDetailDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null)
                return new ResponseModel<ProgramDetailDto>(false, null, "Not found");

            return new ResponseModel<ProgramDetailDto>(true, item);
        }
    }
}
