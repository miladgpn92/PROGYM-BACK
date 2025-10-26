using System;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Common.Enums;
using Entities;
using SharedModels.CustomMapping;

namespace SharedModels.Dtos.Shared
{
    public class GymUserListItemDto : IHaveCustomMapping
    {
        public int GymId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string PhoneNumber { get; set; }
        public UsersRole Role { get; set; }
        public DateTime JoinDate { get; set; }

        public void CreateMappings(Profile profile)
        {
            profile.CreateMap<GymUser, GymUserListItemDto>()
                   .ForMember(d => d.Name, opt => opt.MapFrom(s => s.User.Name))
                   .ForMember(d => d.Family, opt => opt.MapFrom(s => s.User.Family))
                   .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.User.PhoneNumber));
        }
    }

    public class GymUserCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Family { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [EnumDataType(typeof(UsersRole))]
        public UsersRole Role { get; set; }
    }
}
