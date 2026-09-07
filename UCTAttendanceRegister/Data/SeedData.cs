using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        var context =
            serviceProvider.GetRequiredService<ApplicationDbContext>();

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
        var lecturerEmail = "lecturer@test.local";

        var lecturer =
            await userManager.FindByEmailAsync(lecturerEmail);

        if (lecturer == null)
        {
            lecturer = new ApplicationUser
            {
                UserName = lecturerEmail,
                Email = lecturerEmail,
                EmailConfirmed = true,
                FullName = "Walter White"
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
        var fixedCourse = await context.Courses
            .FirstOrDefaultAsync(course => course.CourseCode == "INF3003W");

        if (fixedCourse == null)
        {
            fixedCourse = await context.Courses
                .FirstOrDefaultAsync(course =>
                    course.CourseCode == "INF3003" &&
                    course.EnrollmentCode == "INF3003W");
        }

        if (fixedCourse == null)
        {
            fixedCourse = new Course
            {
                CourseCode = "INF3003W",
                CourseName = "Information Systems",
                EnrollmentCode = "INF3003W",
                LecturerId = lecturer?.Id
            };

            context.Courses.Add(fixedCourse);
        }
        else
        {
            fixedCourse.CourseCode = "INF3003W";
            fixedCourse.EnrollmentCode = "INF3003W";

            if (string.IsNullOrWhiteSpace(fixedCourse.LecturerId))
            {
                fixedCourse.LecturerId = lecturer?.Id;
            }
        }

        var legacyStudent = await context.Users
            .FirstOrDefaultAsync(user => user.StudentNumber == "JLXMAS002");

        if (legacyStudent != null)
        {
            var legacyEnrollment = await context.StudentCourses
                .FirstOrDefaultAsync(enrollment =>
                    enrollment.StudentId == legacyStudent.Id &&
                    enrollment.CourseId == fixedCourse.Id);

            if (legacyEnrollment != null)
            {
                context.StudentCourses.Remove(legacyEnrollment);
            }
        }

        var demoStudentEmail = "student@test.local";
        var demoStudent = await userManager.FindByEmailAsync(demoStudentEmail);

        if (demoStudent == null)
        {
            demoStudent = new ApplicationUser
            {
                UserName = demoStudentEmail,
                Email = demoStudentEmail,
                EmailConfirmed = true,
                FullName = "Student Name 001",
                StudentNumber = "STDNUM001"
            };

            var createResult = await userManager.CreateAsync(demoStudent, "Student123!");
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "The demo student account could not be created.");
            }
        }

        var duplicateCsvStudents = await context.Users
            .Where(user => user.StudentNumber == "STDNUM001" && user.Id != demoStudent.Id)
            .ToListAsync();

        foreach (var duplicateCsvStudent in duplicateCsvStudents)
        {
            var duplicateRecords = await context.AttendanceRecords
                .Where(record => record.StudentId == duplicateCsvStudent.Id)
                .ToListAsync();

            foreach (var duplicateRecord in duplicateRecords)
            {
                var demoRecordExists = await context.AttendanceRecords
                    .AnyAsync(record =>
                        record.StudentId == demoStudent.Id &&
                        record.AttendanceSessionId == duplicateRecord.AttendanceSessionId);

                if (demoRecordExists)
                {
                    context.AttendanceRecords.Remove(duplicateRecord);
                }
                else
                {
                    duplicateRecord.StudentId = demoStudent.Id;
                }
            }

            var duplicateEnrollments = await context.StudentCourses
                .Where(enrollment => enrollment.StudentId == duplicateCsvStudent.Id)
                .ToListAsync();

            foreach (var duplicateEnrollment in duplicateEnrollments)
            {
                var existingDemoEnrollment = await context.StudentCourses
                    .AnyAsync(enrollment =>
                        enrollment.StudentId == demoStudent.Id &&
                        enrollment.CourseId == duplicateEnrollment.CourseId);

                if (existingDemoEnrollment)
                {
                    context.StudentCourses.Remove(duplicateEnrollment);
                }
                else
                {
                    duplicateEnrollment.StudentId = demoStudent.Id;
                }
            }

            duplicateCsvStudent.StudentNumber = null;
        }

        demoStudent.FullName = "Student Name 001";
        demoStudent.StudentNumber = "STDNUM001";
        demoStudent.EmailConfirmed = true;
        await userManager.UpdateAsync(demoStudent);

        if (!await userManager.IsInRoleAsync(demoStudent, "Student"))
        {
            await userManager.AddToRoleAsync(demoStudent, "Student");
        }

        var demoEnrollmentExists = await context.StudentCourses
            .AnyAsync(enrollment =>
                enrollment.StudentId == demoStudent.Id &&
                enrollment.CourseId == fixedCourse.Id);

        if (!demoEnrollmentExists)
        {
            context.StudentCourses.Add(new StudentCourse
            {
                StudentId = demoStudent.Id,
                CourseId = fixedCourse.Id,
                Course = fixedCourse,
                JoinedAt = DateTime.Now
            });
        }

        await context.SaveChangesAsync();
    }
}