using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using UCTAttendanceRegister.Data;
using UCTAttendanceRegister.Models;

namespace UCTAttendanceRegister.Services;

public class AttendanceCsvImportService
{
    private const string DateFormat = "yyyy/MM/dd";
    private readonly ApplicationDbContext _context;
    private readonly Inf3003wCourseService _courseService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHostEnvironment _environment;

    public AttendanceCsvImportService(
        ApplicationDbContext context,
        Inf3003wCourseService courseService,
        UserManager<ApplicationUser> userManager,
        IHostEnvironment environment)
    {
        _context = context;
        _courseService = courseService;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task EnsureCsvRosterImportedAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(
            _environment.ContentRootPath,
            "Data",
            "Imports",
            "INF3003_2026_AttendanceList.csv");

        if (!File.Exists(filePath))
        {
            return;
        }

        var course = await _courseService.GetAsync(cancellationToken);
        if (course == null)
        {
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return;
        }

        var headers = lines[0]
            .Split(';')
            .Select((header, index) => index == 0
                ? header.TrimStart('\uFEFF').Trim()
                : header.Trim())
            .ToArray();

        if (headers.Length < 3 ||
            !string.Equals(headers[0], "Student Name", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(headers[1], "Student No", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dates = headers.Skip(2)
            .Select(header => DateTime.TryParseExact(
                header,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date) ? date.Date : (DateTime?)null)
            .ToArray();

        var sessions = await _context.AttendanceSessions
            .Where(session => session.CourseId == course.Id)
            .ToListAsync(cancellationToken);
        var sessionsByDate = sessions
            .GroupBy(session => session.SessionDate.Date)
            .ToDictionary(group => group.Key, group => group.First());

        var users = await _context.Users
            .Where(user => user.StudentNumber != null)
            .ToListAsync(cancellationToken);
        var usersByNumber = users
            .Where(user => !string.IsNullOrWhiteSpace(user.StudentNumber))
            .GroupBy(user => user.StudentNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        for (var lineNumber = 1; lineNumber < lines.Length; lineNumber++)
        {
            var columns = lines[lineNumber].Split(';');
            if (columns.Length < headers.Length)
            {
                continue;
            }

            var studentName = columns[0].Trim();
            var studentNumber = columns[1].Trim();
            if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNumber))
            {
                continue;
            }

            if (!usersByNumber.TryGetValue(studentNumber, out var student))
            {
                student = new ApplicationUser
                {
                    UserName = $"{studentNumber.ToLowerInvariant()}@inf3003.local",
                    Email = $"{studentNumber.ToLowerInvariant()}@inf3003.local",
                    EmailConfirmed = true,
                    FullName = studentName,
                    StudentNumber = studentNumber
                };

                var createResult = await _userManager.CreateAsync(student);
                if (!createResult.Succeeded)
                {
                    continue;
                }

                await _userManager.AddToRoleAsync(student, "Student");
                usersByNumber[studentNumber] = student;
            }
            else
            {
                student.FullName = studentName;
            }

            var enrollmentExists = await _context.StudentCourses
                .AnyAsync(enrollment =>
                    enrollment.StudentId == student.Id &&
                    enrollment.CourseId == course.Id,
                    cancellationToken);

            if (!enrollmentExists)
            {
                _context.StudentCourses.Add(new StudentCourse
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    JoinedAt = DateTime.Now
                });
            }

            for (var column = 2; column < headers.Length; column++)
            {
                if (dates[column - 2] is not DateTime date ||
                    columns[column].Trim() != "1" ||
                    !sessionsByDate.TryGetValue(date, out var session))
                {
                    continue;
                }

                var recordExists = await _context.AttendanceRecords
                    .AnyAsync(record =>
                        record.AttendanceSessionId == session.Id &&
                        record.StudentId == student.Id,
                        cancellationToken);

                if (!recordExists)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceSessionId = session.Id,
                        StudentId = student.Id,
                        MarkedAt = DateTime.Now
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AttendanceCsvImportResult> ImportAsync(
        IFormFile? file,
        TimeSpan startTime,
        TimeSpan endTime,
        CancellationToken cancellationToken = default)
    {
        var result = new AttendanceCsvImportResult();

        if (file == null || file.Length == 0)
        {
            result.ErrorMessage = "Please select a CSV file to import.";
            return result;
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            result.ErrorMessage = "The uploaded file must have a .csv extension.";
            return result;
        }

        if (startTime >= endTime)
        {
            result.ErrorMessage = "The end time must be after the start time.";
            return result;
        }

        var course = await _courseService.GetAsync(cancellationToken);
        if (course == null)
        {
            result.ErrorMessage = "The INF3003W course could not be found.";
            return result;
        }

        try
        {
            using var reader = new StreamReader(
                file.OpenReadStream(),
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

            var content = await reader.ReadToEndAsync(cancellationToken);
            var lines = content
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                result.ErrorMessage = "The CSV file is empty.";
                return result;
            }

            var headers = lines[0]
                .Split(';')
                .Select((header, index) => index == 0 ? header.TrimStart('\uFEFF').Trim() : header.Trim())
                .ToArray();

            if (headers.Length < 3 ||
                !string.Equals(headers[0], "Student Name", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(headers[1], "Student No", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = "The CSV header must start with Student Name;Student No and contain at least one attendance date.";
                return result;
            }

            var dates = new DateTime?[headers.Length - 2];
            for (var column = 2; column < headers.Length; column++)
            {
                if (DateTime.TryParseExact(
                        headers[column],
                        DateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    dates[column - 2] = date.Date;
                }
                else
                {
                    result.InvalidDates++;
                }
            }

            var users = await _context.Users
                .Where(u => u.StudentNumber != null)
                .ToListAsync(cancellationToken);

            var studentsByNumber = users
                .Where(u => !string.IsNullOrWhiteSpace(u.StudentNumber))
                .GroupBy(u => u.StudentNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var validDates = dates
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .Distinct()
                .ToList();

            var sessions = await _context.AttendanceSessions
                .Where(session => session.CourseId == course.Id && validDates.Contains(session.SessionDate.Date))
                .ToListAsync(cancellationToken);

            var sessionsByDate = sessions
                .GroupBy(session => session.SessionDate.Date)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var date in validDates)
            {
                if (sessionsByDate.ContainsKey(date))
                {
                    continue;
                }

                var session = new AttendanceSession
                {
                    CourseId = course.Id,
                    SessionDate = date,
                    StartTime = startTime,
                    EndTime = endTime,
                    IsOpen = false
                };

                _context.AttendanceSessions.Add(session);
                sessionsByDate[date] = session;
                result.SessionsCreated++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var sessionIds = sessionsByDate.Values.Select(session => session.Id).ToList();
            var existingRecordKeys = (await _context.AttendanceRecords
                    .Where(record => sessionIds.Contains(record.AttendanceSessionId))
                    .Select(record => new { record.AttendanceSessionId, record.StudentId })
                    .ToListAsync(cancellationToken))
                .Select(record => CreateRecordKey(record.AttendanceSessionId, record.StudentId))
                .ToHashSet();

            for (var lineNumber = 1; lineNumber < lines.Length; lineNumber++)
            {
                var columns = lines[lineNumber].Split(';');
                result.StudentsProcessed++;

                if (columns.Length < headers.Length)
                {
                    result.OtherSkippedRecords++;
                    continue;
                }

                var studentNumber = columns[1].Trim();
                var studentName = columns[0].Trim();
                if (string.IsNullOrWhiteSpace(studentNumber) || string.IsNullOrWhiteSpace(studentName))
                {
                    if (!string.IsNullOrWhiteSpace(studentNumber) &&
                        !result.StudentsNotFound.Contains(studentNumber, StringComparer.OrdinalIgnoreCase))
                    {
                        result.StudentsNotFound.Add(studentNumber);
                    }

                    result.OtherSkippedRecords++;
                    continue;
                }

                if (!studentsByNumber.TryGetValue(studentNumber, out var student))
                {
                    student = new ApplicationUser
                    {
                        UserName = $"{studentNumber.ToLowerInvariant()}@inf3003.local",
                        Email = $"{studentNumber.ToLowerInvariant()}@inf3003.local",
                        EmailConfirmed = true,
                        FullName = studentName,
                        StudentNumber = studentNumber
                    };

                    var createResult = await _userManager.CreateAsync(student);
                    if (!createResult.Succeeded)
                    {
                        result.StudentsNotFound.Add(studentNumber);
                        result.OtherSkippedRecords++;
                        continue;
                    }

                    await _userManager.AddToRoleAsync(student, "Student");
                    studentsByNumber[studentNumber] = student;
                }
                else
                {
                    student.FullName = studentName;
                }

                var enrollmentExists = await _context.StudentCourses
                    .AnyAsync(enrollment =>
                        enrollment.StudentId == student.Id &&
                        enrollment.CourseId == course.Id,
                        cancellationToken);

                if (!enrollmentExists)
                {
                    _context.StudentCourses.Add(new StudentCourse
                    {
                        StudentId = student.Id,
                        CourseId = course.Id,
                        JoinedAt = DateTime.Now
                    });
                }

                for (var column = 2; column < headers.Length; column++)
                {
                    var date = dates[column - 2];
                    if (!date.HasValue)
                    {
                        result.OtherSkippedRecords++;
                        continue;
                    }

                    var value = columns[column].Trim();
                    if (value != "0" && value != "1")
                    {
                        result.InvalidAttendanceValues++;
                        result.OtherSkippedRecords++;
                        continue;
                    }

                    if (value == "0")
                    {
                        continue;
                    }

                    var session = sessionsByDate[date.Value];
                    var recordKey = CreateRecordKey(session.Id, student.Id);
                    if (!existingRecordKeys.Add(recordKey))
                    {
                        result.DuplicateAttendanceRecordsSkipped++;
                        continue;
                    }

                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceSessionId = session.Id,
                        StudentId = student.Id,
                        MarkedAt = DateTime.Now
                    });
                    result.AttendanceRecordsImported++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result.Succeeded = true;
            return result;
        }
        catch (Exception)
        {
            result.ErrorMessage = "The CSV could not be imported. Check the file format and try again.";
            return result;
        }
    }

    private static string CreateRecordKey(int sessionId, string studentId)
    {
        return $"{sessionId}:{studentId}";
    }
}

public class AttendanceCsvImportResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int StudentsProcessed { get; set; }
    public int AttendanceRecordsImported { get; set; }
    public int DuplicateAttendanceRecordsSkipped { get; set; }
    public List<string> StudentsNotFound { get; } = new();
    public int InvalidAttendanceValues { get; set; }
    public int InvalidDates { get; set; }
    public int SessionsCreated { get; set; }
    public int OtherSkippedRecords { get; set; }
}