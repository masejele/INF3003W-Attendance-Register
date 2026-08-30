using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceSessions
{
    [Authorize(Roles = "Lecturer")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public AttendanceSession AttendanceSession { get; set; } = new();

        public SelectList? CourseList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCoursesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var lecturerId = _userManager.GetUserId(User);

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == AttendanceSession.CourseId &&
                    c.LecturerId == lecturerId);

            if (course == null)
            {
                ModelState.AddModelError(
                    "AttendanceSession.CourseId",
                    "Please select one of your courses.");
            }

            if (AttendanceSession.EndTime <= AttendanceSession.StartTime)
            {
                ModelState.AddModelError(
                    "AttendanceSession.EndTime",
                    "End time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCoursesAsync();
                return Page();
            }

            AttendanceSession.CourseId = course!.Id;
            AttendanceSession.IsOpen = true;

            _context.AttendanceSessions.Add(AttendanceSession);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadCoursesAsync()
        {
            var lecturerId = _userManager.GetUserId(User);

            var courses = await _context.Courses
                .Where(c => c.LecturerId == lecturerId)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            CourseList = new SelectList(
                courses,
                "Id",
                "CourseCode");
        }
    }
}