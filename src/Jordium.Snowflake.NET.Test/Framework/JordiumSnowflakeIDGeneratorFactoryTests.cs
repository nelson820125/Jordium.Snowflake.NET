using Jordium.Snowflake.NET;
using Xunit;

namespace Jordium.XUnit.Framework
{
    public class JordiumSnowflakeIDGeneratorFactoryTests : IDisposable
    {
        public JordiumSnowflakeIDGeneratorFactoryTests()
        {
            // Reset before each test
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        public void Dispose()
        {
            // Cleanup after each test
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        #region Create Method Tests

        [Fact]
        public void Create_WithWorkerIdAndDataCenterId_ShouldReturnValidGenerator()
        {
            // Arrange & Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);
        }

        [Fact]
        public void Create_WithOptionsObject_ShouldReturnValidGenerator()
        {
            // Arrange
            var options = new IDGeneratorOptions(workerId: 2, dataCenterId: 2)
            {
                Method = 1
            };

            // Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(options);

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);
        }

        [Fact]
        public void Create_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            IDGeneratorOptions? options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JordiumSnowflakeIDGeneratorFactory.Create(options!));
        }

        [Fact]
        public void Create_WithConfigureDelegate_ShouldReturnValidGenerator()
        {
            // Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(options =>
            {
                options.WorkerId = 3;
                options.DataCenterId = 3;
                options.Method = 1;
            });

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);
        }

        [Fact]
        public void Create_WithNullDelegate_ShouldThrowArgumentNullException()
        {
            // Arrange
            Action<IDGeneratorOptions>? configure = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JordiumSnowflakeIDGeneratorFactory.Create(configure!));
        }

        [Fact]
        public void Create_MultipleInstances_ShouldGenerateUniqueIds()
        {
            // Arrange
            var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 2);

            // Act
            var ids1 = new List<long>();
            var ids2 = new List<long>();

            for (int i = 0; i < 100; i++)
            {
                ids1.Add(generator1.NewLong());
                ids2.Add(generator2.NewLong());
            }

            // Assert
            var allIds = ids1.Concat(ids2).ToList();
            Assert.Equal(200, allIds.Count);
            Assert.Equal(200, allIds.Distinct().Count());
        }

        #endregion

        #region InitializeDefault Tests

        [Fact]
        public void InitializeDefault_WithWorkerIdAndDataCenterId_ShouldSucceed()
        {
            // Act
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1, dataCenterId: 1);

            // Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
            Assert.NotNull(JordiumSnowflakeIDGeneratorFactory.Default);
        }

        [Fact]
        public void InitializeDefault_WithOptionsObject_ShouldSucceed()
        {
            // Arrange
            var options = new IDGeneratorOptions(workerId: 2, dataCenterId: 2);

            // Act
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options);

            // Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
            Assert.NotNull(JordiumSnowflakeIDGeneratorFactory.Default);
        }

        [Fact]
        public void InitializeDefault_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            IDGeneratorOptions? options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options!));
        }

        [Fact]
        public void InitializeDefault_WithConfigureDelegate_ShouldSucceed()
        {
            // Act
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options =>
            {
                options.WorkerId = 3;
                options.DataCenterId = 3;
            });

            // Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
            Assert.NotNull(JordiumSnowflakeIDGeneratorFactory.Default);
        }

        [Fact]
        public void InitializeDefault_WithNullDelegate_ShouldThrowArgumentNullException()
        {
            // Arrange
            Action<IDGeneratorOptions>? configure = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JordiumSnowflakeIDGeneratorFactory.InitializeDefault(configure!));
        }

        [Fact]
        public void InitializeDefault_CalledTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 2));

            Assert.Contains("already initialized", ex.Message);
        }

        #endregion

        #region Default Property Tests

        [Fact]
        public void Default_WhenNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var _ = JordiumSnowflakeIDGeneratorFactory.Default;
            });

            Assert.Contains("not initialized", ex.Message);
        }

        [Fact]
        public void Default_WhenInitialized_ShouldReturnValidGenerator()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);

            // Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Default;

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);
        }

        [Fact]
        public void Default_CalledMultipleTimes_ShouldReturnSameInstance()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);

            // Act
            var instance1 = JordiumSnowflakeIDGeneratorFactory.Default;
            var instance2 = JordiumSnowflakeIDGeneratorFactory.Default;

            // Assert
            Assert.Same(instance1, instance2);
        }

        #endregion

        #region IsDefaultInitialized Tests

        [Fact]
        public void IsDefaultInitialized_WhenNotInitialized_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.False(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        [Fact]
        public void IsDefaultInitialized_WhenInitialized_ShouldReturnTrue()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);

            // Act & Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        [Fact]
        public void IsDefaultInitialized_AfterReset_ShouldReturnFalse()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);

            // Act
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();

            // Assert
            Assert.False(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        #endregion

        #region ResetDefault Tests

        [Fact]
        public void ResetDefault_WhenInitialized_ShouldClearDefaultInstance()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);

            // Act
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();

            // Assert
            Assert.False(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        [Fact]
        public void ResetDefault_WhenNotInitialized_ShouldNotThrow()
        {
            // Act & Assert (should not throw)
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
            Assert.False(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        [Fact]
        public void ResetDefault_AllowsReinitialization_ShouldSucceed()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();

            // Act
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 2);

            // Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
            Assert.NotNull(JordiumSnowflakeIDGeneratorFactory.Default);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void Default_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1);
            var ids = new System.Collections.Concurrent.ConcurrentBag<long>();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        ids.Add(JordiumSnowflakeIDGeneratorFactory.Default.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(1000, ids.Count);
            Assert.Equal(1000, ids.Distinct().Count());
        }

        [Fact]
        public void InitializeDefault_ConcurrentCalls_ShouldOnlyInitializeOnce()
        {
            // Arrange
            var successCount = 0;
            var exceptionCount = 0;
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                int workerId = i + 1;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        JordiumSnowflakeIDGeneratorFactory.InitializeDefault((ushort)workerId);
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref exceptionCount);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(1, successCount);
            Assert.Equal(9, exceptionCount);
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
        }

        #endregion

        #region ID Generation Tests

        [Fact]
        public void Create_GeneratedIds_ShouldBeUnique()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var ids = new HashSet<long>();

            // Act
            for (int i = 0; i < 10000; i++)
            {
                ids.Add(generator.NewLong());
            }

            // Assert
            Assert.Equal(10000, ids.Count);
        }

        [Fact]
        public void Create_GeneratedIds_ShouldBeIncreasing()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);

            // Act
            long previousId = 0;
            for (int i = 0; i < 1000; i++)
            {
                long currentId = generator.NewLong();
                Assert.True(currentId > previousId);
                previousId = currentId;
            }
        }

        [Fact]
        public void Create_WithDifferentWorkerIds_ShouldGenerateUniqueIds()
        {
            // Arrange
            var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 2);
            var generator3 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 3);

            var allIds = new HashSet<long>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                allIds.Add(generator1.NewLong());
                allIds.Add(generator2.NewLong());
                allIds.Add(generator3.NewLong());
            }

            // Assert
            Assert.Equal(300, allIds.Count);
        }

        #endregion
    }
}
