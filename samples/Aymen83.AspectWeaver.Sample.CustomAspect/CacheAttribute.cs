using Aymen83.AspectWeaver.Abstractions;

namespace Aymen83.AspectWeaver.Sample.CustomAspect;

[AttributeUsage(AttributeTargets.Method)]
public class CacheAttribute : AspectAttribute
{
    // Defines the execution order of the aspect. Lower numbers execute first.
    public const int DefaultOrder = 50;
    public CacheAttribute() { Order = DefaultOrder; }

    public int DurationSeconds { get; set; } = 60;
}