using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ProTask.API.Middlewares;
using ProTask.Application;
using ProTask.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://httpstatuses.com/400",
            Title = "Error de validación",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Uno o más parámetros de la solicitud son inválidos.",
            Instance = context.HttpContext.Request.Path.Value
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (enableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ProTask API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program
{
}
