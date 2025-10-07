using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.DTCMS.Dev
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string? Name { get; set; }
        public void OnGet()
        {
        }
    }
}
