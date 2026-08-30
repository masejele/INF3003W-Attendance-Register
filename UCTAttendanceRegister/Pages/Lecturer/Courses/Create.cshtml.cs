using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Courses;

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
    public Course Course { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var lecturer = await _userManager.GetUserAsync(User);

        if (lecturer == null)
        {
            return Forbid();
        }

        Course.EnrollmentCode = Course.EnrollmentCode.Trim();

        if (string.IsNullOrWhiteSpace(Course.EnrollmentCode))
        {
            Course.EnrollmentCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }
        else
        {
            Course.EnrollmentCode = Course.EnrollmentCode.ToUpperInvariant();
        }

        var exists = await _context.Courses
            .AnyAsync(c => c.EnrollmentCode == Course.EnrollmentCode);

        if (exists)
        {
            ModelState.AddModelError(nameof(Course.EnrollmentCode), "That enrollment code is already in use. Please choose another one.");
            return Page();
        }

        Course.LecturerId = lecturer.Id;

        _context.Courses.Add(Course);

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}