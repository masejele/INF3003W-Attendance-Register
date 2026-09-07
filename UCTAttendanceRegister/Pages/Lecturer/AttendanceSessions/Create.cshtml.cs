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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly Inf3003wCourseService _courseService;

        public CreateModel(
            ApplicationDbContext context,
            Inf3003wCourseService courseService)
        {
            _context = context;
            _courseService = courseService;
        }

        [BindProperty]
        public AttendanceSession AttendanceSession { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var course = await _courseService.GetAsync();

            if (course == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The INF3003W course could not be found.");
            }

            if (AttendanceSession.EndTime <= AttendanceSession.StartTime)
            {
                ModelState.AddModelError(
                    "AttendanceSession.EndTime",
                    "End time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            AttendanceSession.CourseId = course!.Id;
            AttendanceSession.IsOpen = true;

            _context.AttendanceSessions.Add(AttendanceSession);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}