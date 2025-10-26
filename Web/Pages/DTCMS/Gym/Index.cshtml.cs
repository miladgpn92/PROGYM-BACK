using AutoMapper;
using Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SharedModels;
using SharedModels.Dtos.Shared;
using Web.Infrastructure;

namespace Web.Pages.DTCMS.Gym
{
    [Authorize]
    public class IndexModel : GenericPageModel<GymSelectDto, Entities.Gym>
    {
        public IndexModel(IRepository<Entities.Gym> repository, IMapper mapper) : base(repository, mapper)
        {
        }

        public override Task<IActionResult> OnGetAsync(PageListModel model, string filter, CancellationToken cancellationToken)
        {
            return base.OnGetAsync(model, filter, cancellationToken);
        }
    }
}
