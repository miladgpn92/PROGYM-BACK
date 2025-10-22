using Common.Enums;
using Entities;
using SharedModels.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedModels.Dtos.Shared
{
    public class ProgramRoutineItemInputDto
    {
        [Required]
        public ProgramRoutineItemType ItemType { get; set; }

        public int? DisplayOrder { get; set; }
        public string Title { get; set; }
        public int? RepeatCount { get; set; }
        public int? RestBetweenRepeats { get; set; }
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<ProgramPracticeInputDto> Practices { get; set; } = new List<ProgramPracticeInputDto>();
    }

    public class ProgramRoutineItemSelectDto : SimpleBaseDto<ProgramRoutineItemSelectDto, ProgramRoutineItem>
    {
        public ProgramRoutineItemType ItemType { get; set; }
        public int DisplayOrder { get; set; }
        public string Title { get; set; }
        public int? RepeatCount { get; set; }
        public int? RestBetweenRepeats { get; set; }
        public string Notes { get; set; }
        public List<ProgramPracticeSelectDto> Practices { get; set; } = new List<ProgramPracticeSelectDto>();

        public override void CustomMappings(AutoMapper.IMappingExpression<ProgramRoutineItem, ProgramRoutineItemSelectDto> mapping)
        {
            mapping.ForMember(d => d.Practices, opt => opt.MapFrom(s => s.ProgramPractices.OrderBy(pp => pp.InternalOrder)));
        }
    }
}

