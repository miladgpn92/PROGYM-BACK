using System;
using System.Threading;
using System.Threading.Tasks;
using DariaCMS.Common;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Services.Site.GymLanding;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;

namespace Web.Pages.Gyms
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private const int DefaultPageSize = 12;
        private const int MaxPageSize = 60;

        private readonly IGymLandingService _gymLandingService;

        public IndexModel(IGymLandingService gymLandingService)
        {
            _gymLandingService = gymLandingService;
        }

        [BindProperty(SupportsGet = true)]
        public Pageres Arg { get; set; } = new() { PageNumber = 1, PageSize = DefaultPageSize };

        public PagedResult<GymLandingListItemDto> Gyms { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            Arg ??= new Pageres { PageNumber = 1, PageSize = DefaultPageSize };
            Arg.Normalize(defaultPageSize: DefaultPageSize, maxPageSize: MaxPageSize);

            Gyms = await _gymLandingService.GetGymsAsync(Arg, cancellationToken);

            if (Gyms.TotalCount > 0 && Gyms.Items.Count == 0 && Arg.PageNumber > 1)
            {
                var pageSize = Gyms.PageSize > 0 ? Gyms.PageSize : DefaultPageSize;
                var lastPage = (int)Math.Ceiling(Gyms.TotalCount / (double)pageSize);
                lastPage = Math.Max(1, lastPage);

                if (lastPage != Arg.PageNumber)
                {
                    Arg.PageNumber = lastPage;
                    Gyms = await _gymLandingService.GetGymsAsync(Arg, cancellationToken);
                }
            }


            SEODto PageSeo = new SEODto()
            {
                SEOTitle ="لیست باشگاه ها",
                SEODesc = "لیست باشگاه های پرو جیم",
            
            };

            ViewData["Seo"] = PageSeo;
            return Page();
        }
    }
}
