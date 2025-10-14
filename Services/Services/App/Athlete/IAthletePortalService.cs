using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using DariaCMS.Common;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;

namespace Services.Services.App.Athlete
{
    public interface IAthletePortalService
    {
        Task<ResponseModel<List<AthleteDataDto>>> GetAthleteDataAsync(int userId, CancellationToken cancellationToken);
        Task<ResponseModel<int>> AddAthleteDataAsync(int userId, AthleteDataCreateDto dto, CancellationToken cancellationToken);

        Task<ResponseModel<PagedResult<AthleteCurrentProgramDto>>> GetCurrentProgramAsync(int userId, Pageres pager, CancellationToken cancellationToken);
        Task<ResponseModel<ProgramDetailDto>> GetProgramDetailAsync(int userId, int programId, CancellationToken cancellationToken);

        Task<ResponseModel<int>> CreateExerciseAsync(int userId, ExerciseCreateDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> CompleteExerciseAsync(int userId, int exerciseId, CancellationToken cancellationToken);

        Task<ResponseModel<GymSelectDto>> GetGymAsync(int userId, int gymId, CancellationToken cancellationToken);

        Task<ResponseModel<ProfileDto>> GetProfileAsync(int userId, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateProfileAsync(int userId, AthleteProfileUpdateDto dto, CancellationToken cancellationToken);
    }
}
