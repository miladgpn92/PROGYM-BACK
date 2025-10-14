using AutoMapper;
using Common.Enums;
using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using SharedModels.CustomMapping;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedModels.Dtos.Shared
{
    public class PracticeDto : SimpleBaseDto<PracticeDto, Practice>
    {
        [Display(Name = "Name")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "RequierdMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = "MaxLenMsg", ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Desc { get; set; }

        public IList<PracticeMediaRequestDto> Images { get; set; } = new List<PracticeMediaRequestDto>();

        public IList<PracticeMediaRequestDto> Videos { get; set; } = new List<PracticeMediaRequestDto>();

        [Display(Name = "Practice Category")]
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
        public IList<PracticeMediaSelectDto> Images { get; set; } = new List<PracticeMediaSelectDto>();
        public IList<PracticeMediaSelectDto> Videos { get; set; } = new List<PracticeMediaSelectDto>();

        public override void CustomMappings(IMappingExpression<Practice, PracticeSelectDto> mapping)
        {
            mapping.ForMember(d => d.PracticeCategoryTitle, opt => opt.MapFrom(s => s.PracticeCategory.Title));
            mapping.ForMember(d => d.ApplicationUserName, opt => opt.MapFrom(s => s.User.Name));
            mapping.ForMember(d => d.ApplicationUserFamily, opt => opt.MapFrom(s => s.User.Family));
            mapping.ForMember(d => d.Images, opt => opt.MapFrom(s => s.MediaItems.Where(m => m.MediaType == MediaFileType.Image).OrderBy(m => m.DisplayOrder)));
            mapping.ForMember(d => d.Videos, opt => opt.MapFrom(s => s.MediaItems.Where(m => m.MediaType == MediaFileType.Video).OrderBy(m => m.DisplayOrder)));
        }
    }

    public class PracticeMediaRequestDto
    {
        public int? Id { get; set; }

        [Required]
        public int GymFileId { get; set; }

        public int Order { get; set; }
    }

    public class PracticeMediaSelectDto : IHaveCustomMapping
    {
        public int Id { get; set; }
        public int GymFileId { get; set; }
        public int Order { get; set; }
        public string RelativePath { get; set; }
        public MediaFileType MediaType { get; set; }

        public void CreateMappings(Profile profile)
        {
            profile.CreateMap<PracticeMedia, PracticeMediaSelectDto>()
                   .ForMember(d => d.Order, opt => opt.MapFrom(s => s.DisplayOrder))
                   .ForMember(d => d.RelativePath, opt => opt.MapFrom(s => s.GymFile != null ? s.GymFile.RelativePath : null));
        }
    }
}
