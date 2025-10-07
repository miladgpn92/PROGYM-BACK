using Common.Enums;
using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ProgramDto : SimpleBaseDto<ProgramDto, Program>
    {
        [Display(Name = "عنوان برنامه")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }

        [Display(Name = "تعداد تمرین‌ها")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? CountOfPractice { get; set; }

        [Display(Name = "نوع")]
        [MaxLength(100, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public ProgramTypes Type { get; set; }

        [Display(Name = "مالک")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? OwnerId { get; set; }

        [Display(Name = "ثبت‌کننده")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? SubmitterUserId { get; set; }
    }

    public class ProgramSelectDto : SimpleBaseDto<ProgramSelectDto, Program>
    {
        public string Title { get; set; }
        public int? CountOfPractice { get; set; }
        public ProgramTypes Type { get; set; }
        public int? OwnerId { get; set; }
        public int? SubmitterUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string OwnerName { get; set; }
        public string OwnerFamily { get; set; }
        public string SubmitterName { get; set; }
        public string SubmitterFamily { get; set; }
    }
}

