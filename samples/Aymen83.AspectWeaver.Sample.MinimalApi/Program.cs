using Aymen83.AspectWeaver.Extensions;
using Aymen83.AspectWeaver.Sample.MinimalApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Logging (so LogExecutionHandler can resolve ILoggerFactory)
builder.Services.AddLogging(config => config.AddConsole());

// 2. Register AspectWeaver Handlers
// We must register handlers from the AspectWeaver.Extensions library
builder.Services.AddAspectWeaverHandlers(typeof(Aymen83.AspectWeaver.Extensions.Logging.LogExecutionHandler).Assembly);
// Optionally register handlers defined in this assembly (if any)
// builder.Services.AddAspectWeaverHandlers<Program>();

// 3. Register the target service
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();

app.MapGet("/", () => "AspectWeaver Minimal API Sample. Try:\n" +
                      "- /weather/Paris (Demonstrates Retry and Logging)\n" +
                      "- /weather/London (Demonstrates Success Logging)\n" +
                      "- /weather/ (Demonstrates Validation Error)");

// Define the endpoint
// We inject the interface IWeatherService. The calls will be intercepted.
app.MapGet("/weather/{city?}", async (string? city, IWeatherService weatherService) =>
{
    try
    {
        // The [ValidateParameters] aspect will check if city is null when passed to the method.
        // We use the null-forgiving operator (!) here because the route parameter is optional (nullable),
        // but the service contract requires a non-null string. The aspect handles the validation.
        var weather = await weatherService.GetWeatherAsync(city!);
        return Results.Ok(weather);
    }
    catch (ArgumentNullException ex)
    {
        // Handle validation errors
        return Results.BadRequest($"Validation Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Handle operational errors (e.g., TimeoutException after [Retry] exhaustion)
        return Results.Problem(detail: ex.Message, statusCode: 503); // 503 Service Unavailable
    }
});

Console.WriteLine("Starting AspectWeaver Sample API...");
app.Run();