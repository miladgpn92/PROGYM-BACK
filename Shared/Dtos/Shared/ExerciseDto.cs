using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ExerciseDto : SimpleBaseDto<ExerciseDto, Exercise>
    {
        [Display(Name = "کاربر")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? UserId { get; set; }

        [Display(Name = "برنامه")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? ProgramId { get; set; }


        [Display(Name = "تکمیل شده")]
        public bool Complete { get; set; }
    }

    public class ExerciseSelectDto : SimpleBaseDto<ExerciseSelectDto, Exercise>
    {
        public int? UserId { get; set; }
        public int? ProgramId { get; set; }
        public string ProgramTitle { get; set; }
        public DateTime? Date { get; set; }
        public bool Complete { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }
    }
}

