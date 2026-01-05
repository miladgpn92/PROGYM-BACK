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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Services.Services;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.UserGym
{
    public class UserGymService : IScopedDependency, IUserGymService
    {
        private readonly IRepository<GymUser> mainRepository;
        private readonly IRepository<Entities.Gym> gymRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IMapper mapper;
        private readonly ISMSService smsService;
        private readonly ProjectSettings projectSettings;
        private readonly IHostEnvironment environment;

        public UserGymService(
            IRepository<GymUser> MainRepository,
            IRepository<Entities.Gym> GymRepository,
            UserManager<ApplicationUser> UserManager,
            IMapper Mapper,
            ISMSService smsService,
            IOptionsSnapshot<ProjectSettings> settings,
            IHostEnvironment environment)
        {
            mainRepository = MainRepository;
            gymRepository = GymRepository;
            userManager = UserManager;
            mapper = Mapper;
            this.smsService = smsService;
            projectSettings = settings.Value;
            this.environment = environment;
        }

        public async Task<ResponseModel<List<UserGymDto>>> GetUserInfo(int UserId, CancellationToken cancellationToken)
        {
            var res = await mainRepository.TableNoTracking
                                          .Include(a => a.Gym)
                                          .Where(a => a.UserId == UserId)
                                          .OrderByDescending(a => a.JoinDate)
                                          .ToListAsync(cancellationToken);
            if (res.Count > 0)
            {
                var data = mapper.Map<List<UserGymDto>>(res);
                return new ResponseModel<List<UserGymDto>>(true, data);
            }

            return new ResponseModel<List<UserGymDto>>(false, null, "???????? ???? ???");
        }

        public async Task<ResponseModel<int>> GetGymUserCount(int gymId, int managerId, CancellationToken cancellationToken)
        {
            // Verify the manager is linked to this gym (authorization gate)
            var isManagerLinked = await mainRepository.TableNoTracking
                .AnyAsync(g => g.GymId == gymId && g.UserId == managerId, cancellationToken);

            if (!isManagerLinked)
                return new ResponseModel<int>(false, 0, "?????? ???? ????");

            // Count all users in the gym
            var count = await mainRepository.TableNoTracking
                .CountAsync(g => g.GymId == gymId, cancellationToken);

            return new ResponseModel<int>(true, count);
        }

        public async Task<ResponseModel<List<GymUserListItemDto>>> GetGymUsersAsync(int gymId, CancellationToken cancellationToken)
        {
            var gymExists = await gymRepository.TableNoTracking.AnyAsync(g => g.Id == gymId, cancellationToken);
            if (!gymExists)
            {
                return new ResponseModel<List<GymUserListItemDto>>(false, null, "Gym not found.");
            }

            var users = await mainRepository.TableNoTracking
                .Where(g => g.GymId == gymId)
                .OrderByDescending(g => g.JoinDate)
                .ProjectTo<GymUserListItemDto>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new ResponseModel<List<GymUserListItemDto>>(true, users);
        }

        public async Task<ResponseModel> AddUserToGymAsync(int gymId, GymUserCreateDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return new ResponseModel(false, "User payload is missing.");
            }

            var gym = await gymRepository.TableNoTracking.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
            {
                return new ResponseModel(false, "Gym not found.");
            }

            var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber, cancellationToken);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    Name = dto.Name,
                    Family = dto.Family,
                    PhoneNumber = dto.PhoneNumber,
                    PhoneNumberConfirmed = true,
                    UserName = dto.PhoneNumber,
                    IsActive = true,
                    IsRegisterComplete = true,
                    UserRole = dto.Role
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new ResponseModel(false, createResult.Errors.FirstOrDefault()?.Description);
                }

                var roleResult = await userManager.AddToRoleAsync(user, dto.Role.ToString());
                if (!roleResult.Succeeded)
                {
                    return new ResponseModel(false, roleResult.Errors.FirstOrDefault()?.Description);
                }

                var password = dto.PhoneNumber.Length >= 6
                    ? dto.PhoneNumber.Substring(dto.PhoneNumber.Length - 6)
                    : dto.PhoneNumber;

                var passwordResult = await userManager.AddPasswordAsync(user, password);
                if (!passwordResult.Succeeded)
                {
                    return new ResponseModel(false, passwordResult.Errors.FirstOrDefault()?.Description);
                }
            }
            else
            {
                user.Name = dto.Name;
                user.Family = dto.Family;
                user.PhoneNumber = dto.PhoneNumber;
                user.PhoneNumberConfirmed = true;
                user.UserName = dto.PhoneNumber;
                user.UserRole = dto.Role;
                user.IsActive = true;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return new ResponseModel(false, updateResult.Errors.FirstOrDefault()?.Description);
                }

                var currentRoles = await userManager.GetRolesAsync(user);
                if (!currentRoles.Any(r => string.Equals(r, dto.Role.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    var addRoleResult = await userManager.AddToRoleAsync(user, dto.Role.ToString());
                    if (!addRoleResult.Succeeded)
                    {
                        return new ResponseModel(false, addRoleResult.Errors.FirstOrDefault()?.Description);
                    }
                }
            }

            var existingRelation = await mainRepository.Table
                .FirstOrDefaultAsync(g => g.GymId == gymId && g.UserId == user.Id, cancellationToken);

            if (existingRelation != null)
            {
                if (existingRelation.Role != dto.Role)
                {
                    existingRelation.Role = dto.Role;
                    await mainRepository.UpdateAsync(existingRelation, cancellationToken);
                }

                return new ResponseModel(true);
            }

            var gymUser = new GymUser
            {
                GymId = gymId,
                UserId = user.Id,
                Role = dto.Role,
                AthleteLevel = AthleteLevel.Standard,
                JoinDate = DateTime.UtcNow
            };

            await mainRepository.AddAsync(gymUser, cancellationToken);

            if (IsMemberRole(dto.Role))
            {
                await TrySendWelcomeSmsAsync(user.PhoneNumber ?? string.Empty, gym);
            }

            return new ResponseModel(true);
        }

        private static bool IsMemberRole(UsersRole role) =>
            role == UsersRole.manager || role == UsersRole.coach || role == UsersRole.staff || role == UsersRole.athlete;

        private async Task TrySendWelcomeSmsAsync(string phoneNumber, Entities.Gym gym)
        {
            if (!environment.IsProduction())
                return;

            if (string.IsNullOrWhiteSpace(phoneNumber) || gym == null)
                return;

            var projectSetting = projectSettings?.ProjectSetting;
            if (projectSetting == null ||
                string.IsNullOrWhiteSpace(projectSetting.SMSToken) ||
                string.IsNullOrWhiteSpace(projectSetting.BaseUrl))
                return;

            var gymName = string.IsNullOrWhiteSpace(gym.Title) ? "باشگاه" : gym.Title;
            var message = $"به {gymName} خوش آمدید ، جهت استفاده از لینک زیر استفاده کنید : https://app.pro-gym.ir";

            try
            {
                await smsService.SendSMSAsync(projectSetting.SMSToken,
                    projectSetting.BaseUrl,
                    phoneNumber,
                    message);
            }
            catch
            {
                // ignore sms failures
            }
        }
    }
}
