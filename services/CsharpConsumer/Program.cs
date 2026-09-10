using CsharpConsumer.Services;
using CsharpConsumer.Validation;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

var elasticsearchUrl =
    Environment.GetEnvironmentVariable("ELASTICSEARCH_URL")
    ?? "http://localhost:9200";

var elasticsearchClient =
    new ElasticsearchClient(
        new ElasticsearchClientSettings(
            new Uri(elasticsearchUrl)));

builder.Services.AddSingleton(elasticsearchClient);

builder.Services.AddSingleton<ReportValidator>();

builder.Services.AddSingleton<ElasticsearchService>();

builder.Services.AddHostedService<KafkaConsumer>();

var app = builder.Build();

await app.RunAsync();