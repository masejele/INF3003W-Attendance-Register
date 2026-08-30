using System.ComponentModel.DataAnnotations;

namespace UCTAttendanceRegister.Models;

public class AttendanceRecord
{
    public int Id { get; set; }

    [Required]
    public int AttendanceSessionId { get; set; }

    public AttendanceSession? AttendanceSession { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.Now;
}