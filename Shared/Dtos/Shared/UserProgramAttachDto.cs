using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class UserProgramAttachDto
    {
        [Required]
        public int ProgramId { get; set; }

        [Required]
        public int AthleteUserId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "RepeatCount must be greater than zero")]
        public int? RepeatCount { get; set; }
    }
}
