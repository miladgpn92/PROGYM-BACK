using AutoMapper;
using Common.Consts;
using Common.Enums;
using Entities;
using SharedModels.CustomMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Dtos.Shared
{
    public class UserGymDto
    {
        public int GymId { get; set; }
        public int UserId { get; set; }
        public UsersRole Role { get; set; }
        public string GymTitle { get; set; }
    }
    public class UserGymDtoMapping : IHaveCustomMapping
    {
        public void CreateMappings(Profile profile)
        {
            profile.CreateMap<UserGymDto, GymUser>();
            profile.CreateMap<GymUser, UserGymDto>();
       
        }
    }
}
