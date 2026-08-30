using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Course> Courses { get; set; }

    public DbSet<AttendanceSession> AttendanceSessions { get; set; }

    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
}