using Entities;
using ResourceLibrary.Resources.ErrorMsg;
using SharedModels.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class GymDto : BaseWithSeoDto<GymDto, Gym>
    {
        [Display(Name = "عنوان باشگاه")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = nameof(ErrorMsg.RequierdMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Title { get; set; }

        [Display(Name = "آدرس")]
        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Address { get; set; }

        [Display(Name = "آدرس تصویر لوگو")]
        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string LogoUrl { get; set; }

        [Display(Name = "نامک (Slug)")]
        [MaxLength(200, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Slug { get; set; }

        [MaxLength(50, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string ContactUsPhoneNumber { get; set; }

        [MaxLength(50, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string Phone { get; set; }

        public double Lat { get; set; }
        public double Lng { get; set; }

        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string InstagramLink { get; set; }

        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string TelegramLink { get; set; }

        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string EitaaLink { get; set; }

        [MaxLength(500, ErrorMessageResourceName = nameof(ErrorMsg.MaxLenMsg), ErrorMessageResourceType = typeof(ErrorMsg))]
        public string BaleLink { get; set; }
    }

    public class GymSelectDto : BaseWithSeoDto<GymSelectDto, Gym>
    {
        public string Title { get; set; }
        public string Address { get; set; }
        public string LogoUrl { get; set; }
        public string Slug { get; set; }
        public string ContactUsPhoneNumber { get; set; }
        public string Phone { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string InstagramLink { get; set; }
        public string TelegramLink { get; set; }
        public string EitaaLink { get; set; }
        public string BaleLink { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ApplicationUserName { get; set; }
        public string ApplicationUserFamily { get; set; }
    }
}
