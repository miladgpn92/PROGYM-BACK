using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Services.Site.GymLanding;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;
using System.Text;

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

        public IReadOnlyList<ContactPhoneEntry> ContactPhones { get; private set; } = Array.Empty<ContactPhoneEntry>();

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

            ContactPhones = BuildContactPhones(Gym);
            ContactPhone = ContactPhones.FirstOrDefault()?.Display ?? string.Empty;

            HasLocation = Math.Abs(Gym.Lat) > double.Epsilon || Math.Abs(Gym.Lng) > double.Epsilon;

            if (HasLocation)
            {
                var lat = Gym.Lat.ToString(CultureInfo.InvariantCulture);
                var lng = Gym.Lng.ToString(CultureInfo.InvariantCulture);
                GoogleMapsUrl = $"https://www.google.com/maps?saddr=Current+Location&daddr=@{lat},{lng}";
                NeshanUrl = $"https://nshn.ir/?lat={lat}&lng={lng}";
            }
            else
            {
                var query = Uri.EscapeDataString(Gym.Address ?? Gym.Title ?? string.Empty);
                GoogleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={query}";
                NeshanUrl = $"https://nshn.ir/search/{query}";
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

        private static IReadOnlyList<ContactPhoneEntry> BuildContactPhones(GymLandingDetailDto gym)
        {
            var numbers = new List<ContactPhoneEntry>();
            foreach (var raw in EnumeratePhoneCandidates(gym))
            {
                var display = NormalizeDisplay(raw);
                if (string.IsNullOrWhiteSpace(display))
                {
                    continue;
                }

                var link = BuildTelLink(raw);
                if (string.IsNullOrWhiteSpace(link))
                {
                    continue;
                }

                if (numbers.Any(n => string.Equals(n.Link, link, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                numbers.Add(new ContactPhoneEntry(display, link));
            }

            return numbers;
        }

        private static IEnumerable<string> EnumeratePhoneCandidates(GymLandingDetailDto gym)
        {
            if (!string.IsNullOrWhiteSpace(gym.ContactUsPhoneNumber))
            {
                foreach (var number in SplitPhones(gym.ContactUsPhoneNumber))
                {
                    yield return number;
                }
            }

            if (!string.IsNullOrWhiteSpace(gym.Phone))
            {
                foreach (var number in SplitPhones(gym.Phone))
                {
                    yield return number;
                }
            }
        }

        private static IEnumerable<string> SplitPhones(string value)
        {
            var separators = new[] { '\r', '\n', ',', '،', ';', '؛', '/', '|' };
            return value.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim());
        }

        private static string NormalizeDisplay(string value)
        {
            var trimmed = value.Trim();
            return string.IsNullOrWhiteSpace(trimmed)
                ? string.Empty
                : Regex.Replace(trimmed, @"\s+", " ");
        }

        private static string BuildTelLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var hasPlus = false;

            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                {
                    builder.Append(ch);
                }
                else if (ch == '+' && builder.Length == 0 && !hasPlus)
                {
                    hasPlus = true;
                    builder.Append(ch);
                }
            }

            if (builder.Length == 0 || (hasPlus && builder.Length == 1))
            {
                return string.Empty;
            }

            return $"tel:{builder}";
        }

        public record ContactPhoneEntry(string Display, string Link);
    }
}
