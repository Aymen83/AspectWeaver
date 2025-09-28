using Aymen83.AspectWeaver.Extensions;
using Aymen83.AspectWeaver.Sample.CustomAspect;
using Microsoft.Extensions.DependencyInjection;

// 1. Setup DI Container
var services = new ServiceCollection();

// 2. Register required services for the aspect (IMemoryCache)
services.AddMemoryCache();

// 3. Register AspectWeaver Handlers (scans this assembly for CacheHandler)
services.AddAspectWeaverHandlers<Program>();

// 4. Register the target service
services.AddTransient<DataRepository>();

var serviceProvider = services.BuildServiceProvider();

// --- Demonstration ---
Console.WriteLine("AspectWeaver Custom Caching Aspect Demo");
Console.WriteLine("---------------------------------------");

var repository = serviceProvider.GetRequiredService<DataRepository>();

Console.WriteLine("\nFirst call (ID=1):");
var result1 = await repository.FetchDataAsync(1);
Console.WriteLine($"Result: {result1}");

Console.WriteLine("\nSecond call (ID=1) - Should be cached:");
var result2 = await repository.FetchDataAsync(1);
Console.WriteLine($"Result: {result2}");

Console.WriteLine("\nThird call (ID=2) - New key, should not be cached:");
var result3 = await repository.FetchDataAsync(2);
Console.WriteLine($"Result: {result3}");

Console.WriteLine("\nFourth call (ID=1) - Should still be cached:");
var result4 = await repository.FetchDataAsync(1);
Console.WriteLine($"Result: {result4}");

// You can uncomment the following lines to demonstrate cache expiration:
// Console.WriteLine("\nWaiting 11 seconds for cache expiration...");
// await Task.Delay(11000);
// Console.WriteLine("\nFifth call (ID=1) - Should be expired (Cache Miss):");
// var result5 = await repository.FetchDataAsync(1);
// Console.WriteLine($"Result: {result5}");