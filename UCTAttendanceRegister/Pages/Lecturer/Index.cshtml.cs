using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UCTAttendanceRegister.Pages.Lecturer;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}