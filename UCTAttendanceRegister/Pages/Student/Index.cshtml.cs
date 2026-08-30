using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Student;

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

    public double OverallAttendancePercentage { get; set; }
    public int TotalSessionsAttended { get; set; }
    public int TotalSessionsHeld { get; set; }
    public string OverallStatus { get; set; } = "At Risk";
    public IList<CourseAttendanceInsight> CourseInsights { get; set; }
        = new List<CourseAttendanceInsight>();
    public IList<Course> EnrolledCourses { get; set; } = new List<Course>();

    public async Task OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return;
        }

        var enrolledCourseIds = await _context.StudentCourses
            .Where(sc => sc.StudentId == student.Id)
            .Select(sc => sc.CourseId)
            .ToListAsync();

        var courses = await _context.Courses
            .Where(c => enrolledCourseIds.Contains(c.Id))
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        EnrolledCourses = courses;

        var courseInsights = new List<CourseAttendanceInsight>();
        var overallSessionsAttended = 0;
        var overallSessionsHeld = 0;

        foreach (var course in courses)
        {
            var sessionIds = await _context.AttendanceSessions
                .Where(s => s.CourseId == course.Id)
                .Select(s => s.Id)
                .ToListAsync();

            var totalSessionsHeld = sessionIds.Count;
            var sessionsAttended = await _context.AttendanceRecords
                .CountAsync(r =>
                    r.StudentId == student.Id &&
                    sessionIds.Contains(r.AttendanceSessionId));

            var percentage = AttendanceInsightCalculator.CalculatePercentage(
                sessionsAttended,
                totalSessionsHeld);

            var forecast = AttendanceInsightCalculator.CalculateForecast(
                sessionsAttended,
                totalSessionsHeld);

            var insight = new CourseAttendanceInsight
            {
                CourseId = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                SessionsAttended = sessionsAttended,
                TotalSessionsHeld = totalSessionsHeld,
                AttendancePercentage = percentage,
                Status = AttendanceInsightCalculator.GetStatus(percentage),
                Forecast = forecast
            };

            courseInsights.Add(insight);

            overallSessionsAttended += sessionsAttended;
            overallSessionsHeld += totalSessionsHeld;
        }

        CourseInsights = courseInsights;
        TotalSessionsAttended = overallSessionsAttended;
        TotalSessionsHeld = overallSessionsHeld;
        OverallAttendancePercentage = AttendanceInsightCalculator.CalculatePercentage(
            overallSessionsAttended,
            overallSessionsHeld);
        OverallStatus = AttendanceInsightCalculator.GetStatus(
            OverallAttendancePercentage);
    }
}