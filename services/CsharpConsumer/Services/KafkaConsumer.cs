using System.Text.Json;
using Confluent.Kafka;
using CsharpConsumer.Models;
using CsharpConsumer.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CsharpConsumer.Services;

public class KafkaConsumer : BackgroundService
{
    private const string Topic = "raw_data";

    private readonly ReportValidator _validator;
    private readonly ElasticsearchService _elasticsearch;
    private readonly ILogger<KafkaConsumer> _logger;

    private readonly string _kafkaBroker;

    public KafkaConsumer(
        ReportValidator validator,
        ElasticsearchService elasticsearch,
        ILogger<KafkaConsumer> logger)
    {
        _validator = validator;
        _elasticsearch = elasticsearch;
        _logger = logger;

        _kafkaBroker =
            Environment.GetEnvironmentVariable("KAFKA_BROKER")
            ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _elasticsearch.CreateIndexAsync();

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaBroker,
            GroupId = "csharp-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .Build();

        consumer.Subscribe(Topic);

        _logger.LogInformation(
            "Kafka consumer started. Broker: {Broker}, Topic: {Topic}",
            _kafkaBroker,
            Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);

                try
                {
                    var report = JsonSerializer.Deserialize<Report>(
                        result.Message.Value);

                    if (report == null)
                    {
                        _logger.LogWarning(
                            "Report rejected: empty JSON object");

                        continue;
                    }

                    if (!_validator.Validate(report, out string reason))
                    {
                        _logger.LogWarning(
                            "Report {ReportId} rejected: {Reason}",
                            report.ReportId ?? "unknown",
                            reason);

                        continue;
                    }

                    report.ProcessedAt = DateTimeOffset.UtcNow;

                    await _elasticsearch.SaveAsync(report);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Report rejected: invalid JSON");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while processing Kafka message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }
}