namespace LeaseEtcdManager;

public class LeaseOptions
{
    public required string EtcdEndpoint { get; init; }
    public required string LeaseKey { get; init; }
    public required string PodId { get; init; }
    public int LeaseTtlSeconds { get; init; } = 15;
    public int RetryDelayMilliseconds { get; init; } = 1000;
}