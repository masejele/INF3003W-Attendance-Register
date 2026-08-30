using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Reports;

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

    public int TotalCourses { get; set; }
    public int TotalStudentsEnrolled { get; set; }
    public int OpenSessions { get; set; }
    public int StudentsAtRisk { get; set; }
    public double AverageAttendanceAcrossCourses { get; set; }
    public List<SystemFeatureSummary> Features { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);

        var courses = await _context.Courses
            .Where(c => c.LecturerId == lecturerId)
            .ToListAsync();

        var courseIds = courses.Select(c => c.Id).ToList();

        TotalCourses = courses.Count;
        OpenSessions = await _context.AttendanceSessions
            .CountAsync(s => courseIds.Contains(s.CourseId) && s.IsOpen);

        TotalStudentsEnrolled = await _context.StudentCourses
            .CountAsync(sc => courseIds.Contains(sc.CourseId));

        var attendanceSummaries = new List<double>();
        foreach (var course in courses)
        {
            var sessionIds = await _context.AttendanceSessions
                .Where(s => s.CourseId == course.Id)
                .Select(s => s.Id)
                .ToListAsync();

            if (sessionIds.Count == 0)
            {
                continue;
            }

            var totalEnrollments = await _context.StudentCourses
                .CountAsync(sc => sc.CourseId == course.Id);

            if (totalEnrollments == 0)
            {
                continue;
            }

            var validStudentIds = await _context.StudentCourses
                .Where(sc => sc.CourseId == course.Id)
                .Select(sc => sc.StudentId)
                .Distinct()
                .ToListAsync();

            var totalAttendance = 0d;
            foreach (var studentId in validStudentIds)
            {
                var attended = await _context.AttendanceRecords
                    .CountAsync(r => r.StudentId == studentId && sessionIds.Contains(r.AttendanceSessionId));

                totalAttendance += AttendanceInsightCalculator.CalculatePercentage(attended, sessionIds.Count);
            }

            attendanceSummaries.Add(totalAttendance / validStudentIds.Count);
        }

        AverageAttendanceAcrossCourses = attendanceSummaries.Count > 0
            ? Math.Round(attendanceSummaries.Average(), 1)
            : 0d;

        StudentsAtRisk = await _context.StudentCourses
            .Where(sc => courseIds.Contains(sc.CourseId))
            .Select(sc => sc.StudentId)
            .Distinct()
            .CountAsync(studentId =>
                _context.AttendanceRecords
                    .Where(r => r.StudentId == studentId)
                    .Join(_context.AttendanceSessions
                        .Where(s => courseIds.Contains(s.CourseId)),
                        record => record.AttendanceSessionId,
                        session => session.Id,
                        (record, session) => new { record, session })
                    .GroupBy(x => x.session.CourseId)
                    .Select(g => new
                    {
                        CourseId = g.Key,
                        Attended = g.Count(x => x.record.StudentId == studentId),
                        Total = g.Count()
                    })
                    .Any(x => (x.Attended * 100d / Math.Max(x.Total, 1)) < AttendanceInsightCalculator.NeedsAttentionThreshold));

        Features = new List<SystemFeatureSummary>
        {
            new() { Title = "Course Enrollment", Description = "Students can join available courses through a dropdown-based enrollment flow." },
            new() { Title = "Attendance Tracking", Description = "Lecturers create sessions and students mark attendance in real time." },
            new() { Title = "Performance Insights", Description = "The dashboard highlights good standing, needs attention, and at-risk students." },
            new() { Title = "Reporting", Description = "Attendance data is summarized for quick academic review and intervention planning." }
        };
    }

    public class SystemFeatureSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}