using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Services;

public class Inf3003wCourseService
{
    public const string CourseCode = "INF3003W";

    private readonly ApplicationDbContext _context;

    public Inf3003wCourseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Course?> GetAsync(CancellationToken cancellationToken = default)
    {
        return _context.Courses
            .FirstOrDefaultAsync(
                course => course.CourseCode.Trim().ToUpper() == CourseCode,
                cancellationToken);
    }
}