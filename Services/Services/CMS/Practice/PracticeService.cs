using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using DariaCMS.Common;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.Practices
{
    public class PracticeService : IScopedDependency, IPracticeService
    {
        private readonly IRepository<Entities.Practice> _practiceRepo;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<Entities.PracticeCategory> _categoryRepo;
        private readonly IRepository<GymFile> _gymFileRepo;
        private readonly IRepository<PracticeMedia> _practiceMediaRepo;
        private readonly IMapper _mapper;

        public PracticeService(
            IRepository<Entities.Practice> practiceRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<Entities.PracticeCategory> categoryRepo,
            IRepository<GymFile> gymFileRepo,
            IRepository<PracticeMedia> practiceMediaRepo,
            IMapper mapper)
        {
            _practiceRepo = practiceRepo;
            _gymUserRepo = gymUserRepo;
            _categoryRepo = categoryRepo;
            _gymFileRepo = gymFileRepo;
            _practiceMediaRepo = practiceMediaRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel<PracticeSelectDto>> CreateAsync(int gymId, int userId, PracticeDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PracticeSelectDto>(false, null, "Access denied");

            // Validate category exists to avoid FK violation
            if (!dto.PracticeCategoryId.HasValue ||
                !await _categoryRepo.TableNoTracking.AnyAsync(c => c.Id == dto.PracticeCategoryId.Value, cancellationToken))
            {
                return new ResponseModel<PracticeSelectDto>(false, null, "Invalid PracticeCategoryId");
            }

            var requestedFileIds = CollectRequestedGymFileIds(dto);
            if (!await ValidateGymFileOwnershipAsync(requestedFileIds, gymId, cancellationToken))
            {
                return new ResponseModel<PracticeSelectDto>(false, null, "Invalid gym file reference");
            }

            var entity = dto.ToEntity(_mapper);
            entity.UserId = userId; // submitter/owner
            entity.CreateDate = System.DateTime.Now;

            await _practiceRepo.AddAsync(entity, cancellationToken);

            var mediaEntities = new List<PracticeMedia>();
            mediaEntities.AddRange(CreateMediaEntities(entity.Id, dto.Images, MediaFileType.Image));
            mediaEntities.AddRange(CreateMediaEntities(entity.Id, dto.Videos, MediaFileType.Video));
            entity.MediaItems = mediaEntities;

            if (mediaEntities.Any())
            {
                await _practiceMediaRepo.AddRangeAsync(mediaEntities, cancellationToken);
            }

            var model = PracticeSelectDto.FromEntity(_mapper, entity);
            return new ResponseModel<PracticeSelectDto>(true, model);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, PracticeDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _practiceRepo.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            var requestedFileIds = CollectRequestedGymFileIds(dto);
            if (!await ValidateGymFileOwnershipAsync(requestedFileIds, gymId, cancellationToken))
            {
                return new ResponseModel(false, "Invalid gym file reference");
            }

            // Update allowed fields explicitly to avoid key modification
            entity.Name = dto.Name;
            entity.Desc = dto.Desc;

            if (dto.PracticeCategoryId.HasValue)
            {
                var catId = dto.PracticeCategoryId.Value;
                var catExists = await _categoryRepo.TableNoTracking.AnyAsync(c => c.Id == catId, cancellationToken);
                if (!catExists)
                    return new ResponseModel(false, "Invalid PracticeCategoryId");
                entity.PracticeCategoryId = catId;
            }

            var existingMedia = await _practiceMediaRepo.Table
                .Where(m => m.PracticeId == id)
                .ToListAsync(cancellationToken);

            var mediaDictionary = existingMedia.ToDictionary(m => m.Id);
            var inserts = new List<PracticeMedia>();
            var updates = new List<PracticeMedia>();
            var retained = new HashSet<int>();

            if (!TryApplyMediaChanges(id, dto.Images, MediaFileType.Image, mediaDictionary, inserts, updates, retained, out var mediaError) ||
                !TryApplyMediaChanges(id, dto.Videos, MediaFileType.Video, mediaDictionary, inserts, updates, retained, out mediaError))
            {
                return new ResponseModel(false, mediaError);
            }

            var toRemove = existingMedia.Where(m => !retained.Contains(m.Id)).ToList();

            await _practiceRepo.UpdateAsync(entity, cancellationToken);

            if (toRemove.Any())
                await _practiceMediaRepo.DeleteRangeAsync(toRemove, cancellationToken);
            if (updates.Any())
                await _practiceMediaRepo.UpdateRangeAsync(updates, cancellationToken);
            if (inserts.Any())
                await _practiceMediaRepo.AddRangeAsync(inserts, cancellationToken);

            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _practiceRepo.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            await _practiceRepo.DeleteAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<PagedResult<PracticeSelectDto>>> GetListAsync(int gymId, int userId, string q, Pageres pager, CancellationToken cancellationToken)
        {
            pager ??= new Pageres();
            pager.Normalize();

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PagedResult<PracticeSelectDto>>(false, null, "Access denied");

            var query = _practiceRepo.TableNoTracking;
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Name.Contains(q));

            var ordered = query.OrderByDescending(x => x.Id);
            var totalCount = await ordered.CountAsync(cancellationToken);

            var items = await ordered
                .Paginate(pager)
                .ProjectTo<PracticeSelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<PracticeSelectDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pager.PageNumber,
                PageSize = pager.PageSize
            };

            return new ResponseModel<PagedResult<PracticeSelectDto>>(true, result);
        }

        public async Task<ResponseModel<PracticeSelectDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PracticeSelectDto>(false, null, "Access denied");

            var item = await _practiceRepo.TableNoTracking
                .Where(x => x.Id == id)
                .ProjectTo<PracticeSelectDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null)
                return new ResponseModel<PracticeSelectDto>(false, null, "Not found");

            return new ResponseModel<PracticeSelectDto>(true, item);
        }

        private async Task<bool> ValidateGymFileOwnershipAsync(IEnumerable<int> gymFileIds, int gymId, CancellationToken cancellationToken)
        {
            var ids = (gymFileIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (!ids.Any())
                return true;

            var matchedCount = await _gymFileRepo.TableNoTracking
                .Where(f => f.GymId == gymId && ids.Contains(f.Id))
                .Select(f => f.Id)
                .Distinct()
                .CountAsync(cancellationToken);

            return matchedCount == ids.Count;
        }

        private static IEnumerable<int> CollectRequestedGymFileIds(PracticeDto dto)
        {
            if (dto == null)
                return Enumerable.Empty<int>();

            var imageIds = dto.Images?.Where(m => m != null).Select(m => m.GymFileId) ?? Enumerable.Empty<int>();
            var videoIds = dto.Videos?.Where(m => m != null).Select(m => m.GymFileId) ?? Enumerable.Empty<int>();

            return imageIds.Concat(videoIds).Where(id => id > 0);
        }

        private static List<PracticeMedia> CreateMediaEntities(int practiceId, IEnumerable<PracticeMediaRequestDto> requests, MediaFileType type)
        {
            var normalized = PrepareMediaRequests(requests);
            var result = new List<PracticeMedia>(normalized.Count);
            foreach (var (request, order) in normalized)
            {
                result.Add(new PracticeMedia
                {
                    PracticeId = practiceId,
                    GymFileId = request.GymFileId,
                    MediaType = type,
                    DisplayOrder = order
                });
            }

            return result;
        }

        private static List<(PracticeMediaRequestDto Request, int Order)> PrepareMediaRequests(IEnumerable<PracticeMediaRequestDto> items)
        {
            var result = new List<(PracticeMediaRequestDto Request, int Order)>();
            if (items == null)
                return result;

            var ordered = items
                .Where(item => item != null)
                .Select((item, index) => new { item, index })
                .OrderBy(x => x.item.Order)
                .ThenBy(x => x.index)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                result.Add((ordered[i].item, i));
            }

            return result;
        }

        private bool TryApplyMediaChanges(
            int practiceId,
            IEnumerable<PracticeMediaRequestDto> requests,
            MediaFileType type,
            IDictionary<int, PracticeMedia> existingMedia,
            List<PracticeMedia> inserts,
            List<PracticeMedia> updates,
            HashSet<int> retainedIds,
            out string errorMessage)
        {
            errorMessage = null;

            var normalized = PrepareMediaRequests(requests);
            foreach (var (request, order) in normalized)
            {
                if (request.Id.HasValue)
                {
                    if (!existingMedia.TryGetValue(request.Id.Value, out var current))
                    {
                        errorMessage = "Invalid media reference";
                        return false;
                    }

                    if (current.PracticeId != practiceId || current.MediaType != type)
                    {
                        errorMessage = "Invalid media reference";
                        return false;
                    }

                    if (current.GymFileId != request.GymFileId || current.DisplayOrder != order)
                    {
                        current.GymFileId = request.GymFileId;
                        current.DisplayOrder = order;
                        updates.Add(current);
                    }

                    if (!retainedIds.Add(current.Id))
                    {
                        errorMessage = "Duplicate media reference";
                        return false;
                    }
                }
                else
                {
                    inserts.Add(new PracticeMedia
                    {
                        PracticeId = practiceId,
                        GymFileId = request.GymFileId,
                        MediaType = type,
                        DisplayOrder = order
                    });
                }
            }

            return true;
        }
    }
}
