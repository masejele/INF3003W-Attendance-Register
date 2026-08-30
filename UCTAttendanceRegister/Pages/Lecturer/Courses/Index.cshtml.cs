using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Courses;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Course> Courses { get; set; } = new List<Course>();

    public async Task OnGetAsync()
    {
        Courses = await _context.Courses
            .Include(c => c.Lecturer)
            .ToListAsync();
    }
}