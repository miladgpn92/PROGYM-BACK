using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Services.Site.GymLanding;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;

namespace Web.Pages.G
{
    public class IndexModel : PageModel
    {
        private readonly IGymLandingService _gymLandingService;

        public IndexModel(IGymLandingService gymLandingService)
        {
            _gymLandingService = gymLandingService;
        }

        public GymLandingDetailDto? Gym { get; private set; }

        public string ContactPhone { get; private set; } = string.Empty;

        public bool HasLocation { get; private set; }

        public string GoogleMapsUrl { get; private set; } = string.Empty;

        public string NeshanUrl { get; private set; } = string.Empty;

        public string AppDeepLink { get; private set; } = string.Empty;

        public bool HideChrome { get; private set; }

        public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            Gym = await _gymLandingService.GetGymBySlugAsync(slug, cancellationToken);
            if (Gym is null)
            {
                return NotFound();
            }

            ViewData["Title"] = Gym.Title;

            ContactPhone = !string.IsNullOrWhiteSpace(Gym.ContactUsPhoneNumber)
                ? Gym.ContactUsPhoneNumber
                : Gym.Phone ?? string.Empty;

            HasLocation = Math.Abs(Gym.Lat) > double.Epsilon || Math.Abs(Gym.Lng) > double.Epsilon;

            if (HasLocation)
            {
                var lat = Gym.Lat.ToString(CultureInfo.InvariantCulture);
                var lng = Gym.Lng.ToString(CultureInfo.InvariantCulture);
                GoogleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}";
                NeshanUrl = $"https://neshan.org/maps/places/{lat},{lng}";
            }
            else
            {
                var query = Uri.EscapeDataString(Gym.Address ?? Gym.Title ?? string.Empty);
                GoogleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={query}";
                NeshanUrl = $"https://neshan.org/search/{query}";
            }

            AppDeepLink = $"http://app.pro-gym.ir?gym={Uri.EscapeDataString(Gym.Slug)}";

            HideChrome = string.Equals(Request.Query["share"], "app", StringComparison.OrdinalIgnoreCase);
            if (HideChrome)
            {
                ViewData["HideHeaderFooter"] = true;
            }


            SEODto PageSeo = new SEODto()
            {
                SEOTitle = Gym.Title,
                SEODesc = Gym.Title,
                SEOPic =Gym.LogoUrl,
               
            };

            ViewData["Seo"] = PageSeo;



            return Page();
        }
    }
}
