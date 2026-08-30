using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using Microsoft.AspNetCore.Mvc;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceSessions
{
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

        public IList<AttendanceSession> AttendanceSessions { get; set; }
            = new List<AttendanceSession>();

        public async Task OnGetAsync()
        {
            var lecturerId = _userManager.GetUserId(User);

            AttendanceSessions = await _context.AttendanceSessions
                .Include(a => a.Course)
                .Where(a => a.Course!.LecturerId == lecturerId)
                .OrderByDescending(a => a.SessionDate)
                .ToListAsync();

            AttendanceSessions = AttendanceSessions
                .OrderByDescending(a => a.SessionDate)
                .ThenBy(a => a.StartTime)
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var attendanceSession = await _context.AttendanceSessions
                .FindAsync(id);

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