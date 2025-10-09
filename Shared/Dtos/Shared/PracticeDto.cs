using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using ResourceLibrary;
using SharedModels.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string ThumbPicUrl { get; set; }

        [Display(Name = "ویدیو")]
        public string VideoUrl { get; set; }


        [Display(Name = "دسته بندی")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "RequierdMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        public int? PracticeCategoryId { get; set; }

        [Display(Name = "دسته بندی")]
        public string PracticeCategoryTitle { get; set; }

    }

    public class PracticeSelectDto : SimpleBaseDto<PracticeSelectDto, Practice>
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string ThumbPicUrl { get; set; }
        public string VideoUrl { get; set; }
        public int? PracticeCategoryId { get; set; }
        public string PracticeCategoryTitle { get; set; }
        public DateTime CreateDate { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }
        public override void CustomMappings(AutoMapper.IMappingExpression<Practice, PracticeSelectDto> mapping)
        {
            mapping.ForMember(d => d.PracticeCategoryTitle, opt => opt.MapFrom(s => s.PracticeCategory.Title));
            mapping.ForMember(d => d.ApplicationUserName, opt => opt.MapFrom(s => s.User.Name));
            mapping.ForMember(d => d.ApplicationUserFamily, opt => opt.MapFrom(s => s.User.Family));
        }
    }
}
