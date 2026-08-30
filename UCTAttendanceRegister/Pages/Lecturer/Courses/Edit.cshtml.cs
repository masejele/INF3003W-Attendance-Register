using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Pages.Lecturer.Courses;

[Authorize(Roles = "Lecturer")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Course Course { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existingCourse = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == Course.Id);

        if (existingCourse == null)
        {
            return NotFound();
        }

        existingCourse.CourseCode = Course.CourseCode;
        existingCourse.CourseName = Course.CourseName;

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}