namespace ReportApi.Models;

public class ReportSearchRequest
{
    public string? Text { get; set; }

    public string? Theater { get; set; }

    public string? Sector { get; set; }

    public string? Location { get; set; }

    public string? Priorities { get; set; }

    public string? ReportType { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}