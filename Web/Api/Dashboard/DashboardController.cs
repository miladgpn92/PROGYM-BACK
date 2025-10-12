using Common;
using Common.Consts;
using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.CMS.Dashboard;
using Shared.Api;
using SharedModels.Dtos.Dashboard;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.Dashboard
{
    [ApiVersion("1")]
    [Route("api/manager/dashboard")]
    [ApiExplorerSettings(GroupName = RoleConsts.Manager)]
    [Authorize(AuthenticationSchemes = "JwtScheme")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Returns count metrics for a manager: users, practices, programs.
        /// </summary>
        [HttpGet("ManagerCounts")]
        public async Task<ActionResult<ManagerCountsDto>> ManagerCounts([FromQuery] int gymId, CancellationToken cancellationToken)
        {
            var userId = HttpContext.User.Identity.GetUserIdInt();
            var res = await _dashboardService.GetManagerCountsAsync(gymId, userId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            else
                return BadRequest(res.Message);
        }
    }
}
