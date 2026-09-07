using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Student;

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

        var fixedCourse = await _courseService.GetAsync();
        if (fixedCourse == null)
        {
            return;
        }

        var isEnrolled = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == student.Id && sc.CourseId == fixedCourse.Id);

        var courses = isEnrolled ? new List<Course> { fixedCourse } : new List<Course>();

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