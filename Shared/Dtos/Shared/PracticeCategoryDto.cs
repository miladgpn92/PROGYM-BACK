using Entities;
using AutoMapper;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class PracticeCategoryDto : SimpleBaseDto<PracticeCategoryDto, PracticeCategory>
    {
        [Display(Name = "عنوان دسته تمرین")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }
    }

    public class PracticeCategorySelectDto : SimpleBaseDto<PracticeCategorySelectDto, PracticeCategory>
    {
        public string Title { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }

        public override void CustomMappings(IMappingExpression<PracticeCategory, PracticeCategorySelectDto> mapping)
        {
            mapping.ForMember(d => d.ApplicationUserName, opt => opt.MapFrom(s => s.SubmitterUser.Name));
            mapping.ForMember(d => d.ApplicationUserFamily, opt => opt.MapFrom(s => s.SubmitterUser.Family));
        }
    }
}
