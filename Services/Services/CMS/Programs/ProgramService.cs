using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using DariaCMS.Common;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Services.Services;
using SharedModels.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.Programs
{
    public class ProgramService : IScopedDependency, IProgramService
    {
        private readonly IRepository<Entities.Program> _programRepo;
        private readonly IRepository<ProgramRoutineItem> _programRoutineItemRepo;
        private readonly IRepository<ProgramPractice> _programPracticeRepo;
        private readonly IRepository<UserProgram> _userProgramRepo;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<Entities.Practice> _practiceRepo;
        private readonly IRepository<ProgramPaperFile> _programPaperFileRepo;
        private readonly IRepository<GymFile> _gymFileRepo;
        private readonly IMapper _mapper;
        private readonly ISMSService _smsService;
        private readonly ProjectSettings _projectSettings;
        private readonly IHostEnvironment _environment;

        public ProgramService(
            IRepository<Entities.Program> programRepo,
            IRepository<ProgramRoutineItem> programRoutineItemRepo,
            IRepository<ProgramPractice> programPracticeRepo,
            IRepository<ProgramPaperFile> programPaperFileRepo,
            IRepository<GymFile> gymFileRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<Entities.Practice> practiceRepo,
            IRepository<UserProgram> userProgramRepo,
            IMapper mapper,
            ISMSService smsService,
            IOptionsSnapshot<ProjectSettings> settings,
            IHostEnvironment environment)
        {
            _programRepo = programRepo;
            _programRoutineItemRepo = programRoutineItemRepo;
            _programPracticeRepo = programPracticeRepo;
            _programPaperFileRepo = programPaperFileRepo;
            _gymFileRepo = gymFileRepo;
            _gymUserRepo = gymUserRepo;
            _practiceRepo = practiceRepo;
            _userProgramRepo = userProgramRepo;
            _mapper = mapper;
            _smsService = smsService;
            _projectSettings = settings.Value;
            _environment = environment;
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

            var routinePlans = new List<RoutineItemPlan>();
            List<int> paperFileIds = new List<int>();
            if (dto.Type == ProgramTypes.Paper)
            {
                var (isPaperValid, paperMessage, files) = await ValidatePaperProgramAsync(gymId, dto, cancellationToken);
                if (!isPaperValid)
                    return new ResponseModel<ProgramSelectDto>(false, null, paperMessage);
                paperFileIds = files;
            }
            else
            {
                var routineInputs = dto.RoutineItems ?? new List<ProgramRoutineItemInputDto>();
                var (isValid, validationMessage, plans) = await PrepareRoutinePlansAsync(gymId, routineInputs, cancellationToken);
                if (!isValid)
                    return new ResponseModel<ProgramSelectDto>(false, null, validationMessage);
                routinePlans = plans;
            }

            var entity = new Entities.Program
            {
                Title = dto.Title,
                Note = dto.Note?.Trim(),
                Type = dto.Type,
                OwnerId = ownerId,
                SubmitterUserId = userId,
                GymId = gymId,
                CreateDate = DateTime.Now,
                CountOfPractice = dto.Type == ProgramTypes.Paper ? 0 : routinePlans.Sum(p => p.Practices.Count)
            };

            await _programRepo.AddAsync(entity, cancellationToken);

            if (dto.Type == ProgramTypes.Paper && paperFileIds.Count > 0)
            {
                await SyncPaperFilesAsync(entity, paperFileIds, cancellationToken);
            }
            else if (routinePlans.Count > 0)
            {
                var routineEntities = routinePlans.Select(plan => new ProgramRoutineItem
                {
                    ProgramId = entity.Id,
                    ItemType = plan.ItemType,
                    DisplayOrder = plan.DisplayOrder,
                    Title = plan.Title,
                    RepeatCount = plan.RepeatCount,
                    RestBetweenRepeats = plan.RestBetweenRepeats,
                    Notes = plan.Notes
                }).ToList();

                await _programRoutineItemRepo.AddRangeAsync(routineEntities, cancellationToken);

                var practiceEntities = new List<ProgramPractice>();
                for (int i = 0; i < routinePlans.Count; i++)
                {
                    var plan = routinePlans[i];
                    var routineEntity = routineEntities[i];
                    foreach (var practice in plan.Practices)
                    {
                        practiceEntities.Add(new ProgramPractice
                        {
                            ProgramId = entity.Id,
                            ProgramRoutineItemId = routineEntity.Id,
                            PracticeId = practice.PracticeId,
                            Type = practice.Type,
                            SetCount = practice.Type == PracticeType.Set ? practice.SetCount : null,
                            MovementCount = practice.Type == PracticeType.Set ? practice.MovementCount : null,
                            Duration = practice.Type == PracticeType.Time ? practice.Duration : null,
                            Rest = practice.Rest,
                            InternalOrder = practice.InternalOrder,
                            Notes = practice.Notes
                        });
                    }
                }

                if (practiceEntities.Count > 0)
                    await _programPracticeRepo.AddRangeAsync(practiceEntities, cancellationToken);
            }

            await SyncOwnerAssignmentAsync(
                entity.Id,
                null,
                entity.OwnerId,
                dto.OwnerStartDate,
                dto.OwnerEndDate,
                cancellationToken);

            var model = await _programRepo.TableNoTracking
                .Where(x => x.Id == entity.Id && x.GymId == gymId)
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .ProjectTo<ProgramSelectDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            return new ResponseModel<ProgramSelectDto>(true, model);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, ProgramDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _programRepo.Table
                .Include(x => x.ProgramRoutineItems)
                    .ThenInclude(ri => ri.ProgramPractices)
                .Include(x => x.PaperFiles)
                .FirstOrDefaultAsync(x => x.Id == id && x.GymId == gymId, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            var previousOwnerId = entity.OwnerId;

            int? ownerId = dto.OwnerId;
            if (ownerId.HasValue)
            {
                if (ownerId.Value != entity.OwnerId)
                {
                    var ownerInGym = await _gymUserRepo.TableNoTracking.AnyAsync(x => x.GymId == gymId && x.UserId == ownerId.Value, cancellationToken);
                    if (!ownerInGym)
                        return new ResponseModel(false, "Owner not found in current gym");
                    entity.OwnerId = ownerId.Value;
                }
            }
            else
            {
                entity.OwnerId = null;
            }

            entity.Title = dto.Title;
            entity.Type = dto.Type;
            entity.Note = dto.Note?.Trim();

            if (dto.Type == ProgramTypes.Paper)
            {
                var (isPaperValid, paperMessage, fileIds) = await ValidatePaperProgramAsync(gymId, dto, cancellationToken);
                if (!isPaperValid)
                    return new ResponseModel(false, paperMessage);

                entity.CountOfPractice = 0;

                await RemoveRoutineStructureAsync(entity, cancellationToken);
                await SyncPaperFilesAsync(entity, fileIds, cancellationToken);
            }
            else
            {
                if (entity.PaperFiles != null && entity.PaperFiles.Count > 0)
                {
                    await _programPaperFileRepo.DeleteRangeAsync(entity.PaperFiles, cancellationToken);
                    entity.PaperFiles = new List<ProgramPaperFile>();
                }

                if (dto.RoutineItems != null)
                {
                    var (isValid, validationMessage, plans) = await PrepareRoutinePlansAsync(gymId, dto.RoutineItems, cancellationToken);
                    if (!isValid)
                        return new ResponseModel(false, validationMessage);

                    entity.CountOfPractice = plans.Sum(p => p.Practices.Count);

                    await RemoveRoutineStructureAsync(entity, cancellationToken);

                    if (plans.Count > 0)
                    {
                        var routineEntities = plans.Select(plan => new ProgramRoutineItem
                        {
                            ProgramId = entity.Id,
                            ItemType = plan.ItemType,
                            DisplayOrder = plan.DisplayOrder,
                            Title = plan.Title,
                            RepeatCount = plan.RepeatCount,
                            RestBetweenRepeats = plan.RestBetweenRepeats,
                            Notes = plan.Notes
                        }).ToList();

                        await _programRoutineItemRepo.AddRangeAsync(routineEntities, cancellationToken);

                        var practiceEntities = new List<ProgramPractice>();
                        for (int i = 0; i < plans.Count; i++)
                        {
                            var plan = plans[i];
                            var routineEntity = routineEntities[i];
                            foreach (var practice in plan.Practices)
                            {
                                practiceEntities.Add(new ProgramPractice
                                {
                                    ProgramId = entity.Id,
                                    ProgramRoutineItemId = routineEntity.Id,
                                    PracticeId = practice.PracticeId,
                                    Type = practice.Type,
                                    SetCount = practice.Type == PracticeType.Set ? practice.SetCount : null,
                                    MovementCount = practice.Type == PracticeType.Set ? practice.MovementCount : null,
                                    Duration = practice.Type == PracticeType.Time ? practice.Duration : null,
                                    Rest = practice.Rest,
                                    InternalOrder = practice.InternalOrder,
                                    Notes = practice.Notes
                                });
                            }
                        }

                        if (practiceEntities.Count > 0)
                            await _programPracticeRepo.AddRangeAsync(practiceEntities, cancellationToken);
                    }
                }
                else
                {
                    entity.CountOfPractice = await _programPracticeRepo.TableNoTracking.CountAsync(x => x.ProgramId == entity.Id, cancellationToken);
                }
            }

            await _programRepo.UpdateAsync(entity, cancellationToken);
            await SyncOwnerAssignmentAsync(
                entity.Id,
                previousOwnerId,
                entity.OwnerId,
                dto.OwnerStartDate,
                dto.OwnerEndDate,
                cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _programRepo.Table.FirstOrDefaultAsync(x => x.Id == id && x.GymId == gymId, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            await _programRepo.DeleteAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteRoutineItemAsync(int gymId, int userId, int routineItemId, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var routineItem = await _programRoutineItemRepo.Table
                .Include(ri => ri.Program)
                .Include(ri => ri.ProgramPractices)
                .FirstOrDefaultAsync(ri => ri.Id == routineItemId, cancellationToken);
            if (routineItem == null)
                return new ResponseModel(false, "Not found");
            if (routineItem.Program == null || routineItem.Program.GymId != gymId)
                return new ResponseModel(false, "Not found");

            var practices = routineItem.ProgramPractices.ToList();
            if (practices.Count > 0)
                await _programPracticeRepo.DeleteRangeAsync(practices, cancellationToken);

            await _programRoutineItemRepo.DeleteAsync(routineItem, cancellationToken);

            var program = routineItem.Program;
            if (program != null)
            {
                program.CountOfPractice = await _programPracticeRepo.TableNoTracking.CountAsync(x => x.ProgramId == program.Id, cancellationToken);
                await _programRepo.UpdateAsync(program, cancellationToken);
            }

            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> ReorderRoutineItemsAsync(int gymId, int userId, int programId, ProgramRoutineItemReorderDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            if (dto.ProgramId != programId)
                return new ResponseModel(false, "Program mismatch");

            var program = await _programRepo.Table
                .Include(p => p.ProgramRoutineItems)
                .FirstOrDefaultAsync(p => p.Id == programId && p.GymId == gymId, cancellationToken);
            if (program == null)
                return new ResponseModel(false, "Program not found");

            if (dto.Items == null || dto.Items.Count == 0)
                return new ResponseModel(false, "No routine items provided");

            if (dto.Items.Count != program.ProgramRoutineItems.Count)
                return new ResponseModel(false, "Routine item count mismatch");

            var lookup = program.ProgramRoutineItems.ToDictionary(x => x.Id);
            foreach (var item in dto.Items)
            {
                if (!lookup.TryGetValue(item.RoutineItemId, out var routineItem))
                    return new ResponseModel(false, $"Routine item {item.RoutineItemId} not found in program");

                routineItem.DisplayOrder = item.DisplayOrder;
            }

            var ordered = program.ProgramRoutineItems
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].DisplayOrder = i + 1;

            await _programRoutineItemRepo.UpdateRangeAsync(ordered, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> ReorderSupersetPracticesAsync(int gymId, int userId, ProgramSupersetPracticeReorderDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var routineItem = await _programRoutineItemRepo.Table
                .Include(ri => ri.Program)
                .Include(ri => ri.ProgramPractices)
                .FirstOrDefaultAsync(ri => ri.Id == dto.RoutineItemId, cancellationToken);
            if (routineItem == null)
                return new ResponseModel(false, "Routine item not found");
            if (routineItem.Program == null || routineItem.Program.GymId != gymId)
                return new ResponseModel(false, "Routine item not found");

            if (routineItem.ItemType != ProgramRoutineItemType.Superset)
                return new ResponseModel(false, "Reordering movements is only supported for supersets");

            if (dto.Practices == null || dto.Practices.Count == 0)
                return new ResponseModel(false, "No practices provided");

            if (dto.Practices.Count != routineItem.ProgramPractices.Count)
                return new ResponseModel(false, "Practice count mismatch");

            var lookup = routineItem.ProgramPractices.ToDictionary(x => x.Id);
            foreach (var practiceOrder in dto.Practices)
            {
                if (!lookup.TryGetValue(practiceOrder.ProgramPracticeId, out var practice))
                    return new ResponseModel(false, $"Practice {practiceOrder.ProgramPracticeId} not found in routine item");

                practice.InternalOrder = practiceOrder.InternalOrder;
            }

            var ordered = routineItem.ProgramPractices
                .OrderBy(p => p.InternalOrder)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].InternalOrder = i + 1;

            await _programPracticeRepo.UpdateRangeAsync(ordered, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> UpdateRoutineItemMetadataAsync(int gymId, int userId, ProgramRoutineItemMetadataDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var routineItem = await _programRoutineItemRepo.Table
                .Include(ri => ri.Program)
                .FirstOrDefaultAsync(ri => ri.Id == dto.RoutineItemId, cancellationToken);
            if (routineItem == null)
                return new ResponseModel(false, "Routine item not found");
            if (routineItem.Program == null || routineItem.Program.GymId != gymId)
                return new ResponseModel(false, "Routine item not found");

            if (routineItem.ItemType == ProgramRoutineItemType.Superset)
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return new ResponseModel(false, "Superset title is required");
                if (!dto.RepeatCount.HasValue || dto.RepeatCount.Value <= 0)
                    return new ResponseModel(false, "Superset repeat count must be greater than zero");
                if (!dto.RestBetweenRepeats.HasValue || dto.RestBetweenRepeats.Value < 0)
                    return new ResponseModel(false, "Superset rest between repeats must be zero or positive");
            }

            routineItem.Title = dto.Title?.Trim();
            routineItem.RepeatCount = dto.RepeatCount;
            routineItem.RestBetweenRepeats = dto.RestBetweenRepeats;
            routineItem.Notes = dto.Notes?.Trim();

            await _programRoutineItemRepo.UpdateAsync(routineItem, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<PagedResult<ProgramSelectDto>>> GetListAsync(int gymId, int userId, string q, ProgramTypes? type, bool includeAll, Pageres pager, CancellationToken cancellationToken)
        {
            pager ??= new Pageres();
            pager.Normalize();

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PagedResult<ProgramSelectDto>>(false, null, "Access denied");

            var query = _programRepo.TableNoTracking
                .Where(x => x.GymId == gymId);

            if (!includeAll)
            {
                var effectiveType = type ?? ProgramTypes.Global;
                query = query.Where(x => x.Type == effectiveType);
            }

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.Contains(q));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .OrderByDescending(x => x.Id)
                .Paginate(pager)
                .ProjectTo<ProgramSelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ProgramSelectDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pager.PageNumber,
                PageSize = pager.PageSize
            };

            return new ResponseModel<PagedResult<ProgramSelectDto>>(true, result);
        }

        public async Task<ResponseModel<ProgramDetailDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<ProgramDetailDto>(false, null, "Access denied");

            var item = await _programRepo.TableNoTracking
                .Where(x => x.Id == id && x.GymId == gymId)
                .Include(x => x.Owner)
                .Include(x => x.SubmitterUser)
                .Include(x => x.ProgramRoutineItems)
                    .ThenInclude(ri => ri.ProgramPractices)
                        .ThenInclude(pp => pp.Practice)
                            .ThenInclude(p => p.MediaItems)
                                .ThenInclude(mi => mi.GymFile)
                .ProjectTo<ProgramDetailDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null)
                return new ResponseModel<ProgramDetailDto>(false, null, "Not found");

            await PopulatePracticeDescriptionsAsync(item, cancellationToken);
            return new ResponseModel<ProgramDetailDto>(true, item);
        }

        public async Task<ResponseModel> AttachToAthleteAsync(
            int gymId,
            int managerUserId,
            int programId,
            int athleteUserId,
            DateTime startDate,
            DateTime? endDate,
            CancellationToken cancellationToken)
        {
            var managerHasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(g => g.GymId == gymId && g.UserId == managerUserId && g.Role == UsersRole.manager, cancellationToken);
            if (!managerHasAccess)
                return new ResponseModel(false, "Access denied");

            var program = await _programRepo.TableNoTracking.FirstOrDefaultAsync(p => p.Id == programId && p.GymId == gymId, cancellationToken);
            if (program == null)
                return new ResponseModel(false, "Program not found");

            var athleteLink = await _gymUserRepo.TableNoTracking
                .Include(g => g.User)
                .Include(g => g.Gym)
                .FirstOrDefaultAsync(g => g.GymId == gymId && g.UserId == athleteUserId, cancellationToken);
            if (athleteLink == null)
                return new ResponseModel(false, "Athlete is not a member of the current gym");

            var existing = await _userProgramRepo.TableNoTracking
                .FirstOrDefaultAsync(up => up.ProgramId == programId && up.UserId == athleteUserId, cancellationToken);
            if (existing != null)
                return new ResponseModel(false, "Program is already attached to athlete");

            var entity = new UserProgram
            {
                ProgramId = programId,
                UserId = athleteUserId,
                StartDate = startDate,
                EndDate = endDate
            };

            await _userProgramRepo.AddAsync(entity, cancellationToken);
            await TrySendProgramAssignmentSmsAsync(athleteLink);
            return new ResponseModel(true, "");
        }

        private async Task TrySendProgramAssignmentSmsAsync(GymUser athleteLink)
        {
            if (!_environment.IsProduction())
                return;

            if (athleteLink?.User == null || string.IsNullOrWhiteSpace(athleteLink.User.PhoneNumber))
                return;

            var projectSetting = _projectSettings?.ProjectSetting;
            if (projectSetting == null ||
                string.IsNullOrWhiteSpace(projectSetting.SMSToken) ||
                string.IsNullOrWhiteSpace(projectSetting.BaseUrl))
                return;

            var gymName = string.IsNullOrWhiteSpace(athleteLink.Gym?.Title) ? "باشگاه" : athleteLink.Gym.Title;
            var message = $"برنامه جدیدی برای شما در {gymName} اضافه شد . https://app.pro-gym.ir";

            try
            {
                await _smsService.SendSMSAsync(projectSetting.SMSToken,
                    projectSetting.BaseUrl,
                    athleteLink.User.PhoneNumber,
                    message);
            }
            catch
            {
                // ignore sms failures
            }
        }

        public async Task<ResponseModel> DeAttachAthleteAsync(
            int gymId,
            int managerUserId,
            int userProgramId,
            CancellationToken cancellationToken)
        {
            var managerHasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(g => g.GymId == gymId && g.UserId == managerUserId && g.Role == UsersRole.manager, cancellationToken);
            if (!managerHasAccess)
                return new ResponseModel(false, "Access denied");

            var userProgram = await _userProgramRepo.Table
                .Include(up => up.Program)
                .FirstOrDefaultAsync(up => up.Id == userProgramId, cancellationToken);
            if (userProgram == null)
                return new ResponseModel(false, "Not found");
            if (userProgram.Program == null || userProgram.Program.GymId != gymId)
                return new ResponseModel(false, "Not found");

            await _userProgramRepo.DeleteAsync(userProgram, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> UpdateUserProgramDatesAsync(
            int gymId,
            int managerUserId,
            int userProgramId,
            DateTime startDate,
            DateTime? endDate,
            CancellationToken cancellationToken)
        {
            var managerHasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(g => g.GymId == gymId && g.UserId == managerUserId && g.Role == UsersRole.manager, cancellationToken);
            if (!managerHasAccess)
                return new ResponseModel(false, "Access denied");

            var normalizedStart = startDate.Date;
            var normalizedEnd = endDate?.Date;
            if (normalizedEnd.HasValue && normalizedEnd.Value < normalizedStart)
                return new ResponseModel(false, "تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد");

            var userProgram = await _userProgramRepo.Table
                .Include(up => up.Program)
                .FirstOrDefaultAsync(up => up.Id == userProgramId, cancellationToken);
            if (userProgram == null)
                return new ResponseModel(false, "Not found");
            if (userProgram.Program == null || userProgram.Program.GymId != gymId)
                return new ResponseModel(false, "Not found");

            userProgram.StartDate = normalizedStart;
            userProgram.EndDate = normalizedEnd;

            await _userProgramRepo.UpdateAsync(userProgram, cancellationToken);
            return new ResponseModel(true, "");
        }

        private async Task RemoveRoutineStructureAsync(Entities.Program program, CancellationToken cancellationToken)
        {
            if (program.ProgramRoutineItems == null || program.ProgramRoutineItems.Count == 0)
                return;

            var practices = program.ProgramRoutineItems
                .SelectMany(ri => ri.ProgramPractices)
                .ToList();

            if (practices.Count > 0)
            await _programPracticeRepo.DeleteRangeAsync(practices, cancellationToken);

            var routineItems = program.ProgramRoutineItems.ToList();
            if (routineItems.Count > 0)
                await _programRoutineItemRepo.DeleteRangeAsync(routineItems, cancellationToken);

            program.ProgramRoutineItems = new List<ProgramRoutineItem>();
        }

        private async Task PopulatePracticeDescriptionsAsync(ProgramDetailDto dto, CancellationToken cancellationToken)
        {
            if (dto?.RoutineItems == null || dto.RoutineItems.Count == 0)
                return;

            var practices = dto.RoutineItems
                .Where(ri => ri?.Practices != null)
                .SelectMany(ri => ri.Practices)
                .Where(p => p?.PracticeId.HasValue == true)
                .ToList();

            if (practices.Count == 0)
                return;

            var practiceIds = practices
                .Select(p => p.PracticeId!.Value)
                .Distinct()
                .ToList();

            var descLookup = await _practiceRepo.TableNoTracking
                .Where(pr => practiceIds.Contains(pr.Id))
                .Select(pr => new { pr.Id, pr.Desc })
                .ToDictionaryAsync(pr => pr.Id, pr => pr.Desc, cancellationToken);

            foreach (var practice in practices)
            {
                if (practice?.PracticeId == null)
                    continue;

                if (descLookup.TryGetValue(practice.PracticeId.Value, out var desc))
                    practice.PracticeDesc = desc;
            }
        }

        private async Task SyncPaperFilesAsync(Entities.Program program, IReadOnlyList<int> orderedFileIds, CancellationToken cancellationToken)
        {
            var existing = program.PaperFiles?.ToList() ?? new List<ProgramPaperFile>();

            if (orderedFileIds.Count == 0)
            {
                if (existing.Count > 0)
                    await _programPaperFileRepo.DeleteRangeAsync(existing, cancellationToken);
                program.PaperFiles = new List<ProgramPaperFile>();
                return;
            }

            var toRemove = existing.Where(pf => !orderedFileIds.Contains(pf.GymFileId)).ToList();
            if (toRemove.Count > 0)
            {
                await _programPaperFileRepo.DeleteRangeAsync(toRemove, cancellationToken);
                existing = existing.Except(toRemove).ToList();
            }

            var existingLookup = existing.ToDictionary(pf => pf.GymFileId);
            var toUpdate = new List<ProgramPaperFile>();
            var toAdd = new List<ProgramPaperFile>();

            for (int index = 0; index < orderedFileIds.Count; index++)
            {
                var fileId = orderedFileIds[index];
                var displayOrder = index + 1;
                if (existingLookup.TryGetValue(fileId, out var current))
                {
                    if (current.DisplayOrder != displayOrder)
                    {
                        current.DisplayOrder = displayOrder;
                        toUpdate.Add(current);
                    }
                }
                else
                {
                    toAdd.Add(new ProgramPaperFile
                    {
                        ProgramId = program.Id,
                        GymFileId = fileId,
                        DisplayOrder = displayOrder
                    });
                }
            }

            if (toUpdate.Count > 0)
                await _programPaperFileRepo.UpdateRangeAsync(toUpdate, cancellationToken);

            if (toAdd.Count > 0)
            {
                await _programPaperFileRepo.AddRangeAsync(toAdd, cancellationToken);
                existing.AddRange(toAdd);
            }

            existingLookup = existing.ToDictionary(pf => pf.GymFileId);
            program.PaperFiles = orderedFileIds.Select(id => existingLookup[id]).ToList();
        }

        private async Task<(bool isValid, string message, List<int> fileIds)> ValidatePaperProgramAsync(int gymId, ProgramDto dto, CancellationToken cancellationToken)
        {
            var requestedIds = (dto.PaperFileIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (requestedIds.Count == 0)
                return (false, "انتخاب حداقل یک فایل برای برنامه کاغذی الزامی است.", new List<int>());

            var existingIds = await _gymFileRepo.TableNoTracking
                .Where(f => f.GymId == gymId && requestedIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            if (existingIds.Count != requestedIds.Count)
                return (false, "برخی از فایل‌ها یافت نشد یا به این باشگاه تعلق ندارد.", new List<int>());

            return (true, string.Empty, requestedIds);
        }

        private async Task SyncOwnerAssignmentAsync(
            int programId,
            int? previousOwnerId,
            int? currentOwnerId,
            DateTime? requestedStart,
            DateTime? requestedEnd,
            CancellationToken cancellationToken)
        {
            if (previousOwnerId.HasValue && (!currentOwnerId.HasValue || previousOwnerId.Value != currentOwnerId.Value))
            {
                var existingOld = await _userProgramRepo.Table
                    .FirstOrDefaultAsync(up => up.ProgramId == programId && up.UserId == previousOwnerId.Value, cancellationToken);

                if (existingOld != null)
                    await _userProgramRepo.DeleteAsync(existingOld, cancellationToken);
            }

            if (currentOwnerId.HasValue)
            {
                var current = await _userProgramRepo.Table
                    .FirstOrDefaultAsync(up => up.ProgramId == programId && up.UserId == currentOwnerId.Value, cancellationToken);

                if (current == null)
                {
                    var userProgram = new UserProgram
                    {
                        ProgramId = programId,
                        UserId = currentOwnerId.Value,
                        StartDate = requestedStart?.Date ?? DateTime.Now,
                        EndDate = requestedEnd?.Date
                    };

                    await _userProgramRepo.AddAsync(userProgram, cancellationToken);
                }
                else if (requestedStart.HasValue || requestedEnd.HasValue)
                {
                    if (requestedStart.HasValue)
                        current.StartDate = requestedStart.Value.Date;

                    current.EndDate = requestedEnd?.Date;
                    await _userProgramRepo.UpdateAsync(current, cancellationToken);
                }
            }
        }

        private async Task<(bool Success, string Error, List<RoutineItemPlan> Plans)> PrepareRoutinePlansAsync(
            int gymId,
            List<ProgramRoutineItemInputDto> routineInputs,
            CancellationToken cancellationToken)
        {
            var plans = new List<RoutineItemPlan>();

            if (routineInputs == null || routineInputs.Count == 0)
                return (true, string.Empty, plans);

            var practiceIds = new HashSet<int>();
            var normalizedPlans = new List<RoutineItemPlan>();
            int fallbackDisplayOrder = 1;

            foreach (var item in routineInputs)
            {
                if (item == null)
                    return (false, "Routine item payload is required", null);

                var plan = new RoutineItemPlan
                {
                    ItemType = item.ItemType,
                    Title = item.Title?.Trim(),
                    RepeatCount = item.RepeatCount,
                    RestBetweenRepeats = item.RestBetweenRepeats,
                    Notes = item.Notes?.Trim(),
                    DisplayOrder = item.DisplayOrder ?? fallbackDisplayOrder++
                };

                if (item.ItemType == ProgramRoutineItemType.Single)
                {
                    if (item.Practices == null || item.Practices.Count != 1)
                        return (false, "Single routine items must include exactly one movement", null);
                }
                else if (item.ItemType == ProgramRoutineItemType.Superset)
                {
                    if (item.Practices == null || item.Practices.Count < 2)
                        return (false, "Superset routine items must include at least two movements", null);
                    if (string.IsNullOrWhiteSpace(plan.Title))
                        return (false, "Superset title is required", null);
                    if (!plan.RepeatCount.HasValue || plan.RepeatCount.Value <= 0)
                        return (false, "Superset repeat count must be greater than zero", null);
                    if (!plan.RestBetweenRepeats.HasValue || plan.RestBetweenRepeats.Value < 0)
                        return (false, "Superset rest between repeats must be zero or positive", null);
                }
                else
                {
                    return (false, "Invalid routine item type", null);
                }

                var practicePlans = new List<PracticePlan>();
                int fallbackInternalOrder = 1;
                foreach (var practiceDto in item.Practices ?? new List<ProgramPracticeInputDto>())
                {
                    if (!TryValidatePractice(practiceDto, out var validationError))
                        return (false, validationError, null);

                    var internalOrder = practiceDto.InternalOrder ?? fallbackInternalOrder++;
                    var practicePlan = new PracticePlan
                    {
                        PracticeId = practiceDto.PracticeId.Value,
                        Type = practiceDto.Type,
                        SetCount = practiceDto.SetCount,
                        MovementCount = practiceDto.MovementCount,
                        Duration = practiceDto.Duration,
                        Rest = practiceDto.Rest,
                        InternalOrder = internalOrder,
                        Notes = practiceDto.Notes?.Trim()
                    };

                    practicePlans.Add(practicePlan);
                    practiceIds.Add(practicePlan.PracticeId);
                }

                if (practicePlans.Count == 0)
                    return (false, "Routine items require at least one movement", null);

                var orderedPractices = practicePlans
                    .OrderBy(p => p.InternalOrder)
                    .Select((p, index) =>
                    {
                        p.InternalOrder = index + 1;
                        return p;
                    })
                    .ToList();

                plan.Practices = orderedPractices;
                normalizedPlans.Add(plan);
            }

            var orderedPlans = normalizedPlans
                .OrderBy(p => p.DisplayOrder)
                .Select((p, index) =>
                {
                    p.DisplayOrder = index + 1;
                    return p;
                })
                .ToList();

            var existsCount = await _practiceRepo.TableNoTracking
                .CountAsync(p => practiceIds.Contains(p.Id) && p.GymId == gymId, cancellationToken);

            if (existsCount != practiceIds.Count)
                return (false, "One or more practices are invalid", null);

            return (true, string.Empty, orderedPlans);
        }

        private static bool TryValidatePractice(ProgramPracticeInputDto practiceDto, out string error)
        {
            error = string.Empty;
            if (practiceDto == null)
            {
                error = "Practice payload is required";
                return false;
            }

            if (!practiceDto.PracticeId.HasValue)
            {
                error = "PracticeId is required";
                return false;
            }

            if (practiceDto.Rest.HasValue && practiceDto.Rest.Value < 0)
            {
                error = "Rest must be zero or positive";
                return false;
            }

            switch (practiceDto.Type)
            {
                case PracticeType.Set:
                    if (!practiceDto.SetCount.HasValue || practiceDto.SetCount.Value <= 0)
                    {
                        error = "Set practices require a positive set count";
                        return false;
                    }
                    if (!practiceDto.MovementCount.HasValue || practiceDto.MovementCount.Value <= 0)
                    {
                        error = "Set practices require a positive movement count";
                        return false;
                    }
                    if (!practiceDto.Rest.HasValue)
                    {
                        error = "Set practices require rest value";
                        return false;
                    }
                    break;

                case PracticeType.Time:
                    if (!practiceDto.Duration.HasValue || practiceDto.Duration.Value <= 0)
                    {
                        error = "Time practices require a positive duration";
                        return false;
                    }
                    if (!practiceDto.Rest.HasValue)
                    {
                        error = "Time practices require rest value";
                        return false;
                    }
                    break;

                default:
                    error = "Unsupported practice type";
                    return false;
            }

            return true;
        }

        private class RoutineItemPlan
        {
            public ProgramRoutineItemType ItemType { get; set; }
            public int DisplayOrder { get; set; }
            public string Title { get; set; }
            public int? RepeatCount { get; set; }
            public int? RestBetweenRepeats { get; set; }
            public string Notes { get; set; }
            public List<PracticePlan> Practices { get; set; } = new List<PracticePlan>();
        }

        private class PracticePlan
        {
            public int PracticeId { get; set; }
            public PracticeType Type { get; set; }
            public int? SetCount { get; set; }
            public int? MovementCount { get; set; }
            public int? Duration { get; set; }
            public int? Rest { get; set; }
            public int InternalOrder { get; set; }
            public string Notes { get; set; }
        }
    }
}
