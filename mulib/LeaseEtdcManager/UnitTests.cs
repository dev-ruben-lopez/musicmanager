// File: LeaseEtcdManagerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using LeasingEtcd;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LeasingEtcd.Tests
{
    /// <summary>
    /// Basic unit tests for the LeaseEtcdManager class.
    /// </summary>
    public class LeaseEtcdManagerTests
    {
        /// <summary>
        /// Creates a set of test options for the lease manager.
        /// </summary>
        /// <returns>A LeaseOptions instance with test settings.</returns>
        private LeaseOptions CreateTestOptions() => new LeaseOptions
        {
            // Using localhost; ensure etcd is available on this endpoint for integration tests,
            // or adjust as necessary for your test environment.
            EtcdEndpoint = "http://localhost:2379",
            LeaseKey = "/test/leader",
            PodId = "test-pod-1",
            LeaseTtlSeconds = 5,
            RetryDelayMilliseconds = 500
        };

        /// <summary>
        /// Creates a simple logger for LeaseEtcdManager.
        /// </summary>
        /// <returns>An ILogger instance.</returns>
        private ILogger<LeaseEtcdManager> CreateLogger()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            return loggerFactory.CreateLogger<LeaseEtcdManager>();
        }

        /// <summary>
        /// Verifies that calling StartAsync twice throws an InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task StartAsync_CalledTwice_ThrowsException()
        {
            var options = CreateTestOptions();
            var logger = CreateLogger();
            var leaseManager = new LeaseEtcdManager(options, logger);
            using var cts = new CancellationTokenSource();

            // First call to StartAsync should succeed.
            await leaseManager.StartAsync(cts.Token);

            // A second call to StartAsync should throw an InvalidOperationException.
            await Assert.ThrowsAsync<InvalidOperationException>(() => leaseManager.StartAsync(cts.Token));

            // Clean up by stopping the lease manager.
            await leaseManager.StopAsync(cts.Token);
        }

        /// <summary>
        /// Verifies that calling StopAsync without first calling StartAsync does not throw an exception.
        /// </summary>
        [Fact]
        public async Task StopAsync_WithoutStart_DoesNotThrow()
        {
            var options = CreateTestOptions();
            var logger = CreateLogger();
            var leaseManager = new LeaseEtcdManager(options, logger);

            // Calling StopAsync without starting should not throw any exceptions.
            var exception = await Record.ExceptionAsync(() => leaseManager.StopAsync());
            Assert.Null(exception);
        }
    }
}
