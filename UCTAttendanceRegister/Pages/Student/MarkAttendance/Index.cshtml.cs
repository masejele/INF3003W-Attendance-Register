using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Student.MarkAttendance;

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

    public IList<AttendanceSession> Sessions { get; set; }
        = new List<AttendanceSession>();


    public async Task OnGetAsync()
    {
        Sessions = (await _context.AttendanceSessions
            .Include(s => s.Course)
            .Where(s => s.IsOpen)
            .ToListAsync())
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.StartTime)
            .ToList();
    }


    public async Task<IActionResult> OnPostAsync(int SessionId)
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return Challenge();
        }


        var session = await _context.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == SessionId);


        if (session == null)
        {
            return NotFound();
        }


        if (!session.IsOpen)
        {
            TempData["Error"] = "This attendance session is closed.";

            return RedirectToPage();
        }


        // Check whether the student has already marked attendance
        var alreadyMarked = await _context.AttendanceRecords
            .AnyAsync(r =>
                r.AttendanceSessionId == SessionId &&
                r.StudentId == student.Id);


        if (alreadyMarked)
        {
            TempData["Error"] =
                "You have already marked attendance for this session.";

            return RedirectToPage();
        }


        // Create attendance record
        var record = new AttendanceRecord
        {
            AttendanceSessionId = SessionId,
            StudentId = student.Id,
            MarkedAt = DateTime.Now
        };


        _context.AttendanceRecords.Add(record);

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Your attendance has been successfully marked!";


        return RedirectToPage();
    }
}