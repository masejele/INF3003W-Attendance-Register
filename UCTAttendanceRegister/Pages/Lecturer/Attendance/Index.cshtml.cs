using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Attendance;

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

    public IList<StudentAttendanceRow> AttendanceRows { get; set; }
        = new List<StudentAttendanceRow>();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);

        AttendanceRows = await _context.AttendanceRecords
            .Include(r => r.AttendanceSession)
            .ThenInclude(s => s.Course)
            .Include(r => r.Student)
            .Where(r => r.AttendanceSession != null &&
                r.AttendanceSession.Course != null &&
                r.AttendanceSession.Course.LecturerId == lecturerId)
            .OrderByDescending(r => r.MarkedAt)
            .Select(r => new StudentAttendanceRow
            {
                StudentName = r.Student != null ? r.Student.FullName : "Unknown Student",
                StudentNumber = r.Student != null ? (r.Student.StudentNumber ?? "N/A") : "N/A",
                CourseCode = r.AttendanceSession!.Course!.CourseCode,
                CourseName = r.AttendanceSession.Course.CourseName,
                SessionDate = r.AttendanceSession.SessionDate,
                StartTime = r.AttendanceSession.StartTime,
                EndTime = r.AttendanceSession.EndTime,
                MarkedAt = r.MarkedAt,
                Status = "Present"
            })
            .ToListAsync();
    }

    public class StudentAttendanceRow
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime MarkedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}