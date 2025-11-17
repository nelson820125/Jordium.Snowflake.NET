using Jordium.Snowflake.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Jordium.XUnit.Performance
{
    public class JordiumSnowflakeIDGeneratorFactoryPerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public JordiumSnowflakeIDGeneratorFactoryPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        public void Dispose()
        {
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        #region Multi-Worker Multi-DataCenter Tests

        [Fact]
        public void MultiWorker_MultiDataCenter_ShouldGenerateUniqueIds()
        {
            // Arrange
            var workerCount = 5;
            var dataCenterCount = 3;
            var idsPerWorker = 1000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            _output.WriteLine($"Testing with {workerCount} workers ¡Á {dataCenterCount} datacenters ¡Á {idsPerWorker} IDs = {workerCount * dataCenterCount * idsPerWorker} total IDs");

            // Act
            for (ushort dc = 0; dc < dataCenterCount; dc++)
            {
                for (ushort w = 0; w < workerCount; w++)
                {
                    var workerId = w;
                    var dataCenterId = dc;

                    tasks.Add(Task.Run(() =>
                    {
                        var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, dataCenterId);
                        for (int i = 0; i < idsPerWorker; i++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            var totalExpected = workerCount * dataCenterCount * idsPerWorker;
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total IDs generated: {allIds.Count}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
            Assert.Equal(0, allIds.Count - uniqueIds.Count);
        }

        [Fact]
        public void MultiWorker_LargeScale_ShouldGenerateUniqueIds()
        {
            // Arrange
            var workerCount = 10;
            var idsPerWorker = 10000;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            _output.WriteLine($"Large scale test: {workerCount} workers ¡Á {idsPerWorker} IDs = {workerCount * idsPerWorker} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (ushort w = 0; w < workerCount; w++)
            {
                var workerId = w;
                tasks.Add(Task.Run(() =>
                {
                    var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, 0);
                    for (int i = 0; i < idsPerWorker; i++)
                    {
                        allIds.Add(generator.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var totalExpected = workerCount * idsPerWorker;
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Total IDs: {allIds.Count}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        [Fact]
        public void MultiWorker_MultiDataCenter_HighConcurrency_ShouldGenerateUniqueIds()
        {
            // Arrange
            var workerCount = 8;
            var dataCenterCount = 4;
            var threadsPerGenerator = 5;
            var idsPerThread = 500;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = workerCount * dataCenterCount * threadsPerGenerator * idsPerThread;
            _output.WriteLine($"High concurrency test: {workerCount}W ¡Á {dataCenterCount}DC ¡Á {threadsPerGenerator}T ¡Á {idsPerThread}IDs = {totalExpected} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (ushort dc = 0; dc < dataCenterCount; dc++)
            {
                for (ushort w = 0; w < workerCount; w++)
                {
                    var workerId = w;
                    var dataCenterId = dc;
                    var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, dataCenterId);

                    for (int t = 0; t < threadsPerGenerator; t++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            for (int i = 0; i < idsPerThread; i++)
                            {
                                allIds.Add(generator.NewLong());
                            }
                        }));
                    }
                }
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Total IDs: {allIds.Count}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region Performance Benchmark Tests

        [Fact]
        public void SingleWorker_PerformanceBenchmark_ShouldGenerateMillionIds()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var targetCount = 1_000_000;
            var ids = new long[targetCount];

            _output.WriteLine($"Performance benchmark: Generating {targetCount:N0} IDs with single worker");

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < targetCount; i++)
            {
                ids[i] = generator.NewLong();
            }
            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(ids);

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average time per ID: {stopwatch.ElapsedMilliseconds * 1000.0 / targetCount:F3}¦Ìs");
            _output.WriteLine($"IDs per second: {targetCount * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");

            Assert.Equal(targetCount, uniqueIds.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Generation took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void MultiWorker_ParallelPerformance_ShouldScaleLinearly()
        {
            // Arrange
            var workerCounts = new[] { 1, 2, 4, 8 };
            var idsPerWorker = 100000;
            var results = new Dictionary<int, double>();

            _output.WriteLine($"Testing parallel performance scaling with {idsPerWorker:N0} IDs per worker");
            _output.WriteLine("Workers | Total IDs | Time(ms) | IDs/sec");
            _output.WriteLine("--------|-----------|----------|----------");

            foreach (var workerCount in workerCounts)
            {
                var allIds = new ConcurrentBag<long>();
                var tasks = new List<Task>();

                var stopwatch = Stopwatch.StartNew();

                for (ushort w = 0; w < workerCount; w++)
                {
                    var workerId = w;
                    tasks.Add(Task.Run(() =>
                    {
                        var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, 0);
                        for (int i = 0; i < idsPerWorker; i++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                stopwatch.Stop();

                var totalIds = workerCount * idsPerWorker;
                var idsPerSecond = totalIds * 1000.0 / stopwatch.ElapsedMilliseconds;
                results[workerCount] = idsPerSecond;

                _output.WriteLine($"{workerCount,7} | {totalIds,9:N0} | {stopwatch.ElapsedMilliseconds,8} | {idsPerSecond,10:N0}");

                // Verify uniqueness
                var uniqueIds = new HashSet<long>(allIds);
                Assert.Equal(totalIds, uniqueIds.Count);
            }

            // Assert: Performance should improve with more workers
            Assert.True(results[2] > results[1] * 1.5, "2 workers should be at least 1.5x faster than 1 worker");
        }

        #endregion

        #region Stress Tests

        [Fact]
        public void StressTest_MaxWorkers_MaxDataCenters_ShouldGenerateUniqueIds()
        {
            // Arrange
            var maxWorkers = 32; // 2^5
            var maxDataCenters = 32; // 2^5
            var idsPerGenerator = 100;
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            var totalExpected = maxWorkers * maxDataCenters * idsPerGenerator;
            _output.WriteLine($"Stress test with maximum configuration:");
            _output.WriteLine($"  Workers: {maxWorkers}");
            _output.WriteLine($"  DataCenters: {maxDataCenters}");
            _output.WriteLine($"  IDs per generator: {idsPerGenerator}");
            _output.WriteLine($"  Total expected IDs: {totalExpected:N0}");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (ushort dc = 0; dc < maxDataCenters; dc++)
            {
                for (ushort w = 0; w < maxWorkers; w++)
                {
                    var workerId = w;
                    var dataCenterId = dc;

                    tasks.Add(Task.Run(() =>
                    {
                        var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, dataCenterId);
                        for (int i = 0; i < idsPerGenerator; i++)
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

        [Fact]
        public void StressTest_BurstGeneration_ShouldHandleHighLoad()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var burstCount = 10;
            var idsPerBurst = 10000;
            var allIds = new ConcurrentBag<long>();

            _output.WriteLine($"Burst generation test: {burstCount} bursts ¡Á {idsPerBurst:N0} IDs = {burstCount * idsPerBurst:N0} total IDs");

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int burst = 0; burst < burstCount; burst++)
            {
                var tasks = new List<Task>();

                for (int t = 0; t < 10; t++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        for (int i = 0; i < idsPerBurst / 10; i++)
                        {
                            allIds.Add(generator.NewLong());
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }

            stopwatch.Stop();

            // Assert
            var uniqueIds = new HashSet<long>(allIds);
            var totalExpected = burstCount * idsPerBurst;

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IDs per second: {totalExpected * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
            _output.WriteLine($"Unique IDs: {uniqueIds.Count:N0}");
            _output.WriteLine($"Duplicates: {allIds.Count - uniqueIds.Count}");

            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(totalExpected, uniqueIds.Count);
        }

        #endregion

        #region ID Distribution Tests

        [Fact]
        public void MultiWorker_IdDistribution_ShouldBeDifferentiable()
        {
            // Arrange
            var workerCount = 5;
            var idsPerWorker = 1000;
            var workerIdBuckets = new Dictionary<ushort, List<long>>();

            for (ushort w = 0; w < workerCount; w++)
            {
                workerIdBuckets[w] = new List<long>();
            }

            // Act
            var tasks = new List<Task>();
            for (ushort w = 0; w < workerCount; w++)
            {
                var workerId = w;
                tasks.Add(Task.Run(() =>
                {
                    var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, 0);
                    for (int i = 0; i < idsPerWorker; i++)
                    {
                        lock (workerIdBuckets[workerId])
                        {
                            workerIdBuckets[workerId].Add(generator.NewLong());
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            _output.WriteLine("ID distribution by worker:");
            _output.WriteLine("Worker | Count | Min ID | Max ID");
            _output.WriteLine("-------|-------|--------|-------");

            foreach (var bucket in workerIdBuckets.OrderBy(x => x.Key))
            {
                var workerId = bucket.Key;
                var ids = bucket.Value;
                _output.WriteLine($"{workerId,6} | {ids.Count,5} | {ids.Min()} | {ids.Max()}");

                Assert.Equal(idsPerWorker, ids.Count);
                Assert.Equal(idsPerWorker, ids.Distinct().Count());
            }

            // Verify no overlap between workers
            var allIds = workerIdBuckets.SelectMany(x => x.Value).ToList();
            Assert.Equal(workerCount * idsPerWorker, allIds.Distinct().Count());
        }

        #endregion
    }
}
