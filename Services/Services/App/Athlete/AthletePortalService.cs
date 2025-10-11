using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;

namespace Services.Services.App.Athlete
{
    public class AthletePortalService : IScopedDependency, IAthletePortalService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<AthleteData> _athleteDataRepo;
        private readonly IRepository<UserProgram> _userProgramRepo;
        private readonly IRepository<Program> _programRepo;
        private readonly IRepository<Exercise> _exerciseRepo;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<Entities.Gym> _gymRepo;
        private readonly IMapper _mapper;

        public AthletePortalService(
            UserManager<ApplicationUser> userManager,
            IRepository<AthleteData> athleteDataRepo,
            IRepository<UserProgram> userProgramRepo,
            IRepository<Program> programRepo,
            IRepository<Exercise> exerciseRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<Entities.Gym> gymRepo,
            IMapper mapper)
        {
            _userManager = userManager;
            _athleteDataRepo = athleteDataRepo;
            _userProgramRepo = userProgramRepo;
            _programRepo = programRepo;
            _exerciseRepo = exerciseRepo;
            _gymUserRepo = gymUserRepo;
            _gymRepo = gymRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel<List<AthleteDataDto>>> GetAthleteDataAsync(int userId, CancellationToken cancellationToken)
        {
            var items = await _athleteDataRepo.TableNoTracking
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SubmitDate)
                .Select(a => new AthleteDataDto
                {
                    Id = a.Id,
                    SubmitDate = a.SubmitDate,
                    Height = a.Height,
                    Age = a.Age,
                    Weight = a.Weight
                })
                .ToListAsync(cancellationToken);

            return new ResponseModel<List<AthleteDataDto>>(true, items);
        }

        public async Task<ResponseModel<int>> AddAthleteDataAsync(int userId, AthleteDataCreateDto dto, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.UserRole != UsersRole.athlete)
                return new ResponseModel<int>(false, 0, "User not found or invalid role");

            var entity = new AthleteData
            {
                UserId = userId,
                SubmitDate = DateTime.Now,
                Height = dto.Height,
                Age = dto.Age,
                Weight = dto.Weight
            };

            await _athleteDataRepo.AddAsync(entity, cancellationToken);
            return new ResponseModel<int>(true, entity.Id);
        }

        public async Task<ResponseModel<AthleteCurrentProgramDto>> GetCurrentProgramAsync(int userId, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            var baseQuery = _userProgramRepo.TableNoTracking
                .Where(up => up.UserId == userId)
                .Select(up => new
                {
                    up.Id,
                    up.ProgramId,
                    up.StartDate,
                    up.EndDate,
                    ProgramTitle = up.Program.Title
                });

            var currentProgram = await baseQuery
                .Where(up => !up.EndDate.HasValue || up.EndDate.Value.Date >= today)
                .OrderByDescending(up => up.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentProgram == null)
            {
                currentProgram = await baseQuery
                    .OrderByDescending(up => up.StartDate)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (currentProgram == null)
                return new ResponseModel<AthleteCurrentProgramDto>(false, null, "Program not assigned");

            var dto = new AthleteCurrentProgramDto
            {
                UserProgramId = currentProgram.Id,
                ProgramId = currentProgram.ProgramId,
                ProgramTitle = currentProgram.ProgramTitle,
                StartDate = currentProgram.StartDate,
                EndDate = currentProgram.EndDate
            };

            return new ResponseModel<AthleteCurrentProgramDto>(true, dto);
        }

        public async Task<ResponseModel<ProgramDetailDto>> GetProgramDetailAsync(int userId, int programId, CancellationToken cancellationToken)
        {
            var hasAccess = await _userProgramRepo.TableNoTracking
                .AnyAsync(up => up.UserId == userId && up.ProgramId == programId, cancellationToken);

            if (!hasAccess)
                return new ResponseModel<ProgramDetailDto>(false, null, "Program not assigned to user");

            var model = await _programRepo.TableNoTracking
                .Where(p => p.Id == programId)
                .ProjectTo<ProgramDetailDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
                return new ResponseModel<ProgramDetailDto>(false, null, "Program not found");

            return new ResponseModel<ProgramDetailDto>(true, model);
        }

        public async Task<ResponseModel<int>> CreateExerciseAsync(int userId, ExerciseCreateDto dto, CancellationToken cancellationToken)
        {
            var hasProgram = await _userProgramRepo.TableNoTracking
                .AnyAsync(up => up.UserId == userId && up.ProgramId == dto.ProgramId, cancellationToken);

            if (!hasProgram)
                return new ResponseModel<int>(false, 0, "Program not assigned to user");

            var exercise = new Exercise
            {
                UserId = userId,
                ProgramId = dto.ProgramId,
                Date = dto.Date?.Date ?? DateTime.Now,
                Complete = false
            };

            await _exerciseRepo.AddAsync(exercise, cancellationToken);
            return new ResponseModel<int>(true, exercise.Id);
        }

        public async Task<ResponseModel> CompleteExerciseAsync(int userId, int exerciseId, CancellationToken cancellationToken)
        {
            var exercise = await _exerciseRepo.Table.FirstOrDefaultAsync(
                e => e.Id == exerciseId && e.UserId == userId,
                cancellationToken);

            if (exercise == null)
                return new ResponseModel(false, "Exercise not found");

            if (exercise.Complete)
                return new ResponseModel(true);

            exercise.Complete = true;
            await _exerciseRepo.UpdateAsync(exercise, cancellationToken);
            return new ResponseModel(true);
        }

        public async Task<ResponseModel<GymSelectDto>> GetGymAsync(int userId, int gymId, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.UserId == userId && gu.GymId == gymId && gu.Role == UsersRole.athlete, cancellationToken);

            if (!hasAccess)
                return new ResponseModel<GymSelectDto>(false, null, "Access denied");

            var model = await _gymRepo.TableNoTracking
                .Where(g => g.Id == gymId)
                .ProjectTo<GymSelectDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
                return new ResponseModel<GymSelectDto>(false, null, "Gym not found");

            return new ResponseModel<GymSelectDto>(true, model);
        }

        public async Task<ResponseModel<ProfileDto>> GetProfileAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.UserRole != UsersRole.athlete)
                return new ResponseModel<ProfileDto>(false, null, "User not found");

            var dto = new ProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Family = user.Family,
                Gender = user.Gender,
                UserPicUrl = user.PicUrl
            };

            return new ResponseModel<ProfileDto>(true, dto);
        }

        public async Task<ResponseModel> UpdateProfileAsync(int userId, AthleteProfileUpdateDto dto, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.UserRole != UsersRole.athlete)
                return new ResponseModel(false, "User not found");

            user.Name = dto.Name;
            user.Family = dto.Family;
            user.Gender = dto.Gender;
            user.PicUrl = dto.PicUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Update failed";
                return new ResponseModel(false, error);
            }

            return new ResponseModel(true);
        }
    }
}
