using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class UserProgramDateUpdateDto
    {
        [Required]
        public int UserProgramId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
