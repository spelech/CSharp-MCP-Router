namespace McpRouter.Core.Routing
{
    public partial class SessionManager
    {
        public DateTime StartTime { get; } = DateTime.UtcNow;
        private long _totalRequests = 0;
        public long TotalRequests => _totalRequests;

        private long _totalInputTokens = 0;
        private long _totalOutputTokens = 0;
        private long _totalDurationMs = 0;

        public long TotalInputTokens => _totalInputTokens;
        public long TotalOutputTokens => _totalOutputTokens;
        public long TotalDurationMs => _totalDurationMs;

        public void AddPerformanceMetrics(long inputTokens, long outputTokens, long durationMs)
        {
            Interlocked.Add(ref _totalInputTokens, inputTokens);
            Interlocked.Add(ref _totalOutputTokens, outputTokens);
            Interlocked.Add(ref _totalDurationMs, durationMs);
        }

        public void IncrementTotalRequests()
        {
            Interlocked.Increment(ref _totalRequests);
        }
    }
}
