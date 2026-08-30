using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceSessions
{
    [Authorize(Roles = "Lecturer")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public AttendanceSession AttendanceSession { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var lecturerId = _userManager.GetUserId(User);

            var session = await _context.AttendanceSessions
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Course!.LecturerId == lecturerId);

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

            var lecturerId = _userManager.GetUserId(User);

            var session = await _context.AttendanceSessions
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Course!.LecturerId == lecturerId);

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