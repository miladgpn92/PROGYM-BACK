using Common.Enums;
using ResourceLibrary.Resources.ErrorMsg;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class AthleteDataCreateDto
    {
        [Range(1, 300, ErrorMessage = "Height must be between 1 and 300")]
        public int Height { get; set; }

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int Age { get; set; }

        [Range(typeof(decimal), "1", "400", ErrorMessage = "Weight must be between 1 and 400")]
        public decimal Weight { get; set; }
    }

    public class AthleteCurrentProgramDto
    {
        public int UserProgramId { get; set; }
        public int ProgramId { get; set; }
        public string ProgramTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ExerciseCreateDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int ProgramId { get; set; }

        public DateTime? Date { get; set; }
    }

    public class ExerciseCompleteDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int ExerciseId { get; set; }
    }

    public class AthleteProfileUpdateDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(100, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(100, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Family { get; set; }

        public Gender? Gender { get; set; }

        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string PicUrl { get; set; }
    }
}
