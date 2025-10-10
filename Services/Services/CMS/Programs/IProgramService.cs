using Common;
using SharedModels.Dtos.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.CMS.Programs
{
    public interface IProgramService
    {
        Task<ResponseModel<ProgramSelectDto>> CreateAsync(int gymId, int userId, ProgramDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateAsync(int gymId, int userId, int id, ProgramDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel<List<ProgramSelectDto>>> GetListAsync(int gymId, int userId, string q, CancellationToken cancellationToken);
        Task<ResponseModel<ProgramDetailDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel> DeletePracticeAsync(int gymId, int userId, int programPracticeId, CancellationToken cancellationToken);
        Task<ResponseModel> AttachToAthleteAsync(
            int gymId,
            int managerUserId,
            int programId,
            int athleteUserId,
            System.DateTime startDate,
            System.DateTime? endDate,
            CancellationToken cancellationToken);
    }
}
