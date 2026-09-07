using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Student.Attendance;

[Authorize(Roles = "Student")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Inf3003wCourseService _courseService;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        Inf3003wCourseService courseService)
    {
        _context = context;
        _userManager = userManager;
        _courseService = courseService;
    }

    public IList<StudentAttendanceRow> AttendanceRows { get; private set; } = new List<StudentAttendanceRow>();

    public async Task OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        var course = await _courseService.GetAsync();

        if (student == null || course == null)
        {
            return;
        }

        var isEnrolled = await _context.StudentCourses
            .AnyAsync(enrollment =>
                enrollment.StudentId == student.Id &&
                enrollment.CourseId == course.Id);

        if (!isEnrolled)
        {
            return;
        }

        var sessions = await _context.AttendanceSessions
            .Where(session => session.CourseId == course.Id)
            .OrderByDescending(session => session.SessionDate)
            .ToListAsync();

        sessions = sessions
            .OrderByDescending(session => session.SessionDate)
            .ThenByDescending(session => session.StartTime)
            .ToList();

        var sessionIds = sessions.Select(session => session.Id).ToList();
        var records = await _context.AttendanceRecords
            .Where(record =>
                record.StudentId == student.Id &&
                sessionIds.Contains(record.AttendanceSessionId))
            .ToListAsync();

        var recordsBySessionId = records
            .GroupBy(record => record.AttendanceSessionId)
            .ToDictionary(group => group.Key, group => group.First());

        AttendanceRows = sessions
            .Select(session => new StudentAttendanceRow
            {
                CourseCode = course.CourseCode,
                SessionDate = session.SessionDate,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                IsPresent = recordsBySessionId.ContainsKey(session.Id),
                MarkedAt = recordsBySessionId.TryGetValue(session.Id, out var record)
                    ? record.MarkedAt
                    : null
            })
            .ToList();
    }

    public class StudentAttendanceRow
    {
        public string CourseCode { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsPresent { get; set; }
        public DateTime? MarkedAt { get; set; }
    }
}
