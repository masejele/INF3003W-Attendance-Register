using System.ComponentModel.DataAnnotations;

namespace UCTAttendanceRegister.Models;

public class StudentCourse
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    [Required]
    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.Now;
}
