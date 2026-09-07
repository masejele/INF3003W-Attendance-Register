using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;
using Microsoft.AspNetCore.Mvc;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceSessions
{
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

        public IList<AttendanceSession> AttendanceSessions { get; set; }
            = new List<AttendanceSession>();

        public async Task OnGetAsync()
        {
            var course = await _courseService.GetAsync();

            AttendanceSessions = await _context.AttendanceSessions
                .Include(a => a.Course)
                .Where(a => course != null && a.CourseId == course.Id)
                .OrderByDescending(a => a.SessionDate)
                .ToListAsync();

            AttendanceSessions = AttendanceSessions
                .OrderByDescending(a => a.SessionDate)
                .ThenBy(a => a.StartTime)
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var course = await _courseService.GetAsync();
            var attendanceSession = await _context.AttendanceSessions
                .FirstOrDefaultAsync(session => session.Id == id &&
                    course != null && session.CourseId == course.Id);

            if (attendanceSession == null)
            {
                return NotFound();
            }

            _context.AttendanceSessions.Remove(attendanceSession);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}