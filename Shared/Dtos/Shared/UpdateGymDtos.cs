using System.ComponentModel.DataAnnotations;

namespace SharedModels.Dtos.Shared
{
    public class UpdateGymBasicDto
    {
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string LogoUrl { get; set; }
    }

    public class UpdateGymAddressDto
    {
        [MaxLength(50)]
        public string ContactUsPhoneNumber { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class UpdateGymSocialDto
    {
        [MaxLength(500)]
        public string InstagramLink { get; set; }

        [MaxLength(500)]
        public string TelegramLink { get; set; }

        [MaxLength(500)]
        public string EitaaLink { get; set; }

        [MaxLength(500)]
        public string BaleLink { get; set; }
    }
}

