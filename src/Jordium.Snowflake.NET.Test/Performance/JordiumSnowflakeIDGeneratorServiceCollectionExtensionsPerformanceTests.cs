using Jordium.Snowflake.NET;
using Jordium.Snowflake.NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Jordium.XUnit.Performance
{
    public class JordiumSnowflakeIDGeneratorServiceCollectionExtensionsPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public JordiumSnowflakeIDGeneratorServiceCollectionExtensionsPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Multi-Worker Performance Tests

        [Fact]
        public void DI_MultipleServiceProviders_DifferentWorkers_ShouldGenerateUniqueIds()
        {
            // Arrange
            var workerCount = 5;
            var dataCenterCount = 3;
            var idsPerProvider = 1000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = workerCount * dataCenterCount * idsPerProvider;
            _output.WriteLine($"DI Multi-Provider Test: {workerCount}W ¡Á {dataCenterCount}DC ¡Á {idsPerProvider}IDs = {totalExpected:N0} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (ushort dc = 0; dc < dataCenterCount; dc++)
            {
                for (ushort w = 0; w < workerCount; w++)
                {
                    var workerId = w;
                    var dataCenterId = dc;

                    tasks.Add(Task.Run(() =>
                    {
                        var services = new ServiceCollection();
                        var configuration = new ConfigurationBuilder().Build();
                        services.AddSingleton<IConfiguration>(configuration);

                        services.AddJordiumSnowflakeIdGenerator(options =>
                        {
                            options.WorkerId = workerId;
                            options.DataCenterId = dataCenterId;
                        });

                        var serviceProvider = services.BuildServiceProvider();
                        var generator = serviceProvider.GetRequiredService<IIDGenerator>();

                        for (int i = 0; i < idsPerProvider; i++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        [Fact]
        public void DI_SingleProvider_HighConcurrency_ShouldGenerateUniqueIds()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 1;
                options.DataCenterId = 1;
            });

            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetRequiredService<IIDGenerator>();

            var threadCount = 20;
            var idsPerThread = 5000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = threadCount * idsPerThread;
            _output.WriteLine($"DI High Concurrency Test: {threadCount} threads ¡Á {idsPerThread:N0} IDs = {totalExpected:N0} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < idsPerThread; i++)
                    {
                        allIds.Add(generator.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Average time per ID: {stopwatch.ElapsedMilliseconds * 1000.0 / totalExpected:F3}¦Ìs");
            _output.WriteLine($"Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region Configuration Source Performance Tests

        [Fact]
        public void DI_WithAppsettingsJson_MultiWorker_ShouldGenerateUniqueIds()
        {
            // Arrange
            var workerConfigs = new List<(ushort workerId, ushort dataCenterId)>
            {
                (1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2)
            };

            var idsPerWorker = 2000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = workerConfigs.Count * idsPerWorker;
            _output.WriteLine($"DI Appsettings Test: {workerConfigs.Count} configurations ¡Á {idsPerWorker:N0} IDs = {totalExpected:N0} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var (workerId, dataCenterId) in workerConfigs)
            {
                var wId = workerId;
                var dcId = dataCenterId;

                tasks.Add(Task.Run(() =>
                {
                    var configData = new Dictionary<string, string>
                    {
                        ["JordiumSnowflakeConfig:WorkerId"] = wId.ToString(),
                        ["JordiumSnowflakeConfig:DataCenterId"] = dcId.ToString(),
                        ["JordiumSnowflakeConfig:Method"] = "1"
                    };

                    var configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection(configData!)
                        .Build();

                    var services = new ServiceCollection();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddJordiumSnowflakeIdGenerator();

                    var serviceProvider = services.BuildServiceProvider();
                    var generator = serviceProvider.GetRequiredService<IIDGenerator>();

                    for (int i = 0; i < idsPerWorker; i++)
                    {
                        allIds.Add(generator.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region Scoped Request Simulation Tests

        [Fact]
        public void DI_SimulateWebRequests_ShouldGenerateUniqueIds()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 1;
                options.DataCenterId = 1;
            });

            var serviceProvider = services.BuildServiceProvider();

            var requestCount = 1000;
            var idsPerRequest = 10;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = requestCount * idsPerRequest;
            _output.WriteLine($"DI Web Request Simulation: {requestCount} requests ¡Á {idsPerRequest} IDs = {totalExpected:N0} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act - Simulate concurrent web requests
            for (int r = 0; r < requestCount; r++)
            {
                tasks.Add(Task.Run(() =>
                {
                    // Each request gets the generator from DI
                    var generator = serviceProvider.GetRequiredService<IIDGenerator>();

                    // Generate IDs for entities in this request
                    for (int i = 0; i < idsPerRequest; i++)
                    {
                        allIds.Add(generator.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Requests per second: {requestCount * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Average time per request: {stopwatch.ElapsedMilliseconds * 1.0 / requestCount:F3}ms");
            _output.WriteLine($"Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region Large Scale Integration Tests

        [Fact]
        public void DI_LargeScale_MultipleProvidersAndWorkers_ShouldGenerateUniqueIds()
        {
            // Arrange
            var providerCount = 10;
            var workersPerProvider = 3;
            var idsPerWorker = 1000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = providerCount * workersPerProvider * idsPerWorker;
            _output.WriteLine($"DI Large Scale Test:");
            _output.WriteLine($"  {providerCount} service providers");
            _output.WriteLine($"  {workersPerProvider} workers per provider");
            _output.WriteLine($"  {idsPerWorker:N0} IDs per worker");
            _output.WriteLine($"  Total expected: {totalExpected:N0} IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int p = 0; p < providerCount; p++)
            {
                for (ushort w = 0; w < workersPerProvider; w++)
                {
                    var providerId = p;
                    var workerId = (ushort)(p * workersPerProvider + w);

                    tasks.Add(Task.Run(() =>
                    {
                        var services = new ServiceCollection();
                        var configuration = new ConfigurationBuilder().Build();
                        services.AddSingleton<IConfiguration>(configuration);

                        services.AddJordiumSnowflakeIdGenerator(options =>
                        {
                            options.WorkerId = workerId;
                            options.DataCenterId = (ushort)providerId;
                        });

                        var serviceProvider = services.BuildServiceProvider();
                        var generator = serviceProvider.GetRequiredService<IIDGenerator>();

                        for (int i = 0; i < idsPerWorker; i++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Results:");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"  Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"  Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"  Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region Memory and Resource Tests

        [Fact]
        public void DI_ManyServiceProviders_ShouldNotLeakMemory()
        {
            // Arrange
            var providerCount = 100;
            var idsPerProvider = 100;
            var allIds = new ConcurrentBag<long>();

            _output.WriteLine($"Memory test: Creating {providerCount} service providers");

            var beforeMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < providerCount; i++)
            {
                // Use unique WorkerId and DataCenterId combination to avoid ID conflicts
                var workerId = (ushort)(i % 32);
                var dataCenterId = (ushort)(i / 32);
                
                tasks.Add(Task.Run(() =>
                {
                    var services = new ServiceCollection();
                    var configuration = new ConfigurationBuilder().Build();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddJordiumSnowflakeIdGenerator(options =>
                    {
                        options.WorkerId = workerId;
                        options.DataCenterId = dataCenterId;
                    });

                    using (var serviceProvider = services.BuildServiceProvider())
                    {
                        var generator = serviceProvider.GetRequiredService<IIDGenerator>();
                        for (int j = 0; j < idsPerProvider; j++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            var afterMemory = GC.GetTotalMemory(true);
            var memoryUsed = afterMemory - beforeMemory;

            // Assert
            var totalExpected = providerCount * idsPerProvider;
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Results:");
            _output.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Memory used: {memoryUsed / 1024.0:N2} KB");
            _output.WriteLine($"  Memory per provider: {memoryUsed / (double)providerCount:N2} bytes");
            _output.WriteLine($"  Total IDs: {allIds.Count:N0}");
            _output.WriteLine($"  Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"  Duplicates: {allIds.Count - uniqueIds.Count}");
            _output.WriteLine($"  WorkerId range: 0-31");
            _output.WriteLine($"  DataCenterId range: 0-{(providerCount - 1) / 32}");

            Assert.Equal(totalExpected, uniqueIds.Count);
            Assert.True(memoryUsed < 100 * 1024 * 1024, $"Memory usage too high: {memoryUsed / 1024.0 / 1024.0:N2} MB");
        }

        #endregion
    }
}
