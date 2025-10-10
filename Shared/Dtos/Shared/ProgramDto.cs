using Common.Enums;
using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class ProgramDto : SimpleBaseDto<ProgramDto, Program>
    {
        [Display(Name = "Title")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }

        // CountOfPractice is computed from Practices; do not accept from client

        [Display(Name = "Type")]
        public ProgramTypes Type { get; set; }

        [Display(Name = "Owner")]
        public int? OwnerId { get; set; }
        // SubmitterUserId is taken from authenticated user; do not accept from client

        // Practices to attach on create
        public List<ProgramPracticeInputDto> Practices { get; set; }
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

        public override void CustomMappings(AutoMapper.IMappingExpression<Program, ProgramSelectDto> mapping)
        {
            mapping.ForMember(d => d.OwnerName, opt => opt.MapFrom(s => s.Owner.Name));
            mapping.ForMember(d => d.OwnerFamily, opt => opt.MapFrom(s => s.Owner.Family));
            mapping.ForMember(d => d.SubmitterName, opt => opt.MapFrom(s => s.SubmitterUser.Name));
            mapping.ForMember(d => d.SubmitterFamily, opt => opt.MapFrom(s => s.SubmitterUser.Family));
        }
    }
}
