using System.Collections.Concurrent;
using System.Diagnostics;
using Jordium.Snowflake.NET;
using Xunit.Abstractions;

namespace Jordium.XUnit.Framework
{
    /// <summary>
    /// Snowflake ID 生成器测试
    /// </summary>
    public class SnowflakeTests
    {
        private readonly ITestOutputHelper _output;

        public SnowflakeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试单个 WorkerId 下是否存在重复的结果
        /// </summary>
        [Theory]
        [InlineData(1, 1, 10000)]
        [InlineData(2, 1, 10000)]
        [InlineData(10, 2, 10000)]
        public void SingleWorkerId_ShouldNotGenerateDuplicateIds(ushort workerId, ushort dataCenterId, int count)
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = workerId,
                DataCenterId = dataCenterId
            };
            var generator = new DefaultIDGenerator(options);
            var ids = new ConcurrentBag<long>();

            // Act
            Parallel.For(0, count, i =>
            {
                var id = generator.NewLong();
                ids.Add(id);
            });

            // Assert
            var uniqueIds = ids.Distinct().ToList();
            _output.WriteLine($"WorkerId: {workerId}, DataCenterId: {dataCenterId}, Generated: {ids.Count}, Unique: {uniqueIds.Count}");
            Assert.Equal(ids.Count, uniqueIds.Count);
        }

        /// <summary>
        /// 测试单个 WorkerId 串行生成是否存在重复
        /// </summary>
        [Fact]
        public void SingleWorkerId_SerialGeneration_ShouldNotGenerateDuplicateIds()
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1
            };
            var generator = new DefaultIDGenerator(options);
            var ids = new HashSet<long>();
            var count = 100000;

            // Act
            for (int i = 0; i < count; i++)
            {
                var id = generator.NewLong();
                var added = ids.Add(id);

                // Assert - 如果添加失败说明有重复
                Assert.True(added, $"Duplicate ID found: {id} at iteration {i}");
            }

            // Assert
            _output.WriteLine($"Generated {count} unique IDs successfully");
            Assert.Equal(count, ids.Count);
        }

        /// <summary>
        /// 测试多个不同 WorkerId 同时运行是否存在重复结果
        /// </summary>
        [Theory]
        [InlineData(5, 10000)]
        [InlineData(10, 5000)]
        public void MultipleWorkerIds_Parallel_ShouldNotGenerateDuplicateIds(int workerCount, int countPerWorker)
        {
            // Arrange
            var allIds = new ConcurrentBag<long>();
            var tasks = new List<Task>();

            // Act - 模拟多个进程（不同 WorkerId）同时生成 ID
            for (ushort workerId = 1; workerId <= workerCount; workerId++)
            {
                var currentWorkerId = workerId;
                var task = Task.Run(() =>
                {
                    var options = new IDGeneratorOptions
                    {
                        WorkerId = currentWorkerId,
                        DataCenterId = 1
                    };
                    var generator = new DefaultIDGenerator(options);

                    for (int i = 0; i < countPerWorker; i++)
                    {
                        var id = generator.NewLong();
                        allIds.Add(id);
                    }

                    _output.WriteLine($"WorkerId {currentWorkerId} generated {countPerWorker} IDs");
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            var totalGenerated = workerCount * countPerWorker;
            var uniqueIds = allIds.Distinct().ToList();
            _output.WriteLine($"Total Generated: {allIds.Count}, Unique: {uniqueIds.Count}");
            Assert.Equal(totalGenerated, allIds.Count);
            Assert.Equal(allIds.Count, uniqueIds.Count);
        }

        /// <summary>
        /// 测试多个 DataCenterId 和 WorkerId 组合场景
        /// </summary>
        [Theory]
        [InlineData(2, 5, 5000)]  // 2个数据中心，每个5个worker
        [InlineData(3, 3, 3000)]  // 3个数据中心，每个3个worker
        public void MultipleDataCentersAndWorkers_ShouldNotGenerateDuplicateIds(int dataCenterCount, int workerCountPerDC, int countPerWorker)
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
                            DataCenterId = currentDcId
                        };
                        var generator = new DefaultIDGenerator(options);

                        for (int i = 0; i < countPerWorker; i++)
                        {
                            var id = generator.NewLong();
                            allIds.Add(id);
                        }

                        _output.WriteLine($"DataCenter {currentDcId}, WorkerId {currentWorkerId} generated {countPerWorker} IDs");
                    });
                    tasks.Add(task);
                }
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            var totalGenerated = dataCenterCount * workerCountPerDC * countPerWorker;
            var uniqueIds = allIds.Distinct().ToList();
            _output.WriteLine($"Total DataCenters: {dataCenterCount}, Workers/DC: {workerCountPerDC}");
            _output.WriteLine($"Total Generated: {allIds.Count}, Unique: {uniqueIds.Count}");
            Assert.Equal(totalGenerated, allIds.Count);
            Assert.Equal(allIds.Count, uniqueIds.Count);
        }

        /// <summary>
        /// 测试多个 WorkerId 高并发场景
        /// </summary>
        [Fact]
        public void MultipleWorkerIds_HighConcurrency_ShouldNotGenerateDuplicateIds()
        {
            // Arrange
            var workerCount = 4;
            var threadsPerWorker = 10;
            var countPerThread = 1000;
            var allIds = new ConcurrentBag<long>();

            // Act
            Parallel.For(0, workerCount, workerId =>
            {
                var options = new IDGeneratorOptions
                {
                    WorkerId = (ushort)(workerId + 1),
                    DataCenterId = 1
                };
                var generator = new DefaultIDGenerator(options);

                Parallel.For(0, threadsPerWorker, threadId =>
                {
                    for (int i = 0; i < countPerThread; i++)
                    {
                        var id = generator.NewLong();
                        allIds.Add(id);
                    }
                });
            });

            // Assert
            var totalExpected = workerCount * threadsPerWorker * countPerThread;
            var uniqueIds = allIds.Distinct().ToList();
            _output.WriteLine($"Workers: {workerCount}, Threads/Worker: {threadsPerWorker}, Count/Thread: {countPerThread}");
            _output.WriteLine($"Total Generated: {allIds.Count}, Unique: {uniqueIds.Count}");
            Assert.Equal(totalExpected, allIds.Count);
            Assert.Equal(allIds.Count, uniqueIds.Count);
        }

        /// <summary>
        /// 验证不同 DataCenterId 生成的 ID 不重复
        /// </summary>
        [Fact]
        public void DifferentDataCenterIds_ShouldGenerateDifferentIds()
        {
            // Arrange
            var count = 1000;
            var dc1Ids = new HashSet<long>();
            var dc2Ids = new HashSet<long>();

            var options1 = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 1 };
            var generator1 = new DefaultIDGenerator(options1);

            var options2 = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 2 };
            var generator2 = new DefaultIDGenerator(options2);

            // Act
            for (int i = 0; i < count; i++)
            {
                dc1Ids.Add(generator1.NewLong());
                dc2Ids.Add(generator2.NewLong());
            }

            // Assert
            var intersection = dc1Ids.Intersect(dc2Ids).ToList();
            _output.WriteLine($"DataCenter1 IDs: {dc1Ids.Count}, DataCenter2 IDs: {dc2Ids.Count}, Duplicates: {intersection.Count}");
            Assert.Empty(intersection);
        }

        /// <summary>
        /// 性能测试 - 单线程生成速度
        /// </summary>
        [Theory]
        [InlineData(100000)]
        [InlineData(500000)]
        [InlineData(1000000)]
        public void Performance_SingleThread_GenerationSpeed(int count)
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1
            };
            var generator = new DefaultIDGenerator(options);
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < count; i++)
            {
                generator.NewLong();
            }

            stopwatch.Stop();

            // Report
            var idsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"Generated {count:N0} IDs in {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"Speed: {idsPerSecond:N0} IDs/second");
            _output.WriteLine($"Average: {stopwatch.Elapsed.TotalMilliseconds / count:F4}ms per ID");

            // Assert - 性能应该大于 10万/秒
            Assert.True(idsPerSecond > 100000, $"Performance too low: {idsPerSecond:N0} IDs/second");
        }

        /// <summary>
        /// 性能测试 - 多线程并发生成速度
        /// </summary>
        [Theory]
        [InlineData(4, 250000)]
        [InlineData(8, 125000)]
        public void Performance_MultiThread_GenerationSpeed(int threadCount, int countPerThread)
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1
            };
            var generator = new DefaultIDGenerator(options);
            var stopwatch = Stopwatch.StartNew();
            var totalCount = threadCount * countPerThread;

            // Act
            Parallel.For(0, threadCount, threadId =>
            {
                for (int i = 0; i < countPerThread; i++)
                {
                    generator.NewLong();
                }
            });

            stopwatch.Stop();

            // Report
            var idsPerSecond = totalCount / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"Threads: {threadCount}, IDs/Thread: {countPerThread:N0}");
            _output.WriteLine($"Total: {totalCount:N0} IDs in {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"Speed: {idsPerSecond:N0} IDs/second");
            _output.WriteLine($"Average: {stopwatch.Elapsed.TotalMilliseconds / totalCount:F4}ms per ID");

            // Assert - 多线程性能应该更高
            Assert.True(idsPerSecond > 100000, $"Performance too low: {idsPerSecond:N0} IDs/second");
        }

        /// <summary>
        /// 性能测试 - 多 WorkerId 并发生成速度
        /// </summary>
        [Fact]
        public void Performance_MultipleWorkers_GenerationSpeed()
        {
            // Arrange
            var workerCount = 4;
            var countPerWorker = 250000;
            var totalCount = workerCount * countPerWorker;
            var stopwatch = Stopwatch.StartNew();

            // Act
            Parallel.For(0, workerCount, workerId =>
            {
                var options = new IDGeneratorOptions
                {
                    WorkerId = (ushort)(workerId + 1),
                    DataCenterId = 1
                };
                var generator = new DefaultIDGenerator(options);

                for (int i = 0; i < countPerWorker; i++)
                {
                    generator.NewLong();
                }
            });

            stopwatch.Stop();

            // Report
            var idsPerSecond = totalCount / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"Workers: {workerCount}, IDs/Worker: {countPerWorker:N0}");
            _output.WriteLine($"Total: {totalCount:N0} IDs in {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"Speed: {idsPerSecond:N0} IDs/second");
            _output.WriteLine($"Average: {stopwatch.Elapsed.TotalMilliseconds / totalCount:F4}ms per ID");

            // Assert
            Assert.True(idsPerSecond > 100000, $"Performance too low: {idsPerSecond:N0} IDs/second");
        }

        /// <summary>
        /// 性能对比测试 - Method 1 vs Method 2 vs Method 3
        /// </summary>
        [Fact]
        public void Performance_CompareAllMethods()
        {
            // Arrange
            var count = 100000;
            
            // Method 1 (漂移算法)
            var method1Options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 1
            };

            // Method 2 (传统算法)
            var method2Options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 2
            };

            // Method 3 (无锁算法)
            var method3Options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                Method = 3
            };

            // Test Method 1
            var generator1 = new DefaultIDGenerator(method1Options);
            var stopwatch1 = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                generator1.NewLong();
            }
            stopwatch1.Stop();
            var speed1 = count / stopwatch1.Elapsed.TotalSeconds;

            // Test Method 2
            var generator2 = new DefaultIDGenerator(method2Options);
            var stopwatch2 = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                generator2.NewLong();
            }
            stopwatch2.Stop();
            var speed2 = count / stopwatch2.Elapsed.TotalSeconds;

            // Test Method 3
            var generator3 = new DefaultIDGenerator(method3Options);
            var stopwatch3 = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                generator3.NewLong();
            }
            stopwatch3.Stop();
            var speed3 = count / stopwatch3.Elapsed.TotalSeconds;

            // Report
            _output.WriteLine("=== 算法性能对比 ===");
            _output.WriteLine($"Method 1 (漂移): {count:N0} IDs in {stopwatch1.ElapsedMilliseconds:N0}ms, Speed: {speed1:N0} IDs/s");
            _output.WriteLine($"Method 2 (传统): {count:N0} IDs in {stopwatch2.ElapsedMilliseconds:N0}ms, Speed: {speed2:N0} IDs/s");
            _output.WriteLine($"Method 3 (无锁): {count:N0} IDs in {stopwatch3.ElapsedMilliseconds:N0}ms, Speed: {speed3:N0} IDs/s");

            // Assert - 所有方法都应该满足基本性能要求
            Assert.True(speed1 > 50000, $"Method 1 performance too low: {speed1:N0} IDs/second");
            Assert.True(speed2 > 50000, $"Method 2 performance too low: {speed2:N0} IDs/second");
            Assert.True(speed3 > 50000, $"Method 3 performance too low: {speed3:N0} IDs/second");
        }

        /// <summary>
        /// 验证 ID 的递增性
        /// </summary>
        [Fact]
        public void GeneratedIds_ShouldBeIncreasing()
        {
            // Arrange
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1
            };
            var generator = new DefaultIDGenerator(options);
            var count = 10000;
            long previousId = 0;

            // Act & Assert
            for (int i = 0; i < count; i++)
            {
                var currentId = generator.NewLong();
                Assert.True(currentId > previousId, $"ID not increasing: previous={previousId}, current={currentId}");
                previousId = currentId;
            }

            _output.WriteLine($"All {count} IDs are in increasing order");
        }

        /// <summary>
        /// 验证不同 WorkerId 生成的 ID 不重复
        /// </summary>
        [Fact]
        public void DifferentWorkerIds_ShouldGenerateDifferentIds()
        {
            // Arrange
            var count = 1000;
            var worker1Ids = new HashSet<long>();
            var worker2Ids = new HashSet<long>();

            var options1 = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 1 };
            var generator1 = new DefaultIDGenerator(options1);

            var options2 = new IDGeneratorOptions { WorkerId = 2, DataCenterId = 1 };
            var generator2 = new DefaultIDGenerator(options2);

            // Act
            for (int i = 0; i < count; i++)
            {
                worker1Ids.Add(generator1.NewLong());
                worker2Ids.Add(generator2.NewLong());
            }

            // Assert
            var intersection = worker1Ids.Intersect(worker2Ids).ToList();
            _output.WriteLine($"Worker1 IDs: {worker1Ids.Count}, Worker2 IDs: {worker2Ids.Count}, Duplicates: {intersection.Count}");
            Assert.Empty(intersection);
        }

        /// <summary>
        /// 验证相同 WorkerId 但不同 DataCenterId 生成的 ID 不重复
        /// </summary>
        [Fact]
        public void SameWorkerIdDifferentDataCenter_ShouldGenerateDifferentIds()
        {
            // Arrange
            var count = 5000;
            var allIds = new ConcurrentBag<long>();

            var dc1Options = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 1 };
            var dc1Generator = new DefaultIDGenerator(dc1Options);

            var dc2Options = new IDGeneratorOptions { WorkerId = 1, DataCenterId = 2 };
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
            _output.WriteLine($"Total Generated: {allIds.Count}, Unique: {uniqueIds}");
            Assert.Equal(count * 2, uniqueIds);
        }

        /// <summary>
        /// 性能诊断 - 锁竞争分析
        /// </summary>
        [Fact]
        public void Performance_Diagnosis_LockContention()
        {
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1
            };
            var generator = new DefaultIDGenerator(options);

            // 单线程测试
            var singleThreadStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                generator.NewLong();
            }
            singleThreadStopwatch.Stop();
            var singleThreadSpeed = 100000 / singleThreadStopwatch.Elapsed.TotalSeconds;

            // 多线程测试（2线程）
            var twoThreadStopwatch = Stopwatch.StartNew();
            Parallel.For(0, 2, _ =>
            {
                for (int i = 0; i < 50000; i++)
                {
                    generator.NewLong();
                }
            });
            twoThreadStopwatch.Stop();
            var twoThreadSpeed = 100000 / twoThreadStopwatch.Elapsed.TotalSeconds;

            // 多线程测试（4线程）
            var fourThreadStopwatch = Stopwatch.StartNew();
            Parallel.For(0, 4, _ =>
            {
                for (int i = 0; i < 25000; i++)
                {
                    generator.NewLong();
                }
            });
            fourThreadStopwatch.Stop();
            var fourThreadSpeed = 100000 / fourThreadStopwatch.Elapsed.TotalSeconds;

            // 多线程测试（8线程）
            var eightThreadStopwatch = Stopwatch.StartNew();
            Parallel.For(0, 8, _ =>
            {
                for (int i = 0; i < 12500; i++)
                {
                    generator.NewLong();
                }
            });
            eightThreadStopwatch.Stop();
            var eightThreadSpeed = 100000 / eightThreadStopwatch.Elapsed.TotalSeconds;

            _output.WriteLine("=== 锁竞争分析 ===");
            _output.WriteLine($"单线程速度: {singleThreadSpeed:N0} IDs/s");
            _output.WriteLine($"2线程速度:  {twoThreadSpeed:N0} IDs/s (效率: {(twoThreadSpeed / singleThreadSpeed * 100):F1}%)");
            _output.WriteLine($"4线程速度:  {fourThreadSpeed:N0} IDs/s (效率: {(fourThreadSpeed / singleThreadSpeed * 100):F1}%)");
            _output.WriteLine($"8线程速度:  {eightThreadSpeed:N0} IDs/s (效率: {(eightThreadSpeed / singleThreadSpeed * 100):F1}%)");
            _output.WriteLine("");
            _output.WriteLine("说明:");
            _output.WriteLine("- 如果多线程速度远低于单线程，说明存在严重的锁竞争");
            _output.WriteLine("- 理想情况下，多线程应该能达到单线程的 50-80% 效率");
            _output.WriteLine("- 如果 4/8 线程速度反而下降，说明锁竞争已经成为瓶颈");
        }

        /// <summary>
        /// 性能诊断 - 不同配置对性能的影响
        /// 约束条件: WorkerBitLength + DataCenterBitLength + SeqBitLength ≤ 22
        ///         且 SeqBitLength ≤ 12
        /// </summary>
        [Theory]
        [InlineData(5, 5, 12)]   // 标准配置 (32 DC × 32 Worker × 4096/ms)
        [InlineData(4, 6, 12)]   // 多数据中心配置 (64 DC × 16 Worker × 4096/ms)
        [InlineData(6, 4, 12)]   // 多机器配置 (16 DC × 64 Worker × 4096/ms)
        [InlineData(3, 7, 12)]   // 极多数据中心 (128 DC × 8 Worker × 4096/ms)
        [InlineData(7, 3, 12)]   // 极多机器 (8 DC × 128 Worker × 4096/ms)
        [InlineData(5, 5, 10)]   // 低并发配置 (32 DC × 32 Worker × 1024/ms)
        public void Performance_Diagnosis_BitLengthConfiguration(byte workerBitLength, byte dcBitLength, byte seqBitLength)
        {
            // 验证参数合法性
            var totalBits = workerBitLength + dcBitLength + seqBitLength;
            if (totalBits > 22)
            {
                _output.WriteLine($"? 配置无效: Worker={workerBitLength}, DC={dcBitLength}, Seq={seqBitLength}");
                _output.WriteLine($"总位数 {totalBits} > 22，超出限制");
                Assert.Fail($"测试数据错误: 总位数不能超过 22");
                return;
            }

            if (seqBitLength > 12)
            {
                _output.WriteLine($"? 配置无效: Worker={workerBitLength}, DC={dcBitLength}, Seq={seqBitLength}");
                _output.WriteLine($"SeqBitLength={seqBitLength} > 12，超出限制");
                Assert.Fail($"测试数据错误: SeqBitLength 不能超过 12");
                return;
            }

            var options = new IDGeneratorOptions
            {
                WorkerId = 1,
                DataCenterId = 1,
                WorkerIdBitLength = workerBitLength,
                DataCenterIdBitLength = dcBitLength,
                SeqBitLength = seqBitLength
            };
            var generator = new DefaultIDGenerator(options);
            var count = 100000;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                generator.NewLong();
            }
            stopwatch.Stop();

            var speed = count / stopwatch.Elapsed.TotalSeconds;
            var maxWorkers = (int)Math.Pow(2, workerBitLength);
            var maxDCs = (int)Math.Pow(2, dcBitLength);
            var maxSeqPerMs = (int)Math.Pow(2, seqBitLength);

            _output.WriteLine($"=== 配置分析 ===");
            _output.WriteLine($"位数分配: Worker={workerBitLength}位, DC={dcBitLength}位, Seq={seqBitLength}位 (总计{totalBits}位)");
            _output.WriteLine($"容量支持:");
            _output.WriteLine($"  - 数据中心: {maxDCs:N0} 个");
            _output.WriteLine($"  - 每中心机器: {maxWorkers:N0} 台");
            _output.WriteLine($"  - 总机器数: {maxDCs * maxWorkers:N0} 台");
            _output.WriteLine($"  - 单机器每毫秒: {maxSeqPerMs:N0} 个 ID");
            _output.WriteLine($"  - 全局每毫秒: {(long)maxDCs * maxWorkers * maxSeqPerMs:N0} 个 ID");
            _output.WriteLine($"性能测试:");
            _output.WriteLine($"  - 生成 {count:N0} 个 ID");
            _output.WriteLine($"  - 耗时: {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"  - 速度: {speed:N0} IDs/秒");
            _output.WriteLine("");
        }

        /// <summary>
        /// 性能诊断 - 时间获取开销
        /// </summary>
        [Fact]
        public void Performance_Diagnosis_TimeStampOverhead()
        {
            var baseTime = new DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc);
            var count = 1000000;

            // 测试 DateTime.UtcNow 调用开销
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var _ = DateTime.UtcNow;
            }
            stopwatch.Stop();

            var timePerCall = stopwatch.Elapsed.TotalMilliseconds / count;
            var callsPerSecond = count / stopwatch.Elapsed.TotalSeconds;

            _output.WriteLine("=== DateTime.UtcNow 性能 ===");
            _output.WriteLine($"调用次数: {count:N0}");
            _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds:N0}ms");
            _output.WriteLine($"平均每次: {timePerCall:F6}ms");
            _output.WriteLine($"每秒可调用: {callsPerSecond:N0} 次");
            _output.WriteLine("");
            _output.WriteLine("说明:");
            _output.WriteLine("- DateTime.UtcNow 在 Windows 上精度约为 15ms");
            _output.WriteLine("- 每次调用约 0.001-0.01ms");
            _output.WriteLine("- 如果每次生成 ID 都调用，会成为性能瓶颈");
        }

        /// <summary>
        /// ID 解析测试 - 验证 ID 各部分的正确性
        /// </summary>
        [Fact]
        public void GeneratedId_StructureShouldBeCorrect()
        {
            // Arrange
            ushort workerId = 5;
            ushort dataCenterId = 3;
            var options = new IDGeneratorOptions
            {
                WorkerId = workerId,
                DataCenterId = dataCenterId,
                WorkerIdBitLength = 5,
                DataCenterIdBitLength = 5,
                SeqBitLength = 12
            };
            var generator = new DefaultIDGenerator(options);

            // Act
            var id = generator.NewLong();

            // Assert - 解析 ID 结构
            var seqMask = (1L << 12) - 1;
            var workerMask = (1L << 5) - 1;
            var dcMask = (1L << 5) - 1;

            var extractedSeq = id & seqMask;
            var extractedWorker = (id >> 12) & workerMask;
            var extractedDC = (id >> 17) & dcMask;
            var extractedTimestamp = id >> 22;

            _output.WriteLine($"生成的 ID: {id}");
            _output.WriteLine($"时间戳: {extractedTimestamp}");
            _output.WriteLine($"数据中心ID: {extractedDC} (期望: {dataCenterId})");
            _output.WriteLine($"机器ID: {extractedWorker} (期望: {workerId})");
            _output.WriteLine($"序列号: {extractedSeq}");

            Assert.Equal(dataCenterId, extractedDC);
            Assert.Equal(workerId, extractedWorker);
            Assert.True(extractedTimestamp > 0);
        }
    }
}
