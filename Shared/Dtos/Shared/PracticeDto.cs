using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class PracticeDto : SimpleBaseDto<PracticeDto, Practice>
    {
       [Display(Name = " عنوان تمرین")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "RequierdMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = "MaxLenMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Name { get; set; }

          [Display(Name = "توضیحات")]
        public string Desc { get; set; }

           [Display(Name = "تصویر ")]
        public int? ThumbGymFileId { get; set; }

         [Display(Name = "ویدیو")]
        public int? VideoGymFileId { get; set; }

       [Display(Name = "دسته بندی")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "RequierdMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? PracticeCategoryId { get; set; }

      
    }

    public class PracticeSelectDto : SimpleBaseDto<PracticeSelectDto, Practice>
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int? PracticeCategoryId { get; set; }
        public string PracticeCategoryTitle { get; set; }
        public DateTime CreateDate { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }

        public int? ThumbGymFileId { get; set; }
        public string ThumbRelativePath { get; set; }

        public int? VideoGymFileId { get; set; }
        public string VideoRelativePath { get; set; }

        public override void CustomMappings(AutoMapper.IMappingExpression<Practice, PracticeSelectDto> mapping)
        {
            mapping.ForMember(d => d.PracticeCategoryTitle, opt => opt.MapFrom(s => s.PracticeCategory.Title));
            mapping.ForMember(d => d.ApplicationUserName, opt => opt.MapFrom(s => s.User.Name));
            mapping.ForMember(d => d.ApplicationUserFamily, opt => opt.MapFrom(s => s.User.Family));
            mapping.ForMember(d => d.ThumbGymFileId, opt => opt.MapFrom(s => s.ThumbFileId));
            mapping.ForMember(d => d.ThumbRelativePath, opt => opt.MapFrom(s => s.ThumbFile != null ? s.ThumbFile.RelativePath : null));
            mapping.ForMember(d => d.VideoGymFileId, opt => opt.MapFrom(s => s.VideoFileId));
            mapping.ForMember(d => d.VideoRelativePath, opt => opt.MapFrom(s => s.VideoFile != null ? s.VideoFile.RelativePath : null));
        }
    }
}
