using System.ComponentModel.DataAnnotations;

namespace UCTAttendanceRegister.Models;

public class AttendanceSession
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Session Date")]
    public DateTime SessionDate { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Required]
    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public bool IsOpen { get; set; } = true;
}