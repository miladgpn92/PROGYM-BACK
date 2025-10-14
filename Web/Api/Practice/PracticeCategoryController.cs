using Common.Consts;
using Common;
using DariaCMS.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.CMS.PracticeCategory;
using Shared.Api;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.Practice
{
    [ApiVersion("1")]
    [Route("api/manager/practicecategory")]
    [ApiExplorerSettings(GroupName = RoleConsts.Manager)]
    [Authorize(Roles = RoleConsts.Manager, AuthenticationSchemes = "JwtScheme")]
    public class PracticeCategoryController : BaseController
    {
        private readonly IPracticeCategoryService _service;

        public PracticeCategoryController(IPracticeCategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int gymId, [FromBody] PracticeCategoryDto dto, CancellationToken cancellationToken)
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
        public async Task<IActionResult> Update([FromQuery] int gymId, int id, [FromBody] PracticeCategoryDto dto, CancellationToken cancellationToken)
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
