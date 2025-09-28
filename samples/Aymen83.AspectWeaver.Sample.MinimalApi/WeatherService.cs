namespace Aymen83.AspectWeaver.Sample.MinimalApi;

public class WeatherService(IServiceProvider serviceProvider) : IWeatherService
{
    // Implement the IServiceProvider requirement
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    private int _attemptCount = 0;

    public Task<string> GetWeatherAsync(string city)
    {
        _attemptCount++;

        // Simulate transient failure for the first attempt only for Paris
        if (_attemptCount == 1 && city.Equals("Paris", StringComparison.OrdinalIgnoreCase))
        {
            // Log locally to observe the retry behavior in the console
            Console.WriteLine($"-> Attempt {_attemptCount}: Simulating transient failure for {city}...");
            throw new TimeoutException("API connection timed out.");
        }

        Console.WriteLine($"-> Attempt {_attemptCount}: Successfully fetching weather for {city}...");
        return Task.FromResult($"Weather in {city} is Sunny. (Succeeded on attempt {_attemptCount})");
    }
}