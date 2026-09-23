using Serilog.Core;
using Serilog.Events;

namespace TaskBoard.IntegrationTests.Infrastructure;

/// <summary>Serilog sink that keeps emitted events in memory so tests can assert on logging.</summary>
public sealed class CapturingLogSink : ILogEventSink
{
    private readonly Lock _gate = new();
    private readonly List<LogEvent> _events = [];
    private readonly List<(Func<LogEvent, bool> Predicate, TaskCompletionSource<LogEvent> Completion)> _waiters = [];

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToArray();
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_gate)
        {
            _events.Add(logEvent);

            foreach (var waiter in _waiters.Where(w => w.Predicate(logEvent)).ToList())
            {
                waiter.Completion.TrySetResult(logEvent);
                _waiters.Remove(waiter);
            }
        }
    }

    /// <summary>
    /// Returns the first matching event, waiting for it when it has not been emitted yet. Some
    /// events (such as request completion) are written after the client already received the response.
    /// </summary>
    public Task<LogEvent> WaitForAsync(Func<LogEvent, bool> predicate, TimeSpan timeout)
    {
        var completion = new TaskCompletionSource<LogEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            var existing = _events.FirstOrDefault(predicate);
            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            _waiters.Add((predicate, completion));
        }

        return completion.Task.WaitAsync(timeout);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _events.Clear();
            _waiters.Clear();
        }
    }
}
