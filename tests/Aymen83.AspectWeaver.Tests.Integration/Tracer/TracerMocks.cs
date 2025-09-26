namespace Aymen83.AspectWeaver.Tests.Integration.Tracer;

/// <summary>
/// Interface used to mock and verify aspect execution.
/// </summary>
public interface ITracerMock
{
    void Trace(string message);
}