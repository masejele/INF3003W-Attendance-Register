using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Lecturer.Courses;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly Inf3003wCourseService _courseService;

    public IndexModel(Inf3003wCourseService courseService)
    {
        _courseService = courseService;
    }

    public IList<Course> Courses { get; set; } = new List<Course>();

    public async Task OnGetAsync()
    {
        var course = await _courseService.GetAsync();
        Courses = course == null
            ? new List<Course>()
            : new List<Course> { course };
    }
}