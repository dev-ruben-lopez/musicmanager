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

namespace LeasingEtcd
{
    /// <summary>
    /// Manages a leadership lease in etcd using a TTL.
    /// Only one instance (pod) can acquire the leadership lease at a time.
    /// </summary>
    public class LeaseEtcdManager : ILeaseManager
    {
        private readonly LeaseOptions _options;
        private readonly ILogger<LeaseEtcdManager> _logger;
        private EtcdClient? _etcdClient;
        private CancellationTokenSource? _internalCts;
        private Task? _runTask;
        private LeaseState _currentState = LeaseState.Unknown;
        private long _currentLeaseId;

        /// <inheritdoc/>
        public event Action? OnBecameLeader;

        /// <inheritdoc/>
        public event Action? OnLostLeadership;

        /// <inheritdoc/>
        public LeaseState CurrentState => _currentState;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseEtcdManager"/> class.
        /// </summary>
        /// <param name="options">The lease options containing configuration for etcd connection and TTL.</param>
        /// <param name="logger">The logger used to log information and errors.</param>
        public LeaseEtcdManager(LeaseOptions options, ILogger<LeaseEtcdManager> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Starts the lease manager. It initializes the etcd client and begins the leadership acquisition process.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A completed Task.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the lease manager has already been started.</exception>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_runTask != null)
                throw new InvalidOperationException("Lease manager already started.");

            // Create an instance of the EtcdClient using the configured endpoint.
            _etcdClient = new EtcdClient(_options.EtcdEndpoint);

            // Link the provided cancellation token with an internal one.
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start the main run loop in the background.
            _runTask = Task.Run(() => RunAsync(_internalCts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the lease manager by canceling the internal loop and disposing the etcd client.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_internalCts != null)
            {
                // Signal cancellation to the run loop.
                _internalCts.Cancel();
                try
                {
                    // Await the completion of the run loop.
                    await _runTask;
                }
                catch
                {
                    // Ignore exceptions on cancellation.
                }
            }

            // Dispose the etcd client to free resources.
            _etcdClient?.Dispose();
        }

        /// <summary>
        /// The main loop that repeatedly attempts to acquire leadership until cancellation is requested.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation.</param>
        /// <returns>A Task representing the run loop.</returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Attempt to acquire leadership.
                    await TryToBecomeLeaderAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LeaseEtcdManager] Exception in RunAsync");
                }

                // Wait for a specified retry delay before attempting again.
                await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
            }
        }

        /// <summary>
        /// Attempts to acquire leadership by creating a lease and writing the leadership key to etcd.
        /// If successful, starts the keep-alive loop; otherwise, watches for the lease key release.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation.</param>
        /// <returns>A Task representing the attempt.</returns>
        private async Task TryToBecomeLeaderAsync(CancellationToken cancellationToken)
        {
            // Request a new lease with the configured TTL.
            var leaseGrant = _etcdClient!.LeaseGrant(new LeaseGrantRequest { TTL = _options.LeaseTtlSeconds });
            _currentLeaseId = leaseGrant.ID;

            // Build a transaction to create the lease key only if it doesn't exist.
            var txnRequest = new TxnRequest();
            txnRequest.Compare.Add(new Compare
            {
                Key = ByteString.CopyFromUtf8(_options.LeaseKey),
                Target = Compare.Types.CompareTarget.Create, // Compares the creation revision
                Result = Compare.Types.CompareResult.Equal   // Succeeds if the key does not exist (creation revision equals 0)
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

            // Execute the transaction.
            var txnResponse = _etcdClient.Transaction(txnRequest);

            if (txnResponse.Succeeded)
            {
                _logger.LogInformation("[LeaseEtcdManager] Acquired leadership.");
                UpdateState(LeaseState.Leader);

                // Start the keep-alive loop in a background task.
                _ = Task.Run(() => KeepLeaseAliveAsync(_currentLeaseId, cancellationToken));
            }
            else
            {
                _logger.LogInformation("[LeaseEtcdManager] Leadership unavailable, watching for lease key release...");
                UpdateState(LeaseState.Follower);

                // Wait for the lease key to be released (deleted) before retrying.
                await WatchForLeaseReleaseAsync(_options.LeaseKey, cancellationToken);
            }
        }

        /// <summary>
        /// Continuously sends keep-alive requests to etcd to refresh the lease.
        /// If the loop exits (due to cancellation or error), leadership is considered lost.
        /// </summary>
        /// <param name="leaseId">The ID of the current lease.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation.</param>
        /// <returns>A Task representing the keep-alive loop.</returns>
        private async Task KeepLeaseAliveAsync(long leaseId, CancellationToken cancellationToken)
        {
            try
            {
                // Continue sending keep-alive requests until cancellation is requested.
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Send a one-shot keep-alive request.
                    _etcdClient!.LeaseKeepAlive(leaseId);

                    // Wait for half the TTL duration before sending the next keep-alive.
                    await Task.Delay((_options.LeaseTtlSeconds * 1000) / 2, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LeaseEtcdManager] KeepAlive loop exited unexpectedly");
            }

            // Once the keep-alive loop exits, update the state to Lost.
            UpdateState(LeaseState.Lost);
        }

        /// <summary>
        /// Watches the specified lease key in etcd and waits for its deletion.
        /// Once the key is deleted, the task completes, allowing a retry for leadership.
        /// </summary>
        /// <param name="leaseKey">The etcd key representing leadership.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation.</param>
        /// <returns>A Task that completes when the lease key is deleted or cancellation occurs.</returns>
        private async Task WatchForLeaseReleaseAsync(string leaseKey, CancellationToken cancellationToken)
        {
            try
            {
                // Create a TaskCompletionSource to await the delete event.
                var tcs = new TaskCompletionSource();
                // Start watching the lease key.
                long watcherId = _etcdClient!.Watch(leaseKey, watchEvent =>
                {
                    foreach (var evt in watchEvent.Events)
                    {
                        // If the key is deleted, signal completion.
                        if (evt.Type == Mvccpb.Event.Types.EventType.Delete)
                        {
                            _logger.LogInformation("[LeaseEtcdManager] Lease key deleted. Retrying leadership...");
                            tcs.TrySetResult();
                            break;
                        }
                    }
                });

                // If cancellation is requested, dispose the watcher and cancel the TaskCompletionSource.
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
                _logger.LogInformation("[LeaseEtcdManager] Lease watch cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LeaseEtcdManager] Error in WatchForLeaseReleaseAsync");
            }
        }

        /// <summary>
        /// Updates the current state and raises the corresponding events when leadership is gained or lost.
        /// </summary>
        /// <param name="newState">The new leadership state.</param>
        private void UpdateState(LeaseState newState)
        {
            if (_currentState == newState)
                return;

            var oldState = _currentState;
            _currentState = newState;

            // If transitioning to Leader, raise the OnBecameLeader event.
            if (oldState != LeaseState.Leader && newState == LeaseState.Leader)
            {
                OnBecameLeader?.Invoke();
            }
            // If transitioning away from Leader, raise the OnLostLeadership event.
            else if (oldState == LeaseState.Leader && newState != LeaseState.Leader)
            {
                OnLostLeadership?.Invoke();
            }
        }
    }
}
