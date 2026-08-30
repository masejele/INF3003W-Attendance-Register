using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Course> Courses { get; set; }

    public DbSet<StudentCourse> StudentCourses { get; set; }

    public DbSet<AttendanceSession> AttendanceSessions { get; set; }

    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Course>()
            .HasIndex(c => c.EnrollmentCode)
            .IsUnique();

        builder.Entity<StudentCourse>()
            .HasIndex(sc => new { sc.StudentId, sc.CourseId })
            .IsUnique();

        builder.Entity<StudentCourse>()
            .HasOne(sc => sc.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(sc => sc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentCourse>()
            .HasOne(sc => sc.Course)
            .WithMany(c => c.StudentCourses)
            .HasForeignKey(sc => sc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}