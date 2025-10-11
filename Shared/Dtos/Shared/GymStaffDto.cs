using System.ComponentModel.DataAnnotations;
using Common.Enums;

namespace SharedModels.Dtos.Shared
{
    public class GymStaffCreateDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Family { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        [EnumDataType(typeof(UsersRole))]
        public UsersRole Role { get; set; }
    }

    public class GymStaffUpdateDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Family { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        [EnumDataType(typeof(UsersRole))]
        public UsersRole Role { get; set; }
    }

    public class GymStaffSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string PhoneNumber { get; set; }
        public Gender? Gender { get; set; }
        public UsersRole Role { get; set; }
    }
}

