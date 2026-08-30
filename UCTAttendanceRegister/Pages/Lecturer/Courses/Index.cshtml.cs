using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Courses;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IList<Course> Courses { get; set; } = new List<Course>();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);

        Courses = await _context.Courses
            .Include(c => c.Lecturer)
            .Where(c => c.LecturerId == lecturerId)
            .OrderBy(c => c.CourseCode)
            .ToListAsync();
    }
}