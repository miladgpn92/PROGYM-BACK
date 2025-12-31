using Common;
using Common.Enums;
using DariaCMS.Common;
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
        Task<ResponseModel<PagedResult<ProgramSelectDto>>> GetListAsync(int gymId, int userId, string q, ProgramTypes? type, IEnumerable<int>? categoryIds, bool includeAll, Pageres pager, CancellationToken cancellationToken);
        Task<ResponseModel<ProgramDetailDto>> GetByIdAsync(int gymId, int userId, int id, CancellationToken cancellationToken);
        Task<ResponseModel> DeleteRoutineItemAsync(int gymId, int userId, int routineItemId, CancellationToken cancellationToken);
        Task<ResponseModel> ReorderRoutineItemsAsync(int gymId, int userId, int programId, ProgramRoutineItemReorderDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> ReorderSupersetPracticesAsync(int gymId, int userId, ProgramSupersetPracticeReorderDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> UpdateRoutineItemMetadataAsync(int gymId, int userId, ProgramRoutineItemMetadataDto dto, CancellationToken cancellationToken);
        Task<ResponseModel> AttachToAthleteAsync(
            int gymId,
            int managerUserId,
            int programId,
            int athleteUserId,
            System.DateTime startDate,
            System.DateTime? endDate,
            CancellationToken cancellationToken);
        Task<ResponseModel> DeAttachAthleteAsync(
            int gymId,
            int managerUserId,
            int userProgramId,
            CancellationToken cancellationToken);
        Task<ResponseModel> UpdateUserProgramDatesAsync(
            int gymId,
            int managerUserId,
            int userProgramId,
            System.DateTime startDate,
            System.DateTime? endDate,
            CancellationToken cancellationToken);
    }
}
