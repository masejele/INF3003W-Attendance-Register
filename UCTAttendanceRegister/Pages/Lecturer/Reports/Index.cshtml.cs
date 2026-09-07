using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Lecturer.Reports;

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
    public int TotalStudents { get; private set; }
    public int PresentStudents { get; private set; }
    public int AbsentStudents => TotalStudents - PresentStudents;
    public double AttendanceRate => TotalStudents == 0 ? 0 : PresentStudents * 100d / TotalStudents;
    public int TotalStudentsEnrolled { get; private set; }
    public int OpenSessions { get; private set; }
    public int StudentsAtRisk { get; private set; }
    public double AverageAttendanceAcrossCourses { get; private set; }
    public List<SystemFeatureSummary> Features { get; private set; } = new();

    public async Task OnGetAsync()
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

        TotalStudentsEnrolled = await _context.StudentCourses
            .CountAsync(enrollment => enrollment.CourseId == course.Id);
        OpenSessions = await _context.AttendanceSessions
            .CountAsync(session => session.CourseId == course.Id && session.IsOpen);

        if (Sessions.Count > 0)
        {
            SelectedSession = Sessions.FirstOrDefault(session => session.Id == SessionId)
                ?? Sessions[0];
            SessionId = SelectedSession.Id;

            var rosterStudentIds = await _context.StudentCourses
                .Where(enrollment => enrollment.CourseId == course.Id)
                .Select(enrollment => enrollment.StudentId)
                .Distinct()
                .ToListAsync();

            TotalStudents = rosterStudentIds.Count;
            PresentStudents = await _context.AttendanceRecords
                .CountAsync(record =>
                    record.AttendanceSessionId == SelectedSession.Id &&
                    rosterStudentIds.Contains(record.StudentId));
        }

        Features = new List<SystemFeatureSummary>
        {
            new() { Title = "Course Enrollment", Description = "Students are enrolled in the fixed INF3003W course." },
            new() { Title = "Attendance Tracking", Description = "Lecturers create sessions and students mark attendance in real time." },
            new() { Title = "Performance Insights", Description = "The dashboard highlights attendance performance and students needing attention." },
            new() { Title = "Reporting", Description = "Attendance is shown per INF3003W lecture session." }
        };
    }

    public class SystemFeatureSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
