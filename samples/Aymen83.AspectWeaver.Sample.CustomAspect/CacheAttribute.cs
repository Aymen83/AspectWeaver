using Aymen83.AspectWeaver.Abstractions;

namespace Aymen83.AspectWeaver.Sample.CustomAspect;

[AttributeUsage(AttributeTargets.Method)]
public class CacheAttribute : AspectAttribute
{
    // Define the default order using the convention (Epic 5)
    public const int DefaultOrder = 50;
    public CacheAttribute() { Order = DefaultOrder; }

    public int DurationSeconds { get; set; } = 60;
}