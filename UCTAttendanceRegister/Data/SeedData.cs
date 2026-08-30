using Microsoft.AspNetCore.Identity;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create Student role
        if (!await roleManager.RoleExistsAsync("Student"))
        {
            await roleManager.CreateAsync(new IdentityRole("Student"));
        }

        // Create Lecturer role
        if (!await roleManager.RoleExistsAsync("Lecturer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Lecturer"));
        }

        // Create default lecturer account
        var lecturerEmail = "lecturer@inf3003.local";

        var lecturer =
            await userManager.FindByEmailAsync(lecturerEmail);

        if (lecturer == null)
        {
            lecturer = new ApplicationUser
            {
                UserName = lecturerEmail,
                Email = lecturerEmail,
                EmailConfirmed = true,
                FullName = "INF3003 Lecturer"
            };

            var result = await userManager.CreateAsync(
                lecturer,
                "Lecturer123!"
            );

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    lecturer,
                    "Lecturer"
                );
            }
        }

        // Create default student account
        var studentEmail = "student@inf3003.local";

        var student = await userManager.FindByEmailAsync(studentEmail);

        if (student == null)
        {
            student = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                EmailConfirmed = true,
                FullName = "INF3003 Student",
                StudentNumber = "JLXMAS002"
            };

            var studentResult = await userManager.CreateAsync(
                student,
                "Student123!"
            );

            if (studentResult.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    student,
                    "Student"
                );
            }
        }
        else
        {
            // Update existing student with a student number
            if (string.IsNullOrEmpty(student.StudentNumber))
            {
                student.StudentNumber = "JLXMAS002";

                await userManager.UpdateAsync(student);
            }
        }
    }
}