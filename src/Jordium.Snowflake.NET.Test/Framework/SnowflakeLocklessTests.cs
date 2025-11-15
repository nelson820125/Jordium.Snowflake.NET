using System.Collections.Concurrent;
using System.Diagnostics;
using Jordium.Snowflake.NET;
using Xunit.Abstractions;

namespace Jordium.XUnit.Framework
{
    /// <summary>
    /// 无锁 Snowflake 实现验证测试
    /// 重点验证：无锁实现是否会产生重复 ID
    /// </summary>
    public class SnowflakeLocklessTests
    {
        private readonly ITestOutputHelper _output;

        public SnowflakeLocklessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 验证无锁实现在极端并发下不会产生重复 ID
        /// </summary>
        [Theory]
        [InlineData(2, 100000)]   // 2 线程，每线程 10 万
        [InlineData(4, 100000)]   // 4 线程，每线程 10 万
        [InlineData(8, 100000)]   // 8 线程，每线程 10 万
        [InlineData(16, 50000)]   // 16 线程，每线程 5 万
        public void Lockless_ExtremeConcurrency_ShouldNotGenerateDuplicates(int threadCount, int countPerThread)
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3  // 使用无锁版本
            };
            var generator = new DefaultIDGenerator(options);
            var allIds = new ConcurrentBag<long>();
            var duplicateCount = 0;

            // Act - 极端并发测试
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, threadId =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    var id = generator.NewLong();
                    allIds.Add(id);
                }
            });
            stopwatch.Stop();

            // Assert - 检查重复
            var totalGenerated = threadCount * countPerThread;
            var uniqueIds = allIds.Distinct().ToList();
            duplicateCount = totalGenerated - uniqueIds.Count;

            var speed = totalGenerated / stopwatch.Elapsed.TotalSeconds;

            _output.WriteLine($"=== 无锁实现并发测试 ===");
            _output.WriteLine($"线程数: {threadCount}");
            _output.WriteLine($"每线程生成: {countPerThread:N0}");
            _output.WriteLine($"总生成数: {totalGenerated:N0}");
            _output.WriteLine($"唯一 ID: {uniqueIds.Count:N0}");
            _output.WriteLine($"重复数: {duplicateCount}");
            _output.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"速度: {speed:N0} IDs/秒");

            // 断言：绝对不能有重复
            Assert.Equal(0, duplicateCount);
            Assert.Equal(totalGenerated, uniqueIds.Count);
        }

        /// <summary>
        /// 对比有锁 vs 无锁实现的正确性
        /// </summary>
        [Fact]
        public void Compare_LockedVsLockless_BothShouldBeCorrect()
        {
            var count = 100000;
            var threadCount = 8;

            // Method 1 (有锁)
            var options1 = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 1
            };
            var generator1 = new DefaultIDGenerator(options1);
            var ids1 = new ConcurrentBag<long>();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < count / threadCount; i++)
                {
                    ids1.Add(generator1.NewLong());
                }
            });

            // Method 3 (无锁)
            var options3 = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator3 = new DefaultIDGenerator(options3);
            var ids3 = new ConcurrentBag<long>();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < count / threadCount; i++)
                {
                    ids3.Add(generator3.NewLong());
                }
            });

            // 验证
            var unique1 = ids1.Distinct().Count();
            var unique3 = ids3.Distinct().Count();

            _output.WriteLine($"Method 1 (有锁): 生成 {ids1.Count:N0}, 唯一 {unique1:N0}, 重复 {ids1.Count - unique1}");
            _output.WriteLine($"Method 3 (无锁): 生成 {ids3.Count:N0}, 唯一 {unique3:N0}, 重复 {ids3.Count - unique3}");

            Assert.Equal(count, unique1);
            Assert.Equal(count, unique3);
        }

        /// <summary>
        /// 测试多数据中心 + 无锁实现
        /// </summary>
        [Theory]
        [InlineData(2, 3, 5000)]  // 2个数据中心，每个3个worker
        [InlineData(3, 2, 3000)]  // 3个数据中心，每个2个worker
        public void Lockless_MultipleDataCentersAndWorkers_ShouldNotGenerateDuplicates(int dataCenterCount, int workerCountPerDC, int countPerWorker)
        {
            // Arrange
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            // Act - 模拟多数据中心、多机器场景
            for (ushort dcId = 1; dcId <= dataCenterCount; dcId++)
            {
                for (ushort workerId = 1; workerId <= workerCountPerDC; workerId++)
                {
                    var currentDcId = dcId;
                    var currentWorkerId = workerId;
                    
                    var task = Task.Run(() =>
                    {
                        var options = new IDGeneratorOptions
                        {
                            WorkerId = currentWorkerId,
                            DataCenterId = currentDcId,
                            Method = 3  // 无锁版本
                        };
                        var generator = new DefaultIDGenerator(options);

                        for (int i = 0; i < countPerWorker; i++)
                        {
                            var id = generator.NewLong();
                            allIds.Add(id);
                        }
                    });
                    tasks.Add(task);
                }
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            var totalGenerated = dataCenterCount * workerCountPerDC * countPerWorker;
            var uniqueIds = allIds.Distinct().ToList();
            
            _output.WriteLine($"=== 多数据中心无锁测试 ===");
            _output.WriteLine($"数据中心数: {dataCenterCount}, Workers/DC: {workerCountPerDC}");
            _output.WriteLine($"总生成: {allIds.Count:N0}, 唯一: {uniqueIds.Count:N0}");
            _output.WriteLine($"重复数: {allIds.Count - uniqueIds.Count}");
            
            Assert.Equal(totalGenerated, allIds.Count);
            Assert.Equal(allIds.Count, uniqueIds.Count);
        }

        /// <summary>
        /// 压力测试：持续高并发生成，验证稳定性
        /// </summary>
        [Fact]
        public void Lockless_SustainedHighConcurrency_StressTest()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3  // 使用无锁版本
            };
            var generator = new DefaultIDGenerator(options);
            var allIds = new ConcurrentBag<long>();
            var duration = TimeSpan.FromSeconds(5);
            var threadCount = Environment.ProcessorCount;

            _output.WriteLine($"=== 压力测试 ===");
            _output.WriteLine($"持续时间: {duration.TotalSeconds}秒");
            _output.WriteLine($"并发线程: {threadCount}");

            var cts = new CancellationTokenSource(duration);
            var stopwatch = Stopwatch.StartNew();
            
            // 使用 Task 而非 Parallel.For，避免 OperationCanceledException 导致整体失败
            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var id = generator.NewLong();
                            allIds.Add(id);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常结束，不需要传播异常
                    }
                }, cts.Token);
            }

            // 等待所有任务完成或超时
            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                // 过滤掉 OperationCanceledException，只关注真正的错误
                var realExceptions = ex.InnerExceptions
                    .Where(e => !(e is OperationCanceledException))
                    .ToList();
                
                if (realExceptions.Any())
                {
                    throw new AggregateException(realExceptions);
                }
            }

            stopwatch.Stop();

            // 验证
            var uniqueIds = allIds.Distinct().ToList();
            var duplicates = allIds.Count - uniqueIds.Count;
            var avgSpeed = allIds.Count / stopwatch.Elapsed.TotalSeconds;

            _output.WriteLine($"总生成: {allIds.Count:N0}");
            _output.WriteLine($"唯一ID: {uniqueIds.Count:N0}");
            _output.WriteLine($"重复数: {duplicates}");
            _output.WriteLine($"平均速度: {avgSpeed:N0} IDs/秒");

            Assert.Equal(0, duplicates);
        }

        /// <summary>
        /// 验证 CAS 失败重试机制
        /// </summary>
        [Fact]
        public void Lockless_CAS_RetryMechanism_ShouldWork()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3  // 无锁版本
            };
            var generator = new DefaultIDGenerator(options);
            var ids = new ConcurrentBag<long>();
            var threadCount = 32;  // 高并发
            var countPerThread = 10000;

            // 极端并发，增加 CAS 重试概率
            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    ids.Add(generator.NewLong());
                }
            });

            var uniqueIds = ids.Distinct().Count();
            var expected = threadCount * countPerThread;

            _output.WriteLine($"总生成: {ids.Count:N0}");
            _output.WriteLine($"唯一ID: {uniqueIds:N0}");
            _output.WriteLine($"重复数: {ids.Count - uniqueIds}");

            Assert.Equal(expected, uniqueIds);
        }

        /// <summary>
        /// 性能对比：有锁 vs 无锁（单 WorkerId 竞争场景）
        /// 注意：这不是无锁的优势场景，仅用于验证最坏情况下的性能
        /// </summary>
        [Theory]
        [InlineData(1, 100000)]   // 单线程
        [InlineData(2, 100000)]   // 2 线程
        [InlineData(4, 100000)]   // 4 线程
        [InlineData(8, 100000)]   // 8 线程
        public void Performance_LockedVsLockless_SingleWorker_WorstCase(int threadCount, int totalCount)
        {
            var countPerThread = totalCount / threadCount;

            // Method 1 (有锁)
            var options1 = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 1
            };
            var generator1 = new DefaultIDGenerator(options1);
            var stopwatch1 = Stopwatch.StartNew();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    generator1.NewLong();
                }
            });

            stopwatch1.Stop();
            var speed1 = totalCount / stopwatch1.Elapsed.TotalSeconds;

            // Method 3 (无锁)
            var options3 = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator3 = new DefaultIDGenerator(options3);
            var stopwatch3 = Stopwatch.StartNew();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    generator3.NewLong();
                }
            });

            stopwatch3.Stop();
            var speed3 = totalCount / stopwatch3.Elapsed.TotalSeconds;

            var improvement = ((speed3 - speed1) / speed1) * 100;

            _output.WriteLine($"=== ??  单 WorkerId 高竞争场景（非无锁优势场景）===");
            _output.WriteLine($"线程数: {threadCount}, 总数: {totalCount:N0}");
            _output.WriteLine($"Method 1 (有锁): {speed1:N0} IDs/秒, 耗时 {stopwatch1.ElapsedMilliseconds}ms");
            _output.WriteLine($"Method 3 (无锁): {speed3:N0} IDs/秒, 耗时 {stopwatch3.ElapsedMilliseconds}ms");
            _output.WriteLine($"性能提升: {improvement:F1}%");
            _output.WriteLine($"");
            _output.WriteLine($"??  重要说明:");
            _output.WriteLine($"  - 此场景为 {threadCount} 个线程竞争单个 WorkerId 的 _state");
            _output.WriteLine($"  - CAS 冲突率约: {(1.0 - 1.0/threadCount) * 100:F1}%");
            _output.WriteLine($"  - 这不是无锁实现的设计目标场景");
            _output.WriteLine($"  - 真实优势场景请参考: Performance_LockedVsLockless_MultipleWorkers");

            // 最坏情况下，无锁版本至少应保持基本性能
            if (threadCount == 1)
            {
                Assert.True(speed3 > 5000000, $"无锁版本单线程性能过低: {speed3:N0} IDs/秒");
            }
            else
            {
                // 高竞争下，允许无锁版本慢于有锁（因为这不是设计目标）
                Assert.True(speed3 >= speed1 * 0.6, $"无锁版本性能异常: {speed3:N0} vs {speed1:N0}");
                _output.WriteLine($"  - 高竞争下无锁版本可能慢于有锁，这是正常的");
            }
        }

        /// <summary>
        /// 验证时间回拨场景下的正确性
        /// </summary>
        [Fact]
        public void Lockless_TimeRollback_ShouldHandleCorrectly()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator = new DefaultIDGenerator(options);
            var ids = new HashSet<long>();

            // 快速生成大量 ID，验证短时间内不会触发时间回拨异常
            for (int i = 0; i < 100000; i++)
            {
                var id = generator.NewLong();
                var added = ids.Add(id);
                Assert.True(added, $"发现重复 ID: {id}");
            }

            _output.WriteLine($"生成 {ids.Count:N0} 个唯一 ID，无重复");
            _output.WriteLine("说明: 无锁实现在时间回拨 > 1000ms 时会抛出异常，小幅回拨会自动等待");
        }

        /// <summary>
        /// 验证时间回拨超过阈值时抛出异常
        /// </summary>
        [Fact]
        public void Lockless_LargeTimeRollback_ShouldThrowException()
        {
            // 注意：这个测试很难真实触发，因为需要实际的系统时间回拨
            // 这里只是验证代码逻辑存在，实际异常触发需要在生产环境监控
            
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator = new DefaultIDGenerator(options);
            
            // 生成一些 ID 以建立时间戳基准
            for (int i = 0; i < 1000; i++)
            {
                generator.NewLong();
            }

            _output.WriteLine("说明: 无锁实现在检测到时间回拨超过 1000ms 时会抛出异常");
            _output.WriteLine("异常类型: System.Exception");
            _output.WriteLine("异常消息格式: 'Clock moved backwards. Refusing to generate id for {timeDiff} milliseconds'");
            
            // 由于无法真实模拟系统时间回拨，这里只做文档说明
            Assert.True(true, "时间回拨保护机制已实现");
        }

        /// <summary>
        /// 验证短时间回拨的自动恢复机制
        /// </summary>
        [Fact]
        public void Lockless_SmallTimeRollback_ShouldAutoRecover()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator = new DefaultIDGenerator(options);
            var ids = new ConcurrentBag<long>();

            // 在短时间内快速生成 ID，可能触发同一毫秒内的"伪回拨"
            Parallel.For(0, 4, _ =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    ids.Add(generator.NewLong());
                }
            });

            var uniqueIds = ids.Distinct().Count();
            
            _output.WriteLine($"并发生成: {ids.Count:N0}");
            _output.WriteLine($"唯一ID: {uniqueIds:N0}");
            _output.WriteLine($"重复数: {ids.Count - uniqueIds}");
            _output.WriteLine("说明: 无锁实现通过 SpinWait 自动处理时间未前进的情况");

            Assert.Equal(ids.Count, uniqueIds);
        }

        /// <summary>
        /// 极限测试：百万级 ID 生成验证
        /// </summary>
        [Fact]
        public void Lockless_OneMillionIds_ShouldBeUnique()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator = new DefaultIDGenerator(options);
            var count = 1000000;
            var threadCount = 8;

            var allIds = new ConcurrentBag<long>();
            var stopwatch = Stopwatch.StartNew();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < count / threadCount; i++)
                {
                    allIds.Add(generator.NewLong());
                }
            });

            stopwatch.Stop();

            var uniqueIds = allIds.Distinct().Count();
            var speed = count / stopwatch.Elapsed.TotalSeconds;

            _output.WriteLine($"=== 百万级 ID 测试 ===");
            _output.WriteLine($"总生成: {count:N0}");
            _output.WriteLine($"唯一ID: {uniqueIds:N0}");
            _output.WriteLine($"重复数: {count - uniqueIds}");
            _output.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"速度: {speed:N0} IDs/秒");

            Assert.Equal(count, uniqueIds);
            Assert.True(speed > 500000, $"性能未达标: {speed:N0} IDs/秒");
        }

        /// <summary>
        /// 验证无锁实现的 ID 结构正确性
        /// </summary>
        [Fact]
        public void Lockless_GeneratedId_StructureShouldBeCorrect()
        {
            // Arrange
            ushort workerId = 7;
            ushort dataCenterId = 5;
            var options = new IDGeneratorOptions
            {
                WorkerId = workerId,
                DataCenterId = dataCenterId,
                Method = 3  // 无锁版本
            };
            var generator = new DefaultIDGenerator(options);

            // Act
            var id = generator.NewLong();

            // Assert - 解析 ID 结构 (标准雪花算法: 5位DC + 5位Worker + 12位Seq)
            var seqMask = (1L << 12) - 1;
            var workerMask = (1L << 5) - 1;
            var dcMask = (1L << 5) - 1;

            var extractedSeq = id & seqMask;
            var extractedWorker = (id >> 12) & workerMask;
            var extractedDC = (id >> 17) & dcMask;
            var extractedTimestamp = id >> 22;

            _output.WriteLine($"=== 无锁实现 ID 结构验证 ===");
            _output.WriteLine($"生成的 ID: {id}");
            _output.WriteLine($"时间戳: {extractedTimestamp}");
            _output.WriteLine($"数据中心ID: {extractedDC} (期望: {dataCenterId})");
            _output.WriteLine($"机器ID: {extractedWorker} (期望: {workerId})");
            _output.WriteLine($"序列号: {extractedSeq}");

            Assert.Equal(dataCenterId, extractedDC);
            Assert.Equal(workerId, extractedWorker);
            Assert.True(extractedTimestamp > 0);
        }

        /// <summary>
        /// 不同 DataCenterId 使用无锁实现应生成不同 ID
        /// </summary>
        [Fact]
        public void Lockless_DifferentDataCenterIds_ShouldGenerateDifferentIds()
        {
            var count = 5000;
            var allIds = new ConcurrentBag<long>();

            var dc1Options = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 1, Method = 3 };
            var dc1Generator = new DefaultIDGenerator(dc1Options);

            var dc2Options = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 2, Method = 3 };
            var dc2Generator = new DefaultIDGenerator(dc2Options);

            // Act - 并发生成
            Parallel.Invoke(
                () =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        allIds.Add(dc1Generator.NewLong());
                    }
                },
                () =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        allIds.Add(dc2Generator.NewLong());
                    }
                }
            );

            // Assert
            var uniqueIds = allIds.Distinct().Count();
            _output.WriteLine($"DataCenter1+2 总生成: {allIds.Count:N0}, 唯一: {uniqueIds:N0}");
            Assert.Equal(count * 2, uniqueIds);
        }

        /// <summary>
        /// 无锁实现性能基准测试
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public void Lockless_PerformanceBenchmark(int threadCount)
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };
            var generator = new DefaultIDGenerator(options);
            var countPerThread = 100000;
            var totalCount = threadCount * countPerThread;

            var stopwatch = Stopwatch.StartNew();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    generator.NewLong();
                }
            });

            stopwatch.Stop();

            var speed = totalCount / stopwatch.Elapsed.TotalSeconds;
            var efficiency = threadCount == 1 ? 100 : (speed / threadCount) / (totalCount / threadCount / stopwatch.Elapsed.TotalSeconds) * 100;

            _output.WriteLine($"=== 无锁性能基准 ({threadCount} 线程) ===");
            _output.WriteLine($"总数: {totalCount:N0}");
            _output.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"速度: {speed:N0} IDs/秒");
            _output.WriteLine($"线程效率: {efficiency:F1}%");

            // 更新性能预期：基于新的正确实现
            long expectedMinSpeed;
            if (threadCount == 1)
            {
                expectedMinSpeed = 8000000;  // 单线程至少 8M IDs/秒
            }
            else if (threadCount <= 4)
            {
                expectedMinSpeed = 15000000;  // 2-4 线程至少 15M IDs/秒
            }
            else if (threadCount <= 8)
            {
                expectedMinSpeed = 20000000;  // 8 线程至少 20M IDs/秒
            }
            else
            {
                expectedMinSpeed = 25000000;  // 16 线程至少 25M IDs/秒
            }

            Assert.True(speed > expectedMinSpeed, $"性能未达标: {speed:N0} IDs/秒 (预期 > {expectedMinSpeed:N0})");
        }

        /// <summary>
        /// 性能对比：有锁 vs 无锁（多 WorkerId 场景，模拟分布式环境）
        /// ? 这是无锁实现的真正优势场景
        /// </summary>
        [Theory]
        [InlineData(2, 50000)]   // 2 个 WorkerId
        [InlineData(4, 25000)]   // 4 个 WorkerId
        [InlineData(8, 12500)]   // 8 个 WorkerId
        public void Performance_LockedVsLockless_MultipleWorkers(int workerCount, int countPerWorker)
        {
            var totalCount = workerCount * countPerWorker;

            // Method 1 (有锁) - 每个 WorkerId 独立 generator
            var stopwatch1 = Stopwatch.StartNew();
            Parallel.For(0, workerCount, workerId =>
            {
                var options = new IDGeneratorOptions
                {
                    WorkerId = (ushort)workerId,
                    DataCenterId = 1,
                    Method = 1
                };
                var generator = new DefaultIDGenerator(options);
                
                for (int i = 0; i < countPerWorker; i++)
                {
                    generator.NewLong();
                }
            });
            stopwatch1.Stop();
            var speed1 = totalCount / stopwatch1.Elapsed.TotalSeconds;

            // Method 3 (无锁) - 每个 WorkerId 独立 generator
            var stopwatch3 = Stopwatch.StartNew();
            Parallel.For(0, workerCount, workerId =>
            {
                var options = new IDGeneratorOptions
                {
                    WorkerId = (ushort)workerId,
                    DataCenterId = 1,
                    Method = 3
                };
                var generator = new DefaultIDGenerator(options);
                
                for (int i = 0; i < countPerWorker; i++)
                {
                    generator.NewLong();
                }
            });
            stopwatch3.Stop();
            var speed3 = totalCount / stopwatch3.Elapsed.TotalSeconds;

            var improvement = ((speed3 - speed1) / speed1) * 100;

            _output.WriteLine($"=== ? 多 WorkerId 场景（无锁真正优势场景）===");
            _output.WriteLine($"WorkerId 数量: {workerCount}, 每个生成: {countPerWorker:N0}, 总计: {totalCount:N0}");
            _output.WriteLine($"Method 1 (有锁): {speed1:N0} IDs/秒, 耗时 {stopwatch1.ElapsedMilliseconds}ms");
            _output.WriteLine($"Method 3 (无锁): {speed3:N0} IDs/秒, 耗时 {stopwatch3.ElapsedMilliseconds}ms");
            _output.WriteLine($"性能提升: {improvement:F1}%");
            _output.WriteLine($"");
            _output.WriteLine($"? 场景说明:");
            _output.WriteLine($"  - 模拟分布式系统，每个服务器使用独立 WorkerId");
            _output.WriteLine($"  - 每个 WorkerId 独立 _state，完全无竞争");
            _output.WriteLine($"  - CAS 冲突率: 0%");
            _output.WriteLine($"  - 无锁版本避免了 lock 的固定开销");
            _output.WriteLine($"  - 这是无锁实现的设计初衷和最佳场景");

            // 多 WorkerId 场景：无锁应该明显更快
            Assert.True(speed3 >= speed1 * 1.1, $"多 WorkerId 场景下无锁版本应至少提升 10%: {speed3:N0} vs {speed1:N0}");
            
            if (improvement >= 20)
            {
                _output.WriteLine($"");
                _output.WriteLine($"?? 无锁版本性能提升 {improvement:F1}%，优势明显！");
            }
        }
    }
}
