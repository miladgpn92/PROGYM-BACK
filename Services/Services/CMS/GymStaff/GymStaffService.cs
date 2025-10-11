using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Enums;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;

namespace Services.Services.CMS.GymStaff
{
    public class GymStaffService : IGymStaffService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<GymUser> _gymUserRepo;

        public GymStaffService(UserManager<ApplicationUser> userManager, IRepository<GymUser> gymUserRepo)
        {
            _userManager = userManager;
            _gymUserRepo = gymUserRepo;
        }

        public async Task<ResponseModel<int>> CreateAsync(int gymId, int managerId, GymStaffCreateDto dto, CancellationToken cancellationToken)
        {
            if (!IsStaffRole(dto.Role))
                return new ResponseModel<int>(false, 0, "Role must be manager or coach");

            var hasAccess = await ManagerHasAccessAsync(gymId, managerId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<int>(false, 0, "Unauthorized manager for this gym");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber, cancellationToken);
            var isNewUser = user == null;

            if (isNewUser)
            {
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
                    UserRole = dto.Role
                };

                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                    return new ResponseModel<int>(false, 0, createRes.Errors.FirstOrDefault()?.Description ?? "Create user failed");

                await EnsureRoleAsync(user, dto.Role);

                var defaultPwd = dto.PhoneNumber?.Length >= 6 ? dto.PhoneNumber[^6..] : dto.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(defaultPwd))
                {
                    try { await _userManager.AddPasswordAsync(user, defaultPwd); }
                    catch { /* ignore password policy violations */ }
                }
            }
            else
            {
                user.Name = dto.Name;
                user.Family = dto.Family;
                user.Gender = dto.Gender;

                if (!string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.Ordinal))
                {
                    var exists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != user.Id, cancellationToken);
                    if (exists)
                        return new ResponseModel<int>(false, 0, "Phone number already in use");

                    user.PhoneNumber = dto.PhoneNumber;
                    user.UserName = dto.PhoneNumber;
                }

                if (user.UserRole != dto.Role)
                {
                    if (IsStaffRole(user.UserRole))
                        await _userManager.RemoveFromRoleAsync(user, user.UserRole.ToString());
                    user.UserRole = dto.Role;
                }

                await EnsureRoleAsync(user, dto.Role);

                var updateRes = await _userManager.UpdateAsync(user);
                if (!updateRes.Succeeded)
                    return new ResponseModel<int>(false, 0, updateRes.Errors.FirstOrDefault()?.Description ?? "Update user failed");
            }

            var link = await _gymUserRepo.Table.FirstOrDefaultAsync(gu => gu.GymId == gymId && gu.UserId == user.Id, cancellationToken);
            if (link == null)
            {
                link = new GymUser
                {
                    GymId = gymId,
                    UserId = user.Id,
                    Role = dto.Role
                };
                await _gymUserRepo.AddAsync(link, cancellationToken);
            }
            else
            {
                link.Role = dto.Role;
                await _gymUserRepo.UpdateAsync(link, cancellationToken);
            }

            return new ResponseModel<int>(true, user.Id);
        }

        public async Task<ResponseModel<List<GymStaffSelectDto>>> GetListAsync(int gymId, int managerId, string search, CancellationToken cancellationToken)
        {
            var hasAccess = await ManagerHasAccessAsync(gymId, managerId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<List<GymStaffSelectDto>>(false, null, "Unauthorized manager for this gym");

            var query = from gu in _gymUserRepo.TableNoTracking
                        join u in _userManager.Users on gu.UserId equals u.Id
                        where gu.GymId == gymId && (gu.Role == UsersRole.manager || gu.Role == UsersRole.coach)
                        select new GymStaffSelectDto
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Family = u.Family,
                            PhoneNumber = u.PhoneNumber,
                            Gender = u.Gender,
                            Role = gu.Role
                        };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Name ?? "").Contains(term) ||
                    (x.Family ?? "").Contains(term) ||
                    (x.PhoneNumber ?? "").Contains(term));
            }

            var list = await query
                .OrderBy(x => x.Role)
                .ThenBy(x => x.Family)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return new ResponseModel<List<GymStaffSelectDto>>(true, list);
        }

        public async Task<ResponseModel<GymStaffSelectDto>> GetByIdAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken)
        {
            var hasAccess = await ManagerHasAccessAsync(gymId, managerId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<GymStaffSelectDto>(false, null, "Unauthorized manager for this gym");

            var member = await _gymUserRepo.TableNoTracking
                .Where(gu => gu.GymId == gymId && gu.UserId == userId && (gu.Role == UsersRole.manager || gu.Role == UsersRole.coach))
                .Select(gu => new { gu.Role })
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                return new ResponseModel<GymStaffSelectDto>(false, null, "Staff member not found in this gym");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return new ResponseModel<GymStaffSelectDto>(false, null, "User not found");

            var dto = new GymStaffSelectDto
            {
                Id = user.Id,
                Name = user.Name,
                Family = user.Family,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Role = member.Role
            };

            return new ResponseModel<GymStaffSelectDto>(true, dto);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int managerId, int userId, GymStaffUpdateDto dto, CancellationToken cancellationToken)
        {
            if (!IsStaffRole(dto.Role))
                return new ResponseModel(false, "Role must be manager or coach");

            var hasAccess = await ManagerHasAccessAsync(gymId, managerId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Unauthorized manager for this gym");

            var link = await _gymUserRepo.Table.FirstOrDefaultAsync(gu => gu.GymId == gymId && gu.UserId == userId && (gu.Role == UsersRole.manager || gu.Role == UsersRole.coach), cancellationToken);
            if (link == null)
                return new ResponseModel(false, "Staff member not found in this gym");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return new ResponseModel(false, "User not found");

            if (!string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.Ordinal))
            {
                var exists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != user.Id, cancellationToken);
                if (exists)
                    return new ResponseModel(false, "Phone number already in use");
                user.PhoneNumber = dto.PhoneNumber;
                user.UserName = dto.PhoneNumber;
            }

            user.Name = dto.Name;
            user.Family = dto.Family;
            user.Gender = dto.Gender;

            if (user.UserRole != dto.Role)
            {
                if (IsStaffRole(user.UserRole))
                    await _userManager.RemoveFromRoleAsync(user, user.UserRole.ToString());
                user.UserRole = dto.Role;
            }

            await EnsureRoleAsync(user, dto.Role);

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
                return new ResponseModel(false, updateRes.Errors.FirstOrDefault()?.Description ?? "Update user failed");

            if (link.Role != dto.Role)
            {
                link.Role = dto.Role;
                await _gymUserRepo.UpdateAsync(link, cancellationToken);
            }

            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int managerId, int userId, CancellationToken cancellationToken)
        {
            if (managerId == userId)
                return new ResponseModel(false, "You cannot remove yourself from the gym");

            var hasAccess = await ManagerHasAccessAsync(gymId, managerId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Unauthorized manager for this gym");

            var link = await _gymUserRepo.Table.FirstOrDefaultAsync(gu =>
                gu.GymId == gymId &&
                gu.UserId == userId &&
                (gu.Role == UsersRole.manager || gu.Role == UsersRole.coach),
                cancellationToken);

            if (link == null)
                return new ResponseModel(false, "Staff member not found in this gym");

            await _gymUserRepo.DeleteAsync(link, cancellationToken);
            return new ResponseModel(true, "");
        }

        private Task<bool> ManagerHasAccessAsync(int gymId, int managerId, CancellationToken cancellationToken)
        {
            return _gymUserRepo.TableNoTracking.AnyAsync(gu =>
                gu.GymId == gymId &&
                gu.UserId == managerId &&
                gu.Role == UsersRole.manager, cancellationToken);
        }

        private static bool IsStaffRole(UsersRole role) =>
            role == UsersRole.manager || role == UsersRole.coach;

        private async Task EnsureRoleAsync(ApplicationUser user, UsersRole role)
        {
            var roleName = role.ToString();
            if (!await _userManager.IsInRoleAsync(user, roleName))
                await _userManager.AddToRoleAsync(user, roleName);
        }
    }
}

