namespace ReportApi.Models;

public class StatisticsResponse
{
    public List<StatisticItem> ByPriority { get; set; } = new();

    public List<StatisticItem> ByReportType { get; set; } = new();

    public List<StatisticItem> ByTheater { get; set; } = new();
}

public class StatisticItem
{
    public string Value { get; set; } = "";

    public long Count { get; set; }
}