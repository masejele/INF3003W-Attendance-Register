using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UCTAttendanceRegister.Pages.Student;

[Authorize(Roles = "Student")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}