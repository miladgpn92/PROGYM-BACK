using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Consts;
using Common.Enums;
using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Services.CMS.UserGym;
using SharedModels.Dtos.Shared;

namespace Web.Pages.DTCMS.Gym
{
    [Authorize(Roles = RoleConsts.Admin)]
    public class UsersModel : PageModel
    {
        private readonly IUserGymService userGymService;

        public UsersModel(IUserGymService userGymService)
        {
            this.userGymService = userGymService;
            AvailableRoles = BuildRoleSelectItems();
        }

        [BindProperty(SupportsGet = true)]
        public int GymId { get; set; }

        public List<GymUserListItemDto> GymUsers { get; private set; } = new();

        [BindProperty]
        public GymUserCreateDto Input { get; set; } = new();

        public List<SelectListItem> AvailableRoles { get; private set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (GymId <= 0)
            {
                return NotFound();
            }

            await LoadGymUsersAsync(cancellationToken);
            AvailableRoles = BuildRoleSelectItems();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (GymId <= 0)
            {
                return NotFound();
            }

            AvailableRoles = BuildRoleSelectItems();

            if (!ModelState.IsValid)
            {
                await LoadGymUsersAsync(cancellationToken);
                return Page();
            }

            var result = await userGymService.AddUserToGymAsync(GymId, Input, cancellationToken);
            if (!result.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(result.Description)
                    ? "Unable to add the user to this gym."
                    : result.Description;

                ModelState.AddModelError(string.Empty, message);
                await LoadGymUsersAsync(cancellationToken);
                return Page();
            }

            StatusMessage = "User added to the gym successfully.";
            return RedirectToPage(new { gymId = GymId });
        }

        private async Task LoadGymUsersAsync(CancellationToken cancellationToken)
        {
            var response = await userGymService.GetGymUsersAsync(GymId, cancellationToken);
            if (response.IsSuccess && response.Model != null)
            {
                GymUsers = response.Model;
            }
            else if (!response.IsSuccess && !string.IsNullOrWhiteSpace(response.Message))
            {
                ModelState.AddModelError(string.Empty, response.Message);
            }
        }

        private List<SelectListItem> BuildRoleSelectItems()
        {
            return Enum.GetValues(typeof(UsersRole))
                       .Cast<UsersRole>()
                       .Select(role => new SelectListItem
                       {
                           Value = role.ToString(),
                           Text = role.GetDisplayName() ?? role.ToString()
                       })
                       .ToList();
        }
    }
}
