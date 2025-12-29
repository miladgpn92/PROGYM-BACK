using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using DariaCMS.Common;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.ProgramCategory
{
    public class ProgramCategoryService : IScopedDependency, IProgramCategoryService
    {
        private readonly IRepository<Entities.ProgramCategory> _repository;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IMapper _mapper;

        public ProgramCategoryService(
            IRepository<Entities.ProgramCategory> repository,
            IRepository<GymUser> gymUserRepo,
            IMapper mapper)
        {
            _repository = repository;
            _gymUserRepo = gymUserRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel<ProgramCategorySelectDto>> CreateAsync(int gymId, int userId, ProgramCategoryDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<ProgramCategorySelectDto>(false, null, "Access denied");

            var entity = dto.ToEntity(_mapper);
            entity.SubmitterUserId = userId;
            entity.GymId = gymId;
            await _repository.AddAsync(entity, cancellationToken);

            var model = ProgramCategorySelectDto.FromEntity(_mapper, entity);
            return new ResponseModel<ProgramCategorySelectDto>(true, model);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, ProgramCategoryDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _repository.Table.FirstOrDefaultAsync(x => x.Id == id && x.GymId == gymId, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            entity.Title = dto.Title;
            await _repository.UpdateAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var entity = await _repository.Table.FirstOrDefaultAsync(x => x.Id == id && x.GymId == gymId, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "Not found");

            await _repository.DeleteAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<PagedResult<ProgramCategorySelectDto>>> GetListAsync(int gymId, int userId, string? q, Pageres pager, CancellationToken cancellationToken)
        {
            pager ??= new Pageres();
            pager.Normalize();

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PagedResult<ProgramCategorySelectDto>>(false, null, "Access denied");

            var query = _repository.TableNoTracking
                .Where(x => x.GymId == gymId);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.Contains(q));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(x => x.SubmitterUser)
                .OrderByDescending(x => x.Id)
                .Paginate(pager)
                .ProjectTo<ProgramCategorySelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ProgramCategorySelectDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pager.PageNumber,
                PageSize = pager.PageSize
            };

            return new ResponseModel<PagedResult<ProgramCategorySelectDto>>(true, result);
        }
    }
}
