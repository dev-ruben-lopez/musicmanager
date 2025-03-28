// LeasingEtcd.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="dotnet-etcd" Version="1.8.0" />
    <PackageReference Include="Google.Protobuf" Version="3.24.4" />
    <PackageReference Include="Grpc.Core" Version="2.54.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  </ItemGroup>

</Project>

// LeaseState.cs
namespace LeasingEtcd;

public enum LeaseState
{
    Unknown,
    Leader,
    Follower,
    Lost
}

// LeaseOptions.cs
namespace LeasingEtcd;

public class LeaseOptions
{
    public required string EtcdEndpoint { get; init; }
    public required string LeaseKey { get; init; }
    public required string PodId { get; init; }
    public int LeaseTtlSeconds { get; init; } = 15;
    public int RetryDelayMilliseconds { get; init; } = 1000;
}

// ILeaseManager.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeasingEtcd;

public interface ILeaseManager
{
    event Action? OnBecameLeader;
    event Action? OnLostLeadership;
    LeaseState CurrentState { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

// LeaseEtcdManager.cs
using dotnet_etcd;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeasingEtcd;

public class LeaseEtcdManager : ILeaseManager
{
    private readonly LeaseOptions _options;
    private readonly ILogger<LeaseEtcdManager> _logger;
    private EtcdClient? _etcdClient;
    private CancellationTokenSource? _internalCts;
    private Task? _runTask;
    private LeaseState _currentState = LeaseState.Unknown;
    private long _currentLeaseId;

    public event Action? OnBecameLeader;
    public event Action? OnLostLeadership;

    public LeaseState CurrentState => _currentState;

    public LeaseEtcdManager(LeaseOptions options, ILogger<LeaseEtcdManager> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_runTask != null)
            throw new InvalidOperationException("Lease manager already started.");

        _etcdClient = new EtcdClient(_options.EtcdEndpoint);
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = Task.Run(() => RunAsync(_internalCts.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_internalCts != null)
        {
            _internalCts.Cancel();
            try { await _runTask; } catch { }
        }

        _etcdClient?.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await TryToBecomeLeaderAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LeaseEtcdManager] Exception in RunAsync");
            }

            await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
        }
    }

    private async Task TryToBecomeLeaderAsync(CCancellationToken cancellationToken)
    {
        var leaseGrant = _etcdClient!.LeaseGrant(new LeaseGrantRequest { TTL = _options.LeaseTtlSeconds });
        _currentLeaseId = leaseGrant.ID;

        var txnRequest = new TxnRequest();
        txnRequest.Compare.Add(new Compare
        {
            Key = ByteString.CopyFromUtf8(_options.LeaseKey),
            Target = Compare.Types.CompareTarget.Create,
            Result = Compare.Types.CompareResult.Equal
        });
        txnRequest.Success.Add(new RequestOp
        {
            RequestPut = new PutRequest
            {
                Key = ByteString.CopyFromUtf8(_options.LeaseKey),
                Value = ByteString.CopyFromUtf8(_options.PodId),
                Lease = _currentLeaseId
            }
        });

        var txnResponse = _etcdClient.Transaction(txnRequest);

        if (txnResponse.Succeeded)
        {
            _logger.LogInformation("[LeaseEtcdManager] Acquired leadership.");
            UpdateState(LeaseState.Leader);
            _ = Task.Run(() => KeepLeaseAliveAsync(_currentLeaseId, cancellationToken));
        }
        else
        {
            _logger.LogInformation("[LeaseEtcdManager] Leadership unavailable, watching for key release...");
            UpdateState(LeaseState.Follower);
            await WatchForLeaseReleaseAsync(_options.LeaseKey, cancellationToken);
        }
    }

    private async Task KeepLeaseAliveAsync(long leaseId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _etcdClient!.LeaseKeepAlive(leaseId);
                await Task.Delay((_options.LeaseTtlSeconds * 1000) / 2, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LeaseEtcdManager] KeepAlive loop exited unexpectedly");
        }

        UpdateState(LeaseState.Lost);
    }

    private async Task WatchForLeaseReleaseAsync(string leaseKey, CancellationToken cancellationToken)
    {
        try
        {
            var tcs = new TaskCompletionSource();
            long watcherId = _etcdClient!.Watch(leaseKey, watchEvent =>
            {
                foreach (var evt in watchEvent.Events)
                {
                    if (evt.Type == Mvccpb.Event.Types.EventType.Delete)
                    {
                        _logger.LogInformation("[LeaseEtcdManager] Lease key deleted. Retrying leadership...");
                        tcs.TrySetResult();
                        break;
                    }
                }
            });

            using (cancellationToken.Register(() =>
            {
                _etcdClient.DisposeWatcher(watcherId);
                tcs.TrySetCanceled(cancellationToken);
            }))
            {
                await tcs.Task;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[LeaseEtcdManager] Watch cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LeaseEtcdManager] Error in WatchForLeaseReleaseAsync");
        }
    }

    private void UpdateState(LeaseState newState)
    {
        if (_currentState == newState)
            return;

        var oldState = _currentState;
        _currentState = newState;

        if (oldState != LeaseState.Leader && newState == LeaseState.Leader)
        {
            OnBecameLeader?.Invoke();
        }
        else if (oldState == LeaseState.Leader && newState != LeaseState.Leader)
        {
            OnLostLeadership?.Invoke();
        }
    }
}
