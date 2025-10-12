using Common.Consts;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.App.Athlete;
using Shared.Api;
using SharedModels.Dtos.Shared;
using SharedModels.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Api.AthleteApp
{
    [ApiVersion("1")]
    [Route("api/athlete/portal")]
    [ApiExplorerSettings(GroupName = RoleConsts.Athlete)]
    [Authorize(Roles = RoleConsts.Athlete, AuthenticationSchemes = "JwtScheme")]
    public class AthletePortalController : BaseController
    {
        private readonly IAthletePortalService _service;

        public AthletePortalController(IAthletePortalService service)
        {
            _service = service;
        }

        [HttpGet("athlete-data")]
        public async Task<IActionResult> GetAthleteData(CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.GetAthleteDataAsync(userId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpPost("athlete-data")]
        public async Task<IActionResult> AddAthleteData([FromBody] AthleteDataCreateDto dto, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.AddAthleteDataAsync(userId, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpGet("program/current")]
        public async Task<IActionResult> GetCurrentProgram(CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.GetCurrentProgramAsync(userId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpGet("program/{programId:int}")]
        public async Task<IActionResult> GetProgramDetail(int programId, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.GetProgramDetailAsync(userId, programId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpPost("exercises")]
        public async Task<IActionResult> CreateExercise([FromBody] ExerciseCreateDto dto, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.CreateExerciseAsync(userId, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpPost("exercises/{exerciseId:int}/complete")]
        public async Task<IActionResult> CompleteExercise(int exerciseId, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.CompleteExerciseAsync(userId, exerciseId, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            return BadRequest(res.Description);
        }

        [HttpGet("gym/current")]
        public async Task<IActionResult> GetCurrentGym([FromQuery] int gymId, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.GetGymAsync(userId, gymId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.GetProfileAsync(userId, cancellationToken);
            if (res.IsSuccess)
                return Ok(res.Model);
            return BadRequest(res.Message);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] AthleteProfileUpdateDto dto, CancellationToken cancellationToken)
        {
            var userId = User.Identity.GetUserIdInt();
            var res = await _service.UpdateProfileAsync(userId, dto, cancellationToken);
            if (res.IsSuccess)
                return Ok();
            return BadRequest(res.Description);
        }
    }
}
