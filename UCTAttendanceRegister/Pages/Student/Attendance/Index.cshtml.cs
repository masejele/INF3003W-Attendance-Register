using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Student.Attendance;

[Authorize(Roles = "Student")]
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

    public IList<AttendanceRecord> AttendanceRecords { get; set; }
        = new List<AttendanceRecord>();

    public async Task OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return;
        }

        AttendanceRecords = await _context.AttendanceRecords
            .Include(r => r.AttendanceSession)
            .ThenInclude(s => s.Course)
            .Where(r => r.StudentId == student.Id)
            .OrderByDescending(r => r.MarkedAt)
            .ToListAsync();
    }
}