using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Enums;

namespace Services.Services.CMS.PracticeCategory
{
    public class PracticeCategoryService : IScopedDependency, IPracticeCategoryService
    {
        private readonly IRepository<Entities.PracticeCategory> _repository;
        private readonly IMapper _mapper;
        private readonly IRepository<GymUser> _gymUserRepo;

        public PracticeCategoryService(IRepository<Entities.PracticeCategory> repository, IMapper mapper, IRepository<GymUser> gymUserRepo)
        {
            _repository = repository;
            _mapper = mapper;
            _gymUserRepo = gymUserRepo;
        }

        public async Task<ResponseModel<PracticeCategorySelectDto>> CreateAsync(int gymId, int userId, PracticeCategoryDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<PracticeCategorySelectDto>(false, null, "Access denied");

            var entity = dto.ToEntity(_mapper);
            entity.SubmitterUserId = userId;
            await _repository.AddAsync(entity, cancellationToken);

            var model = PracticeCategorySelectDto.FromEntity(_mapper, entity);
            return new ResponseModel<PracticeCategorySelectDto>(true, model);
        }

        public async Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, PracticeCategoryDto dto, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "دسترسی ندارید");

            var entity = await _repository.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "یافت نشد");

            // Avoid mapping Id from DTO onto tracked entity to prevent key modification
            // Only update allowed fields explicitly
            entity.Title = dto.Title;
            await _repository.UpdateAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "  دسترسی ندارید");

            var entity = await _repository.Table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "یافت نشد");

            await _repository.DeleteAsync(entity, cancellationToken);
            return new ResponseModel(true, "");
        }

        public async Task<ResponseModel<List<PracticeCategorySelectDto>>> GetListAsync(int gymId, int userId, string? q, CancellationToken cancellationToken)
        {
            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<List<PracticeCategorySelectDto>>(false, null, "Access denied");

            var query = _repository.TableNoTracking;
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.Contains(q));

            var list = await query
                .Include(x=> x.SubmitterUser)
                .OrderByDescending(x => x.Id)
                .ProjectTo<PracticeCategorySelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new ResponseModel<List<PracticeCategorySelectDto>>(true, list);
        }
    }
}
