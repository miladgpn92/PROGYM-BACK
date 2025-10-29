using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utilities;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using DariaCMS.Common;
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

        public async Task<PagedResult<GymLandingListItemDto>> GetGymsAsync(Pageres pager, CancellationToken cancellationToken)
        {
            pager ??= new Pageres();
            pager.Normalize(defaultPageSize: 12, maxPageSize: 60);

            var language = CmsEx.GetCurrentLanguage();

            var query = _gymRepository.TableNoTracking
                .Where(g => g.CmsLanguage == language)
                .OrderByDescending(g => g.CreateDate ?? g.PublishDate ?? DateTime.MinValue)
                .Select(g => new GymLandingListItemDto
                {
                    Id = g.Id,
                    Title = g.Title,
                    Address = g.Address,
                    LogoUrl = g.LogoUrl,
                    Slug = g.Slug
                });

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Paginate(pager)
                .ToListAsync(cancellationToken);

            return new PagedResult<GymLandingListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pager.PageNumber,
                PageSize = pager.PageSize
            };
        }

        public async Task<GymLandingDetailDto?> GetGymBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            var language = CmsEx.GetCurrentLanguage();
            slug = slug.Trim();

            return await _gymRepository.TableNoTracking
                .Where(g => g.CmsLanguage == language && g.Slug == slug)
                .Select(g => new GymLandingDetailDto
                {
                    Id = g.Id,
                    Title = g.Title,
                    Address = g.Address,
                    LogoUrl = g.LogoUrl,
                    Slug = g.Slug,
                    ContactUsPhoneNumber = g.ContactUsPhoneNumber,
                    Phone = g.Phone,
                    Lat = g.Lat,
                    Lng = g.Lng,
                    InstagramLink = g.InstagramLink,
                    TelegramLink = g.TelegramLink,
                    EitaaLink = g.EitaaLink,
                    BaleLink = g.BaleLink
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
