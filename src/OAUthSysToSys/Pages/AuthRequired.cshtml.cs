using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace OAUthSysToSys.Pages
{
    [Authorize]
    public class AuthRequiredModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}