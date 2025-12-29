using Common;
using Common.Consts;
using DariaCMS.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.CMS.ProgramCategory;
using Shared.Api;
using SharedModels.Dtos.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.Programs
{
    [ApiVersion("1")]
    [Route("api/manager/programcategory")]
    [ApiExplorerSettings(GroupName = RoleConsts.Manager)]
    [Authorize(Roles = RoleConsts.Manager, AuthenticationSchemes = "JwtScheme")]
    public class ProgramCategoryController : BaseController
    {
        private readonly IProgramCategoryService _service;

        public ProgramCategoryController(IProgramCategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int gymId, [FromBody] ProgramCategoryDto dto, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.CreateAsync(gymId, userId, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> List(
            [FromQuery] int gymId,
            [FromQuery] string? q,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = User.Identity.GetUserIdInt();
            var pager = new Pageres { PageNumber = pageNumber, PageSize = pageSize };
            var res = await _service.GetListAsync(gymId, userId, q, pager, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromQuery] int gymId, int id, [FromBody] ProgramCategoryDto dto, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.UpdateAsync(gymId, userId, id, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            else
                return BadRequest(res.Description);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int gymId, int id, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.DeleteAsync(gymId, userId, id, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            else
                return BadRequest(res.Description);
        }
    }
}
