using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Student.JoinCourse;

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

    [BindProperty]
    [Required]
    [Display(Name = "Available Courses")]
    public int CourseId { get; set; }

    public List<SelectListItem> AvailableCourses { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAvailableCourses();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableCourses();
            return Page();
        }

        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return Challenge();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == CourseId);

        if (course == null)
        {
            TempData["Error"] = "Please select a valid course from the list.";
            await LoadAvailableCourses();
            return Page();
        }

        var alreadyJoined = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == student.Id && sc.CourseId == course.Id);

        if (alreadyJoined)
        {
            TempData["Error"] = "You are already enrolled in this course.";
            await LoadAvailableCourses();
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

    private async Task LoadAvailableCourses()
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            AvailableCourses = new List<SelectListItem>();
            return;
        }

        var joinedCourseIds = await _context.StudentCourses
            .Where(sc => sc.StudentId == student.Id)
            .Select(sc => sc.CourseId)
            .ToListAsync();

        AvailableCourses = await _context.Courses
            .Where(c => !joinedCourseIds.Contains(c.Id))
            .OrderBy(c => c.CourseCode)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.CourseCode + " - " + c.CourseName
            })
            .ToListAsync();
    }
}
