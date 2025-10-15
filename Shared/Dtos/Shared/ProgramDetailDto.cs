using Common.Enums;
using Entities;
using SharedModels.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedModels.Dtos.Shared
{
    public class ProgramPracticeSelectDto : SimpleBaseDto<ProgramPracticeSelectDto, ProgramPractice>
    {
        public int? PracticeId { get; set; }
        public PracticeType Type { get; set; }
        public int? SetCount { get; set; }
        public int? MovementCount { get; set; }
        public int? Duration { get; set; }
        public int? Rest { get; set; }
        public string PracticeName { get; set; }
        public IList<PracticeMediaSelectDto> Images { get; set; } = new List<PracticeMediaSelectDto>();
        public IList<PracticeMediaSelectDto> Videos { get; set; } = new List<PracticeMediaSelectDto>();

        public override void CustomMappings(AutoMapper.IMappingExpression<ProgramPractice, ProgramPracticeSelectDto> mapping)
        {
            mapping.ForMember(d => d.PracticeName, opt => opt.MapFrom(s => s.Practice.Name));
            mapping.ForMember(d => d.Images, opt => opt.MapFrom(s => s.Practice.MediaItems
                .Where(m => m.MediaType == MediaFileType.Image)
                .OrderBy(m => m.DisplayOrder)));
            mapping.ForMember(d => d.Videos, opt => opt.MapFrom(s => s.Practice.MediaItems
                .Where(m => m.MediaType == MediaFileType.Video)
                .OrderBy(m => m.DisplayOrder)));
        }
    }

    public class ProgramDetailDto : SimpleBaseDto<ProgramDetailDto, Program>
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
        public List<ProgramPracticeSelectDto> Practices { get; set; }

        public override void CustomMappings(AutoMapper.IMappingExpression<Program, ProgramDetailDto> mapping)
        {
            mapping.ForMember(d => d.OwnerName, opt => opt.MapFrom(s => s.Owner.Name));
            mapping.ForMember(d => d.OwnerFamily, opt => opt.MapFrom(s => s.Owner.Family));
            mapping.ForMember(d => d.SubmitterName, opt => opt.MapFrom(s => s.SubmitterUser.Name));
            mapping.ForMember(d => d.SubmitterFamily, opt => opt.MapFrom(s => s.SubmitterUser.Family));
            mapping.ForMember(d => d.Practices, opt => opt.MapFrom(s => s.ProgramPractices));
        }
    }
}
