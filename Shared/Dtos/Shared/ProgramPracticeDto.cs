using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ProgramPracticeInputDto
    {
        [Required]
        public int? PracticeId { get; set; }

        [Required]
        public PracticeType Type { get; set; }

        // For Type = Set
        public int? SetCount { get; set; }
        public int? MovementCount { get; set; }

        // For Type = Time
        public int? Duration { get; set; }

        // Shared
        public int? Rest { get; set; }
    }
}
