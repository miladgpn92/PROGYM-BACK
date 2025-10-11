using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Enums;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.Services.CMS.Athletes;
using Services.Services;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.Athletes
{
    public class AthleteService : IScopedDependency, IAthleteService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<Entities.Gym> _gymRepo;
        private readonly IRepository<AthleteData> _athleteDataRepo;
        private readonly IRepository<UserProgram> _userProgramRepo;
        private readonly IMapper _mapper;
        private readonly ISMSService _smsService;
        private readonly ProjectSettings _projectSettings;

        public AthleteService(
            UserManager<ApplicationUser> userManager,
            IRepository<GymUser> gymUserRepo,
            IRepository<Entities.Gym> gymRepo,
            IRepository<AthleteData> athleteDataRepo,
            IRepository<UserProgram> userProgramRepo,
            IMapper mapper,
            ISMSService smsService,
            IOptionsSnapshot<ProjectSettings> settings)
        {
            _userManager = userManager;
            _gymUserRepo = gymUserRepo;
            _gymRepo = gymRepo;
            _athleteDataRepo = athleteDataRepo;
            _userProgramRepo = userProgramRepo;
            _mapper = mapper;
            _smsService = smsService;
            _projectSettings = settings.Value;
        }

        public async Task<ResponseModel<int>> CreateOrJoinAsync(int gymId, int managerId, AthleteCreateDto dto, CancellationToken cancellationToken)
        {
            var isManagerLinked = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerId && gu.Role == UsersRole.manager, cancellationToken);
            if (!isManagerLinked)
                return new ResponseModel<int>(false, 0, "Unauthorized manager for this gym");

            var gym = await _gymRepo.TableNoTracking.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel<int>(false, 0, "Gym not found");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber, cancellationToken);
            bool isNew = false;
            if (user == null)
            {
                isNew = true;
                user = new ApplicationUser
                {
                    Name = dto.Name,
                    Family = dto.Family,
                    PhoneNumber = dto.PhoneNumber,
                    Gender = dto.Gender,
                    UserName = dto.PhoneNumber,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    IsRegisterComplete = true,
                    UserRole = UsersRole.athlete
                };

                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                    return new ResponseModel<int>(false, 0, createRes.Errors.FirstOrDefault()?.Description ?? "Create user failed");
                await _userManager.AddToRoleAsync(user, UsersRole.athlete.ToString());

                var defaultPwd = dto.PhoneNumber.Length >= 6 ? dto.PhoneNumber[^6..] : dto.PhoneNumber;
                try { await _userManager.AddPasswordAsync(user, defaultPwd); } catch { /* ignore weak policy issues */ }

                var initialData = new AthleteData
                {
                    UserId = user.Id,
                    SubmitDate = DateTime.Now,
                    Height = 0,
                    Age = 0,
                    Weight = 0
                };
                await _athleteDataRepo.AddAsync(initialData, cancellationToken);
            }
            else
            {
                // ensure up-to-date basic info if provided
                user.Name = dto.Name;
                user.Family = dto.Family;
                user.Gender = dto.Gender;
                await _userManager.UpdateAsync(user);
            }

            // Join to current gym if not already
            var existsInGym = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == user.Id, cancellationToken);
            if (!existsInGym)
            {
                var gu = new GymUser { GymId = gymId, UserId = user.Id, Role = UsersRole.athlete };
                await _gymUserRepo.AddAsync(gu, cancellationToken);
            }

            // Send SMS notification
            var text = $"You joined to {gym.Title}";
            try
            {
                await _smsService.SendSMSAsync(_projectSettings.ProjectSetting.SMSToken,
                    _projectSettings.ProjectSetting.BaseUrl,
                    user.PhoneNumber,
                    text);
            }
            catch { /* ignore sms failures */ }

            return new ResponseModel<int>(true, user.Id);
        }

        public async Task<ResponseModel<List<AthleteSelectDto>>> GetListAsync(int gymId, int managerId, string q, CancellationToken cancellationToken)
        {
            var isManagerLinked = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerId && gu.Role == UsersRole.manager, cancellationToken);
            if (!isManagerLinked)
                return new ResponseModel<List<AthleteSelectDto>>(false, null, "Unauthorized manager for this gym");

            var query = from gu in _gymUserRepo.TableNoTracking
                        join u in _userManager.Users on gu.UserId equals u.Id
                        where gu.GymId == gymId && gu.Role == UsersRole.athlete
                        select new AthleteSelectDto
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Family = u.Family,
                            PhoneNumber = u.PhoneNumber,
                            Gender = u.Gender
                        };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => (x.Name ?? "").Contains(term) || (x.Family ?? "").Contains(term) || (x.PhoneNumber ?? "").Contains(term));
            }

            var list = await query.OrderBy(x => x.Family).ThenBy(x => x.Name).ToListAsync(cancellationToken);
            return new ResponseModel<List<AthleteSelectDto>>(true, list);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int managerId, int userId, AthleteUpdateDto dto, CancellationToken cancellationToken)
        {
            var isManagerLinked = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerId && gu.Role == UsersRole.manager, cancellationToken);
            if (!isManagerLinked)
                return new ResponseModel(false, "Unauthorized manager for this gym");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return new ResponseModel(false, "User not found");

            // Check existing by phone if changed
            if (!string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            {
                var dup = await _userManager.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId, cancellationToken);
                if (dup)
                    return new ResponseModel(false, "PhoneNumber already in use");
            }

            user.Name = dto.Name;
            user.Family = dto.Family;
            user.Gender = dto.Gender;
            user.PhoneNumber = dto.PhoneNumber;
            user.UserName = dto.PhoneNumber;

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
                return new ResponseModel(false, res.Errors.FirstOrDefault()?.Description);

            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken)
        {
            var isManagerLinked = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerId && gu.Role == UsersRole.manager, cancellationToken);
            if (!isManagerLinked)
                return new ResponseModel(false, "Unauthorized manager for this gym");

            var link = await _gymUserRepo.Table.FirstOrDefaultAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (link == null)
                return new ResponseModel(false, "User is not a member of this gym");

            await _gymUserRepo.DeleteAsync(link, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<AthleteDetailDto>> GetByIdAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken)
        {
            var isManagerLinked = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == managerId && gu.Role == UsersRole.manager, cancellationToken);
            if (!isManagerLinked)
                return new ResponseModel<AthleteDetailDto>(false, null, "Unauthorized manager for this gym");

            var inGym = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.athlete, cancellationToken);
            if (!inGym)
                return new ResponseModel<AthleteDetailDto>(false, null, "Athlete not found in this gym");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return new ResponseModel<AthleteDetailDto>(false, null, "User not found");

            var detail = new AthleteDetailDto
            {
                Id = user.Id,
                Name = user.Name,
                Family = user.Family,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                AthleteData = await _athleteDataRepo.TableNoTracking
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.SubmitDate)
                    .Select(a => new AthleteDataDto
                    {
                        Id = a.Id,
                        SubmitDate = a.SubmitDate,
                        Height = a.Height,
                        Age = a.Age,
                        Weight = a.Weight
                    }).ToListAsync(cancellationToken),
                UserPrograms = await _userProgramRepo.TableNoTracking
                    .Where(up => up.UserId == userId)
                    .Include(up => up.Program)
                    .OrderByDescending(up => up.StartDate)
                    .Select(up => new UserProgramBriefDto
                    {
                        Id = up.Id,
                        ProgramId = up.ProgramId,
                        StartDate = up.StartDate,
                        EndDate = up.EndDate,
                        ProgramTitle = up.Program.Title
                    }).ToListAsync(cancellationToken)
            };

            return new ResponseModel<AthleteDetailDto>(true, detail);
        }
    }
}
