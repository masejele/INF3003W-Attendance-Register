using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Student.JoinCourse;

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

    public async Task OnGetAsync()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return Challenge();
        }

        var course = await _courseService.GetAsync();

        if (course == null)
        {
            TempData["Error"] = "The INF3003W course could not be found.";
            return Page();
        }

        var alreadyJoined = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == student.Id && sc.CourseId == course.Id);

        if (alreadyJoined)
        {
            TempData["Error"] = "You are already enrolled in INF3003W.";
            return Page();
        }

        _context.StudentCourses.Add(new StudentCourse
        {
            StudentId = student.Id,
            CourseId = course.Id,
            JoinedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"You have successfully joined {course.CourseCode} - {course.CourseName}.";

        return RedirectToPage("/Student/Index");
    }

}
