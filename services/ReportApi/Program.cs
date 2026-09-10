using Elastic.Clients.Elasticsearch;
using ReportApi.Middleware;
using ReportApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var elasticsearchUrl =
    builder.Configuration["Elasticsearch:Url"]
    ?? "http://localhost:9200";

var elasticsearchSettings =
    new ElasticsearchClientSettings(
        new Uri(elasticsearchUrl));

var elasticsearchClient =
    new ElasticsearchClient(elasticsearchSettings);

builder.Services.AddSingleton(elasticsearchClient);

builder.Services.AddScoped<ElasticsearchService>();

builder.Services.AddLogging();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();


app.MapControllers();

app.Run();