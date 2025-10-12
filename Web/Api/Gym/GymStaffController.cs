using Common;
using Common.Consts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.CMS.GymStaff;
using Shared.Api;
using SharedModels.Dtos.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.Gym
{
    [ApiVersion("1")]
    [Route("api/manager/gymstaff")]
    [ApiExplorerSettings(GroupName = RoleConsts.Manager)]
    [Authorize(Roles = RoleConsts.Manager, AuthenticationSchemes = "JwtScheme")]
    public class GymStaffController : BaseController
    {
        private readonly IGymStaffService _service;

        public GymStaffController(IGymStaffService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int gymId, [FromBody] GymStaffCreateDto dto, CancellationToken cancellationToken)
        {
            var managerId = User.Identity.GetUserIdInt();
            var res = await _service.CreateAsync(gymId, managerId, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> List([FromQuery] int gymId, string? q, CancellationToken cancellationToken)
        {
            var managerId = User.Identity.GetUserIdInt();
            var res = await _service.GetListAsync(gymId, managerId, q ?? string.Empty, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetById([FromQuery] int gymId, int id, CancellationToken cancellationToken)
        {
            var managerId = User.Identity.GetUserIdInt();
            var res = await _service.GetByIdAsync(gymId, managerId, id, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromQuery] int gymId, int id, [FromBody] GymStaffUpdateDto dto, CancellationToken cancellationToken)
        {
            var managerId = User.Identity.GetUserIdInt();
            var res = await _service.UpdateAsync(gymId, managerId, id, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            else
                return BadRequest(res.Description);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int gymId, int id, CancellationToken cancellationToken)
        {
            var managerId = User.Identity.GetUserIdInt();
            var res = await _service.DeleteAsync(gymId, managerId, id, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            else
                return BadRequest(res.Description);
        }
    }
}
