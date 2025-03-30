namespace LeaseEtcdManager;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface ILeaseManager
{
    event Action? OnBecameLeader;
    event Action? OnLostLeadership;
    LeaseState CurrentState { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}