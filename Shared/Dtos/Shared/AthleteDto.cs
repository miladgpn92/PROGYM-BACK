using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common.Enums;

namespace SharedModels.Dtos.Shared
{
    public class AthleteCreateDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Family { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public Gender Gender { get; set; }
    }

    public class AthleteUpdateDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Family { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public Gender Gender { get; set; }
    }

    public class AthleteSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string PhoneNumber { get; set; }
        public Gender? Gender { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public class AthleteDetailDto : AthleteSelectDto
    {
        public List<AthleteDataDto> AthleteData { get; set; } = new();
        public List<UserProgramBriefDto> UserPrograms { get; set; } = new();
    }

    public class AthleteDataDto
    {
        public int Id { get; set; }
        public DateTime SubmitDate { get; set; }
        public int Height { get; set; }
        public int Age { get; set; }
        public decimal Weight { get; set; }
    }

    public class UserProgramBriefDto
    {
        public int Id { get; set; }
        public int ProgramId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ProgramTitle { get; set; }
    }
}
