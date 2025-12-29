using AutoMapper;
using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using SharedModels.CustomMapping;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ProgramCategoryDto : SimpleBaseDto<ProgramCategoryDto, ProgramCategory>
    {
        [Display(Name = "Title")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }
    }

    public class ProgramCategorySelectDto : SimpleBaseDto<ProgramCategorySelectDto, ProgramCategory>
    {
        public int GymId { get; set; }
        public string Title { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }

        public override void CustomMappings(IMappingExpression<ProgramCategory, ProgramCategorySelectDto> mapping)
        {
            mapping.ForMember(d => d.ApplicationUserName, opt => opt.MapFrom(s => s.SubmitterUser.Name));
            mapping.ForMember(d => d.ApplicationUserFamily, opt => opt.MapFrom(s => s.SubmitterUser.Family));
        }
    }

    public class ProgramCategoryItemDto : IHaveCustomMapping
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public void CreateMappings(Profile profile)
        {
            profile.CreateMap<ProgramCategory, ProgramCategoryItemDto>();
        }
    }
}
