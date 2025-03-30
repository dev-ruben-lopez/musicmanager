# LeaseEtcdManager

**LeaseEtcdManager** is a .NET 8 library that provides a leader election mechanism using etcd leases. It enables high availability (HA) in distributed systems (e.g., Kubernetes pods) by ensuring that only one instance holds the leadership lease at any given time.

## Features

- **Leader Election:** Only one pod (instance) can acquire the lease and become the leader.
- **Automatic Failover:** If the leader pod fails or loses connectivity, the lease expires and another pod can acquire leadership.
- **Simple Integration:** Configure with a few settings (etcd endpoint, lease key, pod ID, TTL) and subscribe to leadership events.
- **Self-contained:** The library creates and manages its own etcd client internally.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [etcd](https://etcd.io/) (running and accessible)
- The library uses the [dotnet-etcd](https://www.nuget.org/packages/dotnet-etcd) package, so ensure your project has access to it.

### Installation

Clone the repository or add the project to your solution. If you want to use it as a NuGet package, create a package from this project.

### Configuration

Create an instance of `LeaseOptions` with your desired settings. For example:

```csharp
var leaseOptions = new LeaseOptions
{
    EtcdEndpoint = Environment.GetEnvironmentVariable("ETCD_ENDPOINT") ?? "http://etcd:2379",
    LeaseKey = Environment.GetEnvironmentVariable("LEASE_KEY") ?? "/afgaadapter/leader",
    PodId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "default-pod-id",
    LeaseTtlSeconds = 15,
    RetryDelayMilliseconds = 1000
};
