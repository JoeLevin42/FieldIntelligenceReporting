using CsharpConsumer.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Logging;

namespace CsharpConsumer.Services;

public class ElasticsearchService
{
    private const string IndexName = "reports";

    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(
        ElasticsearchClient client,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task CreateIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);

        if (exists.Exists)
        {
            return;
        }

        var response = await _client.Indices.CreateAsync(
            IndexName,
            index => index
                .Mappings(mapping => mapping
                    .Properties<Report>(properties => properties
                        .Keyword(x => x.ReportId)
                        .Date("@timestamp")
                        .Keyword(x => x.AgentId)
                        .Keyword(x => x.Unit)
                        .Keyword(x => x.Theater)
                        .Keyword(x => x.Sector)
                        .Keyword(x => x.Location)
                        .Keyword(x => x.ReportType)
                        .Keyword(x => x.Priority)
                        .Keyword(x => x.SourceType)
                        .Text(x => x.Message)
                        .Keyword(x => x.SubjectId)
                        .Keyword(x => x.SubjectType)
                        .Date(x => x.ProcessedAt)
                    )
                )
        );

        if (!response.IsValidResponse)
        {
            throw new Exception(
                $"Could not create Elasticsearch index: {response.DebugInformation}");
        }

        _logger.LogInformation(
            "Elasticsearch index {IndexName} created",
            IndexName);
    }

    public async Task SaveAsync(Report report)
    {
        var response = await _client.CreateAsync(
            report,
            request => request
                .Index(IndexName)
                .Id(report.ReportId!)
        );

        if (response.IsValidResponse)
        {
            _logger.LogInformation(
                "Report {ReportId} saved to Elasticsearch",
                report.ReportId);

            return;
        }

        if (response.ElasticsearchServerError?.Status == 409)
        {
            _logger.LogWarning(
                "Duplicate report {ReportId}. Report was not saved.",
                report.ReportId);

            return;
        }

        _logger.LogError(
            "Failed to save report {ReportId}: {Error}",
            report.ReportId,
            response.DebugInformation);
    }
}