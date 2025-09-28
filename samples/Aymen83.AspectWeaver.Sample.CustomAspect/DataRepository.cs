namespace Aymen83.AspectWeaver.Sample.CustomAspect;

public class DataRepository
{
    // Expose IServiceProvider (AW001 requirement)
    internal IServiceProvider ServiceProvider { get; }

    public DataRepository(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    // Apply the custom aspect
    [Cache(DurationSeconds = 10)]
    public virtual async Task<string> FetchDataAsync(int id)
    {
        Console.WriteLine($"... (Executing expensive database query for ID={id}) ...");
        await Task.Delay(500); // Simulate latency
        return $"Data_{id}_{DateTime.Now:HH:mm:ss}";
    }
}