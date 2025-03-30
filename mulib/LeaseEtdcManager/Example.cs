using LeaseEtcdManager;
using Microsoft.Extensions.Logging;

// Configure logger (for example purposes only; use your own logging configuration)
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var leaseLogger = loggerFactory.CreateLogger<LeaseEtcdManager>();

// Configure lease options as shown above
var leaseOptions = new LeaseOptions
{
    EtcdEndpoint = "http://etcd:2379",
    LeaseKey = "/afgaadapter/leader",
    PodId = "my-pod-01",
    LeaseTtlSeconds = 15,
    RetryDelayMilliseconds = 1000
};

// Create the lease manager instance
var leaseManager = new LeaseEtcdManager(leaseOptions, leaseLogger);

// Subscribe to leadership events
leaseManager.OnBecameLeader += () =>
{
    Console.WriteLine("This instance is now the leader. Starting business logic...");
    // Call your logic to start processing, message sending, etc.
};

leaseManager.OnLostLeadership += () =>
{
    Console.WriteLine("Leadership lost. Stopping business logic...");
    // Call your logic to stop processing, message sending, etc.
};

// Start the lease manager (typically in an async context)
using var cancellationTokenSource = new CancellationTokenSource();
await leaseManager.StartAsync(cancellationTokenSource.Token);

// Keep the application running (for demonstration)
Console.WriteLine("Press Ctrl+C to exit.");
Console.ReadLine();

// On shutdown, stop the lease manager
await leaseManager.StopAsync();
