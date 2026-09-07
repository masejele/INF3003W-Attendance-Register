using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceSessions
{
    [Authorize(Roles = "Lecturer")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly Inf3003wCourseService _courseService;

        public EditModel(
            ApplicationDbContext context,
            Inf3003wCourseService courseService)
        {
            _context = context;
            _courseService = courseService;
        }

        [BindProperty]
        public AttendanceSession AttendanceSession { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var course = await _courseService.GetAsync();

            var session = await _context.AttendanceSessions
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    course != null && a.CourseId == course.Id);

            if (session == null)
            {
                return NotFound();
            }

            AttendanceSession = session;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var course = await _courseService.GetAsync();

            var session = await _context.AttendanceSessions
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    course != null && a.CourseId == course.Id);

            if (session == null)
            {
                return NotFound();
            }

            session.SessionDate = AttendanceSession.SessionDate;
            session.StartTime = AttendanceSession.StartTime;
            session.EndTime = AttendanceSession.EndTime;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}