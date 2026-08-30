using System.ComponentModel.DataAnnotations;

namespace UCTAttendanceRegister.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Course Code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    // Lecturer responsible for this course
    public string? LecturerId { get; set; }

    public ApplicationUser? Lecturer { get; set; }
}