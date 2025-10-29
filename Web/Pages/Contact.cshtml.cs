using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Services.CMS.GlobalSetting;
using Services.Services.CMS.Setting;
using SharedModels.Dtos;

namespace Web.Pages;

[AllowAnonymous]
public class ContactModel : PageModel
{
    private readonly ISettingService _settingService;
    private readonly IGlobalSettingService _globalSettingService;

    public ContactModel(ISettingService settingService, IGlobalSettingService globalSettingService)
    {
        _settingService = settingService;
        _globalSettingService = globalSettingService;
    }

    public ContactDetails Details { get; private set; } = ContactDetails.Empty;

    public MapDetails Map { get; private set; } = MapDetails.Empty;

    public void OnGet()
    {
        var settingResponse = _settingService.GetSetting();
        var globalResponse = _globalSettingService.GetGlobalSetting();

        var setting = settingResponse.IsSuccess ? settingResponse.Model : null;
        var globalSetting = globalResponse.IsSuccess ? globalResponse.Model : null;

        Details = ContactDetails.Create(setting, globalSetting);
        Map = MapDetails.Create(setting);

        var title = "تماس با ما";
        ViewData["Title"] = title;

        if (setting is not null)
        {
            ViewData["Seo"] = new SEODto
            {
                SEOTitle = title,
                SEODesc = setting.AboutUs ?? title,
                SEOPic = setting.LogoUrl
            };
        }
    }

    public readonly record struct ContactLink(string Display, string Link);

    public sealed record ContactDetails(
        IReadOnlyList<ContactLink> MobileNumbers,
        IReadOnlyList<ContactLink> LandlineNumbers,
        string? Email,
        string? Address)
    {
        public static ContactDetails Empty { get; } = new(Array.Empty<ContactLink>(), Array.Empty<ContactLink>(), null, null);

        public bool HasPhone => MobileNumbers.Count > 0;
        public bool HasLandline => LandlineNumbers.Count > 0;
        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
        public bool HasAddress => !string.IsNullOrWhiteSpace(Address);

        public static ContactDetails Create(SettingSelectDto? setting, GetGlobalSettingDto? globalSetting)
        {
            var mobiles = BuildPhoneList(setting?.Phonenumber);
            var landlines = BuildPhoneList(setting?.Tell);

            string? email = null;
            if (!string.IsNullOrWhiteSpace(globalSetting?.EmailUsername))
            {
                email = globalSetting.EmailUsername.Trim();
            }

            var address = setting?.Address;

            return new ContactDetails(mobiles, landlines, email, address);
        }

        private static IReadOnlyList<ContactLink> BuildPhoneList(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<ContactLink>();
            }

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<ContactLink>();

            foreach (var number in SplitPhones(value))
            {
                var display = NormalizeDisplay(number);
                var link = BuildTelLink(number);

                if (string.IsNullOrWhiteSpace(display) || string.IsNullOrWhiteSpace(link))
                {
                    continue;
                }

                if (!unique.Add(link))
                {
                    continue;
                }

                list.Add(new ContactLink(display, link));
            }

            return list;
        }

        private static IEnumerable<string> SplitPhones(string value)
        {
            var separators = new[] { '\r', '\n', ',', ';', '/', '|', '،' };
            return value.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part));
        }

        private static string NormalizeDisplay(string value)
        {
            var trimmed = value.Trim();
            return string.IsNullOrWhiteSpace(trimmed)
                ? string.Empty
                : System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ");
        }

        private static string BuildTelLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            var hasPlus = false;

            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                {
                    builder.Append(ch);
                }
                else if (ch is '+' && builder.Length == 0 && !hasPlus)
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
    }

    public sealed record MapDetails(
        bool HasLocation,
        double? Latitude,
        double? Longitude,
        string? GoogleEmbedUrl,
        string? GoogleDirectionsUrl,
        string? NeshanDirectionsUrl)
    {
        public static MapDetails Empty { get; } = new(false, null, null, null, null, null);

        public static MapDetails Create(SettingSelectDto? setting)
        {
            if (setting is null)
            {
                return Empty;
            }

            if (!TryParseCoordinate(setting.Latitude, out var lat) ||
                !TryParseCoordinate(setting.Longitude, out var lng))
            {
                return new MapDetails(
                    false,
                    null,
                    null,
                    BuildGoogleEmbed(setting.Address),
                    BuildGoogleDirections(setting.Address),
                    BuildNeshanDirections(setting.Address));
            }

            return new MapDetails(
                true,
                lat,
                lng,
                BuildGoogleEmbed(lat, lng),
                BuildGoogleDirections(lat, lng),
                BuildNeshanDirections(lat, lng));
        }

        private static bool TryParseCoordinate(string? input, out double value)
        {
            return double.TryParse(
                input,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static string? BuildGoogleEmbed(double lat, double lng)
        {
            return $"https://maps.google.com/maps?q={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}&z=15&hl=fa&output=embed";
        }

        private static string? BuildGoogleEmbed(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var encoded = Uri.EscapeDataString(query);
            return $"https://maps.google.com/maps?q={encoded}&z=15&hl=fa&output=embed";
        }

        private static string? BuildGoogleDirections(double lat, double lng)
        {
            return $"https://www.google.com/maps/dir/?api=1&destination={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string? BuildGoogleDirections(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var encoded = Uri.EscapeDataString(query);
            return $"https://www.google.com/maps/dir/?api=1&destination={encoded}";
        }

        private static string? BuildNeshanDirections(double lat, double lng)
        {
            var latString = lat.ToString(CultureInfo.InvariantCulture);
            var lngString = lng.ToString(CultureInfo.InvariantCulture);
            return $"https://nshn.ir/?lat={latString}&lng={lngString}";
        }

        private static string? BuildNeshanDirections(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var encoded = Uri.EscapeDataString(query);
            return $"https://nshn.ir/search/{encoded}";
        }
    }
}
