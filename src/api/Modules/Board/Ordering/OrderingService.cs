using System.Collections.Concurrent;

namespace ProjectTracker.Api.Modules.Board.Ordering;

public sealed class OrderingService
{
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.Ordinal);

    public OrderingServiceResult InsertBetween(string? left, string? right)
    {
        var midpoint = LexoKey.Midpoint(left, right);
        return midpoint.NeedsRebalance
            ? new OrderingServiceResult(string.Empty, true)
            : new OrderingServiceResult(midpoint.Key, false);
    }

    public IReadOnlyList<string> RebalanceScope(int count) => LexoKey.Rebalance(count);

    public static IDisposable AcquireScopeLock(string scopeKey)
    {
        var gate = Locks.GetOrAdd(scopeKey, static _ => new object());
        Monitor.Enter(gate);
        return new ScopeLock(scopeKey, gate);
    }

    public sealed record OrderingServiceResult(string SortOrder, bool NeedsRebalance);

    private sealed class ScopeLock : IDisposable
    {
        private readonly string scopeKey;
        private readonly object gate;
        private bool disposed;

        public ScopeLock(string scopeKey, object gate)
        {
            this.scopeKey = scopeKey;
            this.gate = gate;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Monitor.Exit(gate);
        }
    }
}
