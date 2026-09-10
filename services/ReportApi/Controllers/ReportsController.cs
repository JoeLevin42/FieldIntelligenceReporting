using Microsoft.AspNetCore.Mvc;
using ReportApi.Models;
using ReportApi.Services;

namespace ReportApi.Controllers;

[ApiController]
[Route("api")]
public class ReportsController : ControllerBase
{
    private readonly ElasticsearchService _elasticsearchService;

    public ReportsController(
        ElasticsearchService elasticsearchService)
    {
        _elasticsearchService = elasticsearchService;
    }

    [HttpGet("reports/search")]
    public async Task<ActionResult<List<Report>>> Search(
        [FromQuery] ReportSearchRequest request)
    {
        var reports =
            await _elasticsearchService.SearchAsync(request);

        return Ok(reports);
    }

    [HttpGet("subjects/{subjectId}/reports")]
    public async Task<ActionResult<List<Report>>> GetSubjectReports(
        string subjectId)
    {
        var reports =
            await _elasticsearchService.GetSubjectReportsAsync(subjectId);

        return Ok(reports);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<List<Report>>> GetReports(
        [FromQuery] ReportSearchRequest request)
    {
        var reports =
            await _elasticsearchService.SearchAsync(request);

        return Ok(reports);
    }

    [HttpGet("reports/statistics")]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics()
    {
        var statistics =
            await _elasticsearchService.GetStatisticsAsync();

        return Ok(statistics);
    }
}