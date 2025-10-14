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
        private readonly IMapper _mapper;

        public PracticeService(
            IRepository<Entities.Practice> practiceRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<Entities.PracticeCategory> categoryRepo,
            IRepository<GymFile> gymFileRepo,
            IMapper mapper)
        {
            _practiceRepo = practiceRepo;
            _gymUserRepo = gymUserRepo;
            _categoryRepo = categoryRepo;
            _gymFileRepo = gymFileRepo;
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

            if (!await ValidateGymFileOwnershipAsync(dto.ThumbGymFileId, gymId, cancellationToken) ||
                !await ValidateGymFileOwnershipAsync(dto.VideoGymFileId, gymId, cancellationToken))
            {
                return new ResponseModel<PracticeSelectDto>(false, null, "Invalid gym file reference");
            }

            var entity = dto.ToEntity(_mapper);
            entity.UserId = userId; // submitter/owner
            entity.CreateDate = System.DateTime.Now;
            entity.ThumbFileId = dto.ThumbGymFileId;
            entity.VideoFileId = dto.VideoGymFileId;

            await _practiceRepo.AddAsync(entity, cancellationToken);

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

            if (!await ValidateGymFileOwnershipAsync(dto.ThumbGymFileId, gymId, cancellationToken) ||
                !await ValidateGymFileOwnershipAsync(dto.VideoGymFileId, gymId, cancellationToken))
            {
                return new ResponseModel(false, "Invalid gym file reference");
            }

            // Update allowed fields explicitly to avoid key modification
            entity.Name = dto.Name;
            entity.Desc = dto.Desc;
            entity.ThumbFileId = dto.ThumbGymFileId;
            entity.VideoFileId = dto.VideoGymFileId;
            if (dto.PracticeCategoryId.HasValue)
            {
                var catId = dto.PracticeCategoryId.Value;
                var catExists = await _categoryRepo.TableNoTracking.AnyAsync(c => c.Id == catId, cancellationToken);
                if (!catExists)
                    return new ResponseModel(false, "Invalid PracticeCategoryId");
                entity.PracticeCategoryId = catId;
            }

            await _practiceRepo.UpdateAsync(entity, cancellationToken);
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

        private async Task<bool> ValidateGymFileOwnershipAsync(int? gymFileId, int gymId, CancellationToken cancellationToken)
        {
            if (!gymFileId.HasValue)
                return true;

            return await _gymFileRepo.TableNoTracking
                .AnyAsync(f => f.Id == gymFileId.Value && f.GymId == gymId, cancellationToken);
        }
    }
}
