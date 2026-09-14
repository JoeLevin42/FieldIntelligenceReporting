using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ReportApi.Exceptions;
using ReportApi.Models;

namespace ReportApi.Services;

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

    public async Task<List<Report>> SearchAsync(
        ReportSearchRequest request)
    {
        ValidateRequest(request);

        try
        {
            var mustQueries = new List<Query>();
            var filterQueries = new List<Query>();

            if (!string.IsNullOrWhiteSpace(request.Text))
            {
                var matchQuery = new MatchQuery(
                    new Field("message"))
                {
                    Query = request.Text
                };

                mustQueries.Add(matchQuery);
            }

            AddFilters(request, filterQueries);

            var response =
                await _client.SearchAsync<Report>(s => s
                    .Indices(IndexName)
                    .Size(1000)
                    .Query(q => q
                        .Bool(b =>
                        {
                            if (mustQueries.Count > 0)
                            {
                                b.Must(mustQueries.ToArray());
                            }

                             if (filterQueries.Count > 0)
                            {
                                b.Filter(filterQueries.ToArray());
                            }
                        }))
                    .Sort(sort => sort
                        .Field(x => x.Timestamp)));

            EnsureSuccessful(response);

            return response.Documents.ToList();
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to search Elasticsearch");

            throw new ExternalServiceException(
                "Failed to communicate with Elasticsearch.",
                ex);
        }
    }

    public async Task<List<Report>> GetSubjectReportsAsync(
        string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new BadRequestException(
                "subjectId cannot be empty.");
        }

        try
        {
            var response =
                await _client.SearchAsync<Report>(s => s
                    .Indices(IndexName)
                    .Size(1000)
                    .Query(q => q
                        .Term(t => t
                            .Field(x => x.SubjectId)
                            .Value(subjectId)))
                    .Sort(sort => sort
                        .Field(x => x.Timestamp)));

            EnsureSuccessful(response);

            var reports = response.Documents.ToList();

            if (reports.Count == 0)
            {
                throw new NotFoundException(
                    $"No reports were found for subject '{subjectId}'.");
            }

            return reports;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve reports for subject {SubjectId}",
                subjectId);

            throw new ExternalServiceException(
                "Failed to communicate with Elasticsearch.",
                ex);
        }
    }

    public async Task<StatisticsResponse> GetStatisticsAsync()
    {
        try
        {
            var response =
                await _client.SearchAsync<Report>(s => s
                    .Indices(IndexName)
                    .Size(0)
                    .Aggregations(aggs => aggs
                        .Add("by_priority", a => a
                            .Terms(t => t
                                .Field(x => x.Priority)
                                .Size(10)))

                        .Add("by_report_type", a => a
                            .Terms(t => t
                                .Field(x => x.ReportType)
                                .Size(20)))

                        .Add("by_theater", a => a
                            .Terms(t => t
                                .Field(x => x.Theater)
                                .Size(100)))));

            EnsureSuccessful(response);

            var result = new StatisticsResponse();

            var priorityAggregation =
                response.Aggregations
                    .GetStringTerms("by_priority");

            if (priorityAggregation != null &&
                priorityAggregation.Buckets != null)
            {
                foreach (var bucket in priorityAggregation.Buckets)
                {
                    result.ByPriority.Add(new StatisticItem
                    {
                        Value = bucket.Key.ToString(),
                        Count = bucket.DocCount
                    });
                }
            }

            var reportTypeAggregation =
                response.Aggregations
                    .GetStringTerms("by_report_type");

            if (reportTypeAggregation != null &&
                reportTypeAggregation.Buckets != null)
            {
                foreach (var bucket in reportTypeAggregation.Buckets)
                {
                    result.ByReportType.Add(new StatisticItem
                    {
                        Value = bucket.Key.ToString(),
                        Count = bucket.DocCount
                    });
                }
            }

            var theaterAggregation =
                response.Aggregations
                    .GetStringTerms("by_theater");

            if (theaterAggregation != null &&
                theaterAggregation.Buckets != null)
            {
                foreach (var bucket in theaterAggregation.Buckets)
                {
                    result.ByTheater.Add(new StatisticItem
                    {
                        Value = bucket.Key.ToString(),
                        Count = bucket.DocCount
                    });
                }
            }

            return result;
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve Elasticsearch statistics");

            throw new ExternalServiceException(
                "Failed to communicate with Elasticsearch.",
                ex);
        }
    }

    private static void AddFilters(
        ReportSearchRequest request,
        List<Query> filters)
    {
        if (!string.IsNullOrWhiteSpace(request.Theater))
        {
            filters.Add(
                new TermQuery(
                    new Field("theater"))
                {
                    Value = request.Theater
                });
        }

        if (!string.IsNullOrWhiteSpace(request.Sector))
        {
            filters.Add(
                new TermQuery(
                    new Field("sector"))
                {
                    Value = request.Sector
                });
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            filters.Add(
                new TermQuery(
                    new Field("location"))
                {
                    Value = request.Location
                });
        }

        if (!string.IsNullOrWhiteSpace(request.ReportType))
        {
            filters.Add(
                new TermQuery(
                    new Field("reportType"))
                {
                    Value = request.ReportType
                });
        }

        if (!string.IsNullOrWhiteSpace(request.Priorities))
        {
            var priorities = request.Priorities
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToArray();

            var priorityValues = priorities
                .Select(x => (FieldValue)x)
                .ToArray();

            filters.Add(
                new TermsQuery
                {
                    Field = new Field("priority"),
                    Term = new TermsQueryField(priorityValues)
                });
        }

        if (request.From.HasValue ||
            request.To.HasValue)
        {
            var dateQuery = new DateRangeQuery(
                new Field("@timestamp"));

            if (request.From.HasValue)
            {
                dateQuery.Gte =
                    request.From.Value.ToString("O");
            }

            if (request.To.HasValue)
            {
                dateQuery.Lte =
                    request.To.Value.ToString("O");
            }

            filters.Add(dateQuery);
        }
    }

    private static void ValidateRequest(
        ReportSearchRequest request)
    {
        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From > request.To)
        {
            throw new BadRequestException(
                "'from' cannot be later than 'to'.");
        }

        if (!string.IsNullOrWhiteSpace(request.Priorities))
        {
            var priorities = request.Priorities
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var allowed = new[]
            {
                "High",
                "Critical"
            };

            if (priorities.Any(x => !allowed.Contains(x)))
            {
                throw new BadRequestException(
                    "priorities must contain only High, Critical, or both.");
            }
        }
    }

    private void EnsureSuccessful<T>(
        SearchResponse<T> response)
    {
        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Elasticsearch request failed: {DebugInformation}",
                response.DebugInformation);

            throw new ExternalServiceException(
                $"Elasticsearch request failed: {response.DebugInformation}");
        }
    }
}