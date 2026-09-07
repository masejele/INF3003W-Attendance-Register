using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Lecturer.Attendance;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly Inf3003wCourseService _courseService;

    public IndexModel(
        ApplicationDbContext context,
        Inf3003wCourseService courseService)
    {
        _context = context;
        _courseService = courseService;
    }

    [BindProperty(SupportsGet = true)]
    public int? SessionId { get; set; }

    public IList<AttendanceSession> Sessions { get; private set; } = new List<AttendanceSession>();
    public AttendanceSession? SelectedSession { get; private set; }
    public IList<StudentAttendanceRow> AttendanceRows { get; private set; } = new List<StudentAttendanceRow>();
    public int TotalStudents => AttendanceRows.Count;
    public int PresentStudents => AttendanceRows.Count(row => row.IsPresent);
    public int AbsentStudents => TotalStudents - PresentStudents;
    public double AttendanceRate => TotalStudents == 0 ? 0 : PresentStudents * 100d / TotalStudents;

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int sessionId,
        string studentId,
        bool present)
    {
        var course = await _courseService.GetAsync();
        var session = course == null
            ? null
            : await _context.AttendanceSessions
                .FirstOrDefaultAsync(item => item.Id == sessionId && item.CourseId == course.Id);

        if (session == null)
        {
            return NotFound();
        }

        var studentExists = await _context.Users
            .AnyAsync(user => user.Id == studentId && user.StudentNumber != null);

        if (!studentExists)
        {
            return NotFound();
        }

        var records = await _context.AttendanceRecords
            .Where(record => record.AttendanceSessionId == sessionId && record.StudentId == studentId)
            .ToListAsync();

        if (present)
        {
            if (records.Count == 0)
            {
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    AttendanceSessionId = sessionId,
                    StudentId = studentId,
                    MarkedAt = DateTime.Now
                });
            }

            TempData["Success"] = "Attendance marked present successfully.";
        }
        else
        {
            if (records.Count > 0)
            {
                _context.AttendanceRecords.RemoveRange(records);
            }

            TempData["Success"] = "Attendance marked absent successfully.";
        }

        await _context.SaveChangesAsync();

        return RedirectToPage(new { sessionId });
    }

    private async Task LoadPageAsync()
    {
        var course = await _courseService.GetAsync();
        if (course == null)
        {
            return;
        }

        Sessions = await _context.AttendanceSessions
            .Where(session => session.CourseId == course.Id)
            .OrderByDescending(session => session.SessionDate)
            .ToListAsync();

        Sessions = Sessions
            .OrderByDescending(session => session.SessionDate)
            .ThenByDescending(session => session.StartTime)
            .ToList();

        if (Sessions.Count == 0)
        {
            return;
        }

        var selectedSessionId = SessionId ?? Sessions[0].Id;
        SelectedSession = Sessions.FirstOrDefault(session => session.Id == selectedSessionId);

        if (SelectedSession == null)
        {
            SessionId = Sessions[0].Id;
            SelectedSession = Sessions[0];
        }

        var students = await _context.StudentCourses
            .Where(enrollment => enrollment.CourseId == course.Id &&
                enrollment.Student != null &&
                enrollment.Student.StudentNumber != null &&
                enrollment.Student.StudentNumber != "")
            .Include(enrollment => enrollment.Student)
            .Select(enrollment => enrollment.Student!)
            .OrderBy(student => student.StudentNumber)
            .ToListAsync();

        var presentRecords = await _context.AttendanceRecords
            .Where(record => record.AttendanceSessionId == SelectedSession.Id)
            .Include(record => record.Student)
            .ToListAsync();

        var recordsByStudentId = presentRecords
            .GroupBy(record => record.StudentId)
            .ToDictionary(group => group.Key, group => group.First());

        AttendanceRows = students
            .Select(student => new StudentAttendanceRow
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                StudentNumber = student.StudentNumber!,
                IsPresent = recordsByStudentId.ContainsKey(student.Id),
                MarkedAt = recordsByStudentId.TryGetValue(student.Id, out var record)
                    ? record.MarkedAt
                    : null
            })
            .ToList();
    }

    public class StudentAttendanceRow
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public DateTime? MarkedAt { get; set; }
    }
}
