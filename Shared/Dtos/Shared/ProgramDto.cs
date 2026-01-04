using Common.Enums;
using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedModels.Dtos.Shared
{
    public class ProgramDto : SimpleBaseDto<ProgramDto, Program>
    {
        [Display(Name = "Title")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }

        [Display(Name = "یادداشت")]
        [MaxLength(1000, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Note { get; set; }

        // CountOfPractice is computed from routine items; do not accept from client

        [Display(Name = "Type")]
        public ProgramTypes Type { get; set; }

        [Display(Name = "Owner")]
        public int? OwnerId { get; set; }

        public DateTime? OwnerStartDate { get; set; }
        public DateTime? OwnerEndDate { get; set; }
        public int? OwnerRepeatCount { get; set; }
        public List<int> PaperFileIds { get; set; } = new();
        public List<int>? CategoryIds { get; set; }
        // SubmitterUserId is taken from authenticated user; do not accept from client

        // Routine items (single movement or superset) to attach on create
        public List<ProgramRoutineItemInputDto> RoutineItems { get; set; }

        public override void CustomMappings(AutoMapper.IMappingExpression<Program, ProgramDto> mapping)
        {
            mapping.ForMember(d => d.RoutineItems, opt => opt.Ignore());
            mapping.ForMember(d => d.PaperFileIds, opt => opt.Ignore());
            mapping.ForMember(d => d.CategoryIds, opt => opt.Ignore());
            mapping.ReverseMap()
                   .ForMember(d => d.ProgramRoutineItems, opt => opt.Ignore())
                   .ForMember(d => d.PaperFiles, opt => opt.Ignore())
                   .ForMember(d => d.ProgramCategoryPrograms, opt => opt.Ignore());
        }
    }

    public class ProgramSelectDto : SimpleBaseDto<ProgramSelectDto, Program>
    {
        public int GymId { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public int? CountOfPractice { get; set; }
        public ProgramTypes Type { get; set; }
        public int? OwnerId { get; set; }
        public int? SubmitterUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string OwnerName { get; set; }
        public string OwnerFamily { get; set; }
        public string SubmitterName { get; set; }
        public string SubmitterFamily { get; set; }
        public List<int> PaperFileIds { get; set; } = new();
        public List<ProgramCategoryItemDto> Categories { get; set; } = new();

        public override void CustomMappings(AutoMapper.IMappingExpression<Program, ProgramSelectDto> mapping)
        {
            mapping.ForMember(d => d.OwnerName, opt => opt.MapFrom(s => s.Owner.Name));
            mapping.ForMember(d => d.OwnerFamily, opt => opt.MapFrom(s => s.Owner.Family));
            mapping.ForMember(d => d.SubmitterName, opt => opt.MapFrom(s => s.SubmitterUser.Name));
            mapping.ForMember(d => d.SubmitterFamily, opt => opt.MapFrom(s => s.SubmitterUser.Family));
            mapping.ForMember(d => d.PaperFileIds, opt => opt.MapFrom(s => s.PaperFiles
                .OrderBy(pf => pf.DisplayOrder)
                .Select(pf => pf.GymFileId)));
            mapping.ForMember(d => d.Categories, opt => opt.MapFrom(s => s.ProgramCategoryPrograms
                .Select(pcp => pcp.ProgramCategory)
                .OrderBy(pc => pc.Title)));
        }
    }
}
