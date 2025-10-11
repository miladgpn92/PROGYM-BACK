using Common;
using Common.Consts;
using DariaCMS.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.CMS.Gym;
using Shared.Api;
using SharedModels.Dtos.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.Gym
{
    [ApiVersion("1")]
    [Authorize(Roles = RoleConsts.Manager, AuthenticationSchemes = "JwtScheme")]
    public class GymFilesController : BaseController
    {
        private readonly IGymFileService _service;

        public GymFilesController(IGymFileService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int gymId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = User.Identity.GetUserIdInt();
            var request = new GymFileListRequest
            {
                Pager = new Pageres
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            };

            var res = await _service.GetListAsync(gymId, userId, request, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromQuery] int gymId, [FromForm] GymFileUploadRequest request, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.UploadAsync(gymId, userId, request, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int gymId, [FromBody] GymFileDeleteRequest request, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.DeleteAsync(gymId, userId, request, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            return BadRequest(res.Description);
        }
    }
}
