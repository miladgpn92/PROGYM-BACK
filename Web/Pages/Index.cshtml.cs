using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Services.CMS.Setting;
using Services.Services.Site.GymLanding;
using SharedModels.Dtos;
using SharedModels.Dtos.Shared;

namespace Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ISettingService _settingService;
    private readonly IGymLandingService _gymLandingService;

    public IndexModel(ISettingService settingService, IGymLandingService gymLandingService)
    {
        _settingService = settingService;
        _gymLandingService = gymLandingService;
    }

    [BindProperty]
    public SettingSelectDto Setting { get; set; } = new();

    public List<GymLandingListItemDto> LatestGyms { get; private set; } = new();

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var resSetting = _settingService.GetSetting();
        if (resSetting.IsSuccess)
        {
            Setting = resSetting.Model;
        }

        LatestGyms = await _gymLandingService.GetLatestGymsAsync(12, cancellationToken);
    }
}
