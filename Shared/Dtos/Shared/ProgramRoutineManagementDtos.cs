using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ProgramRoutineItemReorderDto
    {
        [Required]
        public int ProgramId { get; set; }

        [Required]
        [MinLength(1)]
        public List<ProgramRoutineItemOrderDto> Items { get; set; } = new List<ProgramRoutineItemOrderDto>();
    }

    public class ProgramRoutineItemOrderDto
    {
        [Required]
        public int RoutineItemId { get; set; }

        [Required]
        public int DisplayOrder { get; set; }
    }

    public class ProgramSupersetPracticeReorderDto
    {
        [Required]
        public int RoutineItemId { get; set; }

        [Required]
        [MinLength(1)]
        public List<ProgramSupersetPracticeOrderDto> Practices { get; set; } = new List<ProgramSupersetPracticeOrderDto>();
    }

    public class ProgramSupersetPracticeOrderDto
    {
        [Required]
        public int ProgramPracticeId { get; set; }

        [Required]
        public int InternalOrder { get; set; }
    }

    public class ProgramRoutineItemMetadataDto
    {
        [Required]
        public int RoutineItemId { get; set; }

        public string Title { get; set; }
        public int? RepeatCount { get; set; }
        public int? RestBetweenRepeats { get; set; }
        public string Notes { get; set; }
    }
}

