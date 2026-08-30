using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer;

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

    public IList<CoursePerformanceSummary> CourseSummaries { get; set; }
        = new List<CoursePerformanceSummary>();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);

        var courses = await _context.Courses
            .Where(c => c.LecturerId == lecturerId)
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        var summaries = new List<CoursePerformanceSummary>();

        foreach (var course in courses)
        {
            var sessionIds = await _context.AttendanceSessions
                .Where(s => s.CourseId == course.Id)
                .Select(s => s.Id)
                .ToListAsync();

            var studentIds = await _context.AttendanceRecords
                .Where(r => sessionIds.Contains(r.AttendanceSessionId))
                .Select(r => r.StudentId)
                .Distinct()
                .ToListAsync();

            var studentUserMap = await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var studentMetrics = new List<StudentCourseAttendanceRisk>();
            var goodStandingCount = 0;
            var needsAttentionCount = 0;
            var atRiskCount = 0;

            foreach (var studentId in studentIds)
            {
                var attended = await _context.AttendanceRecords
                    .CountAsync(r =>
                        r.StudentId == studentId &&
                        sessionIds.Contains(r.AttendanceSessionId));

                var totalSessions = sessionIds.Count;
                var percentage = AttendanceInsightCalculator.CalculatePercentage(
                    attended,
                    totalSessions);

                var status = AttendanceInsightCalculator.GetStatus(percentage);

                if (status == "Good Standing")
                {
                    goodStandingCount++;
                }
                else if (status == "Needs Attention")
                {
                    needsAttentionCount++;
                }
                else
                {
                    atRiskCount++;
                }

                studentMetrics.Add(new StudentCourseAttendanceRisk
                {
                    StudentName = studentUserMap.TryGetValue(studentId, out var user)
                        ? user.FullName
                        : "Unknown Student",
                    CourseCode = course.CourseCode,
                    AttendancePercentage = percentage,
                    Status = status
                });
            }

            var averageAttendance = studentMetrics.Count > 0
                ? studentMetrics.Average(x => x.AttendancePercentage)
                : 0d;

            summaries.Add(new CoursePerformanceSummary
            {
                CourseId = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                AverageAttendancePercentage = averageAttendance,
                StudentCount = studentMetrics.Count,
                GoodStandingCount = goodStandingCount,
                NeedsAttentionCount = needsAttentionCount,
                AtRiskCount = atRiskCount,
                StudentsBelowTarget = studentMetrics
                    .Where(x => x.AttendancePercentage < AttendanceInsightCalculator.GoodStandingThreshold)
                    .OrderByDescending(x => x.AttendancePercentage)
                    .ToList()
            });
        }

        CourseSummaries = summaries;
    }
}