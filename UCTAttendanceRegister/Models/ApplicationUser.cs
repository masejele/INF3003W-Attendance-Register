using Microsoft.AspNetCore.Identity;

namespace UCTAttendanceRegister.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? StudentNumber { get; set; }
    }
}