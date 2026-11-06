using System.Text.Json.Serialization;
using FinanceAI.Api.ErrorHandling;
using FinanceAI.Application;
using FinanceAI.Application.Abstractions;
using FinanceAI.Infrastructure;
using FinanceAI.Infrastructure.Persistence;
using FinanceAI.ML;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.IncludeXmlComments(
    Path.Combine(AppContext.BaseDirectory, "FinanceAI.Api.xml")));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddMachineLearning();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
// Loads (or trains) the model at startup instead of on the first request.
app.Services.GetRequiredService<ITransactionClassifier>();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
