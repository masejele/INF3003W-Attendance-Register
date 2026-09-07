using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Student.MarkAttendance;

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

    public IList<AttendanceSession> Sessions { get; set; }
        = new List<AttendanceSession>();


    public async Task OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return;
        }

        var course = await _courseService.GetAsync();
        if (course == null)
        {
            return;
        }

        Sessions = (await _context.AttendanceSessions
            .Include(s => s.Course)
            .Where(s => s.IsOpen && s.CourseId == course.Id &&
                _context.StudentCourses.Any(sc => sc.StudentId == student.Id && sc.CourseId == course.Id))
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

        var course = await _courseService.GetAsync();
        if (course == null)
        {
            return NotFound();
        }


        var session = await _context.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == SessionId);


        if (session == null)
        {
            return NotFound();
        }

        var isEnrolled = session.CourseId == course.Id && await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == student.Id && sc.CourseId == course.Id);

        if (!isEnrolled)
        {
            TempData["Error"] = "You must join this course before marking attendance.";
            return RedirectToPage();
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