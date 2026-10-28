using Jordium.Snowflake.NET;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Jordium.XUnit.Performance
{
    public class SnowflakeIdPopulatorPerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public SnowflakeIdPopulatorPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        public void Dispose()
        {
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        private class Order
        {
            [SnowflakeID]
            public long Id { get; set; }

            [SnowflakeID]
            public string TraceCode { get; set; } = string.Empty;

            public decimal Amount { get; set; }
        }

        [Fact]
        public void Populate_FirstCallVsCachedCall_ShouldShowCacheSpeedup()
        {
            // Arrange: use an explicit generator instance to avoid relying on the
            // process-wide Default singleton, which may be mutated concurrently by
            // other test classes running in parallel.
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 30, dataCenterId: 1);
            var warmupOrder = new Order();

            // Act: first call triggers reflection scan + cache population
            var firstCallWatch = Stopwatch.StartNew();
            SnowflakeIdPopulator.Populate(warmupOrder, generator);
            firstCallWatch.Stop();

            // Act: subsequent calls should hit the type-level cache
            const int iterations = 100_000;
            var cachedWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                SnowflakeIdPopulator.Populate(new Order(), generator);
            }
            cachedWatch.Stop();

            var avgCachedMicroseconds = cachedWatch.Elapsed.TotalMilliseconds * 1000 / iterations;

            _output.WriteLine($"First call (reflection scan): {firstCallWatch.Elapsed.TotalMilliseconds:F4} ms");
            _output.WriteLine($"{iterations} cached calls total: {cachedWatch.Elapsed.TotalMilliseconds:F2} ms");
            _output.WriteLine($"Average per cached call: {avgCachedMicroseconds:F3} us");

            // Assert: cached per-call cost should stay well below 50 microseconds on typical hardware
            Assert.True(avgCachedMicroseconds < 50, $"Cached populate call too slow: {avgCachedMicroseconds:F3} us");
        }

        [Fact]
        public void PopulateMany_WithLargeBatch_ShouldGenerateUniqueIdsEfficiently()
        {
            // Arrange: use an explicit generator instance (avoids Default singleton contention
            // with other test classes running in parallel)
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 31, dataCenterId: 1);
            const int count = 200_000;
            var orders = Enumerable.Range(0, count).Select(_ => new Order()).ToList();

            // Act
            var watch = Stopwatch.StartNew();
            SnowflakeIdPopulator.PopulateMany(orders, generator);
            watch.Stop();

            var throughput = count / watch.Elapsed.TotalSeconds;
            _output.WriteLine($"PopulateMany: {count} entities in {watch.Elapsed.TotalMilliseconds:F2} ms ({throughput:N0} ops/sec)");

            // Assert
            Assert.Equal(count, orders.Select(o => o.Id).Distinct().Count());
            Assert.True(throughput > 100_000, $"Throughput too low: {throughput:N0} ops/sec");
        }

        [Fact]
        public void TryPopulateProperty_RepeatedCalls_ShouldAvoidRepeatedAttributeReflection()
        {
            // Arrange: simulates an ORM insert callback invoked per-row, per-property
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 2, dataCenterId: 2);
            var property = typeof(Order).GetProperty(nameof(Order.Id))!;
            const int iterations = 100_000;

            // Act
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var order = new Order();
                SnowflakeIdPopulator.TryPopulateProperty(order, property, generator);
            }
            watch.Stop();

            var avgMicroseconds = watch.Elapsed.TotalMilliseconds * 1000 / iterations;
            _output.WriteLine($"TryPopulateProperty average: {avgMicroseconds:F3} us/call over {iterations} calls");

            // Assert
            Assert.True(avgMicroseconds < 20, $"TryPopulateProperty too slow: {avgMicroseconds:F3} us");
        }
    }
}
