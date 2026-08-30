namespace UCTAttendanceRegister.Models;

public static class AttendanceInsightCalculator
{
    public const double GoodStandingThreshold = 75d;
    public const double NeedsAttentionThreshold = 60d;

    public static string GetStatus(double percentage)
    {
        if (percentage >= GoodStandingThreshold)
        {
            return "Good Standing";
        }

        if (percentage >= NeedsAttentionThreshold)
        {
            return "Needs Attention";
        }

        return "At Risk";
    }

    public static double CalculatePercentage(int attended, int total)
    {
        if (total <= 0)
        {
            return 0d;
        }

        return Math.Round((attended * 100d) / total, 1);
    }

    public static ForecastResult CalculateForecast(
        int attended,
        int total,
        double target = GoodStandingThreshold)
    {
        var result = new ForecastResult
        {
            CurrentAttendancePercentage = CalculatePercentage(attended, total),
            CurrentSessionsAttended = attended,
            TotalSessionsHeld = total,
            TargetPercentage = target
        };

        if (total <= 0)
        {
            result.Message = "No attendance sessions are available yet.";
            return result;
        }

        if (result.CurrentAttendancePercentage >= target)
        {
            var targetRatio = target / 100d;
            var missableSessions = Math.Floor((attended / targetRatio) - total);

            if (missableSessions > 0)
            {
                result.MissableFutureSessions = (int)missableSessions;
                result.Message = $"You're on track! Your attendance is currently {result.CurrentAttendancePercentage:0.##}%. You could miss up to {result.MissableFutureSessions} future session(s) and still stay at or above {target:0.##}%.";
            }
            else
            {
                result.Message = $"You're on track! Your attendance is currently {result.CurrentAttendancePercentage:0.##}%.";
            }

            return result;
        }

        var requiredSessions = Math.Ceiling((target / 100d * total - attended) / (1d - (target / 100d)));

        result.RequiredFutureSessions = (int)Math.Max(1, requiredSessions);
        result.Message = $"Your attendance is currently {result.CurrentAttendancePercentage:0.##}%. You need to attend your next {result.RequiredFutureSessions} session(s) consecutively to reach {target:0.##}%.";

        return result;
    }
}

public class CourseAttendanceInsight
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int SessionsAttended { get; set; }
    public int TotalSessionsHeld { get; set; }
    public double AttendancePercentage { get; set; }
    public string Status { get; set; } = "At Risk";
    public ForecastResult Forecast { get; set; } = new();
}

public class ForecastResult
{
    public double CurrentAttendancePercentage { get; set; }
    public int CurrentSessionsAttended { get; set; }
    public int TotalSessionsHeld { get; set; }
    public double TargetPercentage { get; set; } = AttendanceInsightCalculator.GoodStandingThreshold;
    public int RequiredFutureSessions { get; set; }
    public int MissableFutureSessions { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CoursePerformanceSummary
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public double AverageAttendancePercentage { get; set; }
    public int StudentCount { get; set; }
    public int GoodStandingCount { get; set; }
    public int NeedsAttentionCount { get; set; }
    public int AtRiskCount { get; set; }
    public List<StudentCourseAttendanceRisk> StudentsBelowTarget { get; set; } = new();
}

public class StudentCourseAttendanceRisk
{
    public string StudentName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public double AttendancePercentage { get; set; }
    public string Status { get; set; } = "At Risk";
}
