using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utilities;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;

namespace Services.Services.Site.GymLanding
{
    public class GymLandingService : IGymLandingService
    {
        private readonly IRepository<Entities.Gym> _gymRepository;

        public GymLandingService(IRepository<Entities.Gym> gymRepository)
        {
            _gymRepository = gymRepository;
        }

        public async Task<List<GymLandingListItemDto>> GetLatestGymsAsync(int count, CancellationToken cancellationToken)
        {
            count = Math.Max(1, count);
            var language = CmsEx.GetCurrentLanguage();

            return await _gymRepository.TableNoTracking
                .Where(g => g.CmsLanguage == language)
                .OrderByDescending(g => g.CreateDate ?? g.PublishDate ?? DateTime.MinValue)
                .Select(g => new GymLandingListItemDto
                {
                    Id = g.Id,
                    Title = g.Title,
                    Address = g.Address,
                    LogoUrl = g.LogoUrl,
                    Slug = g.Slug
                })
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
