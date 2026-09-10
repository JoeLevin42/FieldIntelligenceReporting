using System.Globalization;
using CsharpConsumer.Models;

namespace CsharpConsumer.Validation;

public class ReportValidator
{
    private readonly string[] _allowedPriorities =
    {
        "Low",
        "Medium",
        "High",
        "Critical"
    };

    private readonly string[] _allowedReportTypes =
    {
        "Observation",
        "Movement",
        "Meeting",
        "Access",
        "Communication",
        "Logistics",
        "Incident"
    };

    public bool Validate(Report report, out string reason)
    {
        if (string.IsNullOrWhiteSpace(report.ReportId))
        {
            reason = "reportId is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Timestamp))
        {
            reason = "timestamp is missing or empty";
            return false;
        }

        if (!DateTimeOffset.TryParse(
                report.Timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            reason = "timestamp is not a valid date and time";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.AgentId))
        {
            reason = "agentId is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Unit))
        {
            reason = "unit is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Theater))
        {
            reason = "theater is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Sector))
        {
            reason = "sector is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Location))
        {
            reason = "location is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.ReportType))
        {
            reason = "reportType is missing or empty";
            return false;
        }

        if (!_allowedReportTypes.Contains(report.ReportType))
        {
            reason = $"invalid reportType: {report.ReportType}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Priority))
        {
            reason = "priority is missing or empty";
            return false;
        }

        if (!_allowedPriorities.Contains(report.Priority))
        {
            reason = $"invalid priority: {report.Priority}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.SourceType))
        {
            reason = "sourceType is missing or empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.Message))
        {
            reason = "message is missing or empty";
            return false;
        }

        bool hasSubjectId = !string.IsNullOrWhiteSpace(report.SubjectId);
        bool hasSubjectType = !string.IsNullOrWhiteSpace(report.SubjectType);

        if (hasSubjectId != hasSubjectType)
        {
            reason = "subjectId and subjectType must appear together";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}