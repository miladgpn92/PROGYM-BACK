using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Services.Services.CMS;
using SharedModels.Dtos.Shared;
using System.Web;

namespace Web.Pages.DTCMS.Gym
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly IRepository<Entities.Gym> _repository;
        private readonly ISlugService<Entities.Gym> _slugService;

        public UpsertModel(IRepository<Entities.Gym> repository, ISlugService<Entities.Gym> slugService, IMapper mapper)
        {
            _repository = repository;
            _slugService = slugService;
            Mapper = mapper;
        }

        [BindProperty]
        public GymDto? Items { get; set; } = new();



        [BindProperty]
        public bool IsEdit { get; set; }
        public IMapper Mapper { get; }

        [BindProperty]
        public bool? _IsReturn { get; set; }

        public async Task OnGetAsync(int? id, bool? IsReturn, CancellationToken cancellationToken)
        {
            _IsReturn = IsReturn;

            if (id.HasValue)
            {
                IsEdit = true;

                Items = await _repository.TableNoTracking.ProjectTo<GymDto>(Mapper.ConfigurationProvider)
                  .SingleOrDefaultAsync(p => p.Id.Equals(id), cancellationToken);



            }
            else
                IsEdit = false;
        }


        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }


            if (IsEdit)
            {
                var model = await _repository.GetByIdAsync(cancellationToken, Items?.Id);
                if (model.Slug != Items.Slug)
                {
                    if (Items.Slug != null)
                        Items.Slug = _slugService.CheckSlug(Items.Slug, cancellationToken);
                }
                model = Items?.ToEntity(Mapper, model);
                if (model != null)
                    await _repository.UpdateAsync(model, cancellationToken);
                if (_IsReturn != null && _IsReturn == true)
                {
                    return Redirect($@"/gym/{HttpUtility.UrlEncode(model.Slug)}");
                }
            }
            else
            {
                if (Items?.Slug != null)
                    Items.Slug = _slugService.CheckSlug(Items.Slug, cancellationToken);
                var model = Items?.ToEntity(Mapper);
                if (model != null)
                    await _repository.AddAsync(model, cancellationToken);

                if (_IsReturn != null && _IsReturn == true)
                {
                    return Redirect($@"/gym/{HttpUtility.UrlEncode(model.Slug)}");
                }

            }



            return RedirectToPage("./Index");
        }



    }
}
