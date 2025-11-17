using Jordium.Snowflake.NET;
using Jordium.Snowflake.NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Jordium.XUnit.Compatibility
{
    /// <summary>
    /// Cross-framework compatibility tests for .NET Framework 4.6.1+, .NET 5-10, .NET Standard 2.0/2.1
    /// </summary>
    public class CrossFrameworkCompatibilityTests
    {
        private readonly ITestOutputHelper _output;

        public CrossFrameworkCompatibilityTests(ITestOutputHelper output)
        {
            _output = output;
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        #region Target Framework Detection

        [Fact]
        public void DetectTargetFramework_ShouldOutputCurrentFramework()
        {
#if NET10_0
            var framework = "NET 10.0";
#elif NET9_0
            var framework = "NET 9.0";
#elif NET8_0
            var framework = "NET 8.0";
#elif NET7_0
            var framework = "NET 7.0";
#elif NET6_0
            var framework = "NET 6.0";
#elif NETSTANDARD2_1
            var framework = ".NET Standard 2.1";
#elif NETSTANDARD2_0
            var framework = ".NET Standard 2.0";
#elif NET48
            var framework = ".NET Framework 4.8";
#elif NET472
            var framework = ".NET Framework 4.7.2";
#elif NET461
            var framework = ".NET Framework 4.6.1";
#else
            var framework = "Unknown";
#endif

            _output.WriteLine($"Running tests on: {framework}");
            _output.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            
            Assert.NotEqual("Unknown", framework);
        }

        #endregion

        #region Factory Compatibility Tests

        [Fact]
        public void Factory_Create_ShouldWorkAcrossAllFrameworks()
        {
            // Arrange & Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"Generated ID: {id}");
        }

        [Fact]
        public void Factory_CreateWithOptions_ShouldWorkAcrossAllFrameworks()
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

            _output.WriteLine($"Generated ID: {id}");
        }

        [Fact]
        public void Factory_CreateWithDelegate_ShouldWorkAcrossAllFrameworks()
        {
            // Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(opts =>
            {
                opts.WorkerId = 3;
                opts.DataCenterId = 3;
                opts.Method = 1;
            });

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"Generated ID: {id}");
        }

        [Fact]
        public void Factory_InitializeDefault_ShouldWorkAcrossAllFrameworks()
        {
            // Act
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 10, dataCenterId: 5);

            // Assert
            Assert.True(JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized);
            var generator = JordiumSnowflakeIDGeneratorFactory.Default;
            Assert.NotNull(generator);

            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"Generated ID from Default: {id}");

            // Cleanup
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        [Fact]
        public void Factory_MultipleInstances_ShouldGenerateUniqueIds()
        {
            // Arrange
            var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 2);
            var ids = new HashSet<long>();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                ids.Add(generator1.NewLong());
                ids.Add(generator2.NewLong());
            }

            // Assert
            Assert.Equal(2000, ids.Count);
            _output.WriteLine($"Generated {ids.Count} unique IDs across frameworks");
        }

        #endregion

        #region DI Extension Compatibility Tests

        [Fact]
        public void DI_AddJordiumSnowflakeIdGenerator_ShouldWorkAcrossAllFrameworks()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 1;
                options.DataCenterId = 1;
            });

            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetRequiredService<IIDGenerator>();

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"DI Generated ID: {id}");
        }

        [Fact]
        public void DI_WithConfiguration_ShouldWorkAcrossAllFrameworks()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "5",
                ["JordiumSnowflakeConfig:DataCenterId"] = "3",
                ["JordiumSnowflakeConfig:Method"] = "1"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator();

            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetRequiredService<IIDGenerator>();

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"Config-based ID: {id}");
        }

        [Fact]
        public void DI_WithOptionsObject_ShouldWorkAcrossAllFrameworks()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new IDGeneratorOptions(workerId: 10, dataCenterId: 5)
            {
                Method = 1
            };

            // Act
            services.AddJordiumSnowflakeIdGenerator(options);

            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetRequiredService<IIDGenerator>();

            // Assert
            Assert.NotNull(generator);
            long id = generator.NewLong();
            Assert.True(id > 0);

            _output.WriteLine($"Options-based ID: {id}");
        }

        #endregion

        #region Framework-Specific Feature Tests

#if NET6_0_OR_GREATER
        [Fact]
        public void Modern_NET_Features_ShouldWork()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);

            // Act - Test modern C# features
            var ids = Enumerable.Range(0, 100)
                .Select(_ => generator.NewLong())
                .ToHashSet();

            // Assert
            Assert.Equal(100, ids.Count);
            _output.WriteLine($"Modern .NET features work correctly on {GetFrameworkName()}");
        }

        [Fact]
        public void Async_Pattern_ShouldWork()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var tasks = new List<Task<long>>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => generator.NewLong()));
            }

            var ids = Task.WhenAll(tasks).Result;

            // Assert
            Assert.Equal(100, ids.Length);
            Assert.Equal(100, ids.Distinct().Count());
            _output.WriteLine($"Async pattern works on {GetFrameworkName()}");
        }

        private string GetFrameworkName()
        {
#if NET10_0
            return ".NET 10.0";
#elif NET9_0
            return ".NET 9.0";
#elif NET8_0
            return ".NET 8.0";
#elif NET7_0
            return ".NET 7.0";
#elif NET6_0
            return ".NET 6.0";
#else
            return "Unknown";
#endif
        }
#endif

        #endregion

        #region Performance Comparison Across Frameworks

        [Fact]
        public void Performance_Comparison_AcrossFrameworks()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1);
            var iterations = 10000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                generator.NewLong();
            }
            stopwatch.Stop();

            // Assert
            var idsPerSecond = iterations * 1000.0 / stopwatch.ElapsedMilliseconds;
            
            _output.WriteLine($"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            _output.WriteLine($"Generated {iterations:N0} IDs in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Performance: {idsPerSecond:N0} IDs/second");
            _output.WriteLine($"Average: {stopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3}¦Ìs per ID");

            Assert.True(idsPerSecond > 10000, $"Performance too low: {idsPerSecond:N0} IDs/sec");
        }

        #endregion

        #region Compatibility Matrix Tests

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 3)]
        [InlineData(10, 5)]
        [InlineData(31, 31)]
        public void Compatibility_Matrix_DifferentWorkerConfigs(ushort workerId, ushort dataCenterId)
        {
            // Arrange & Act
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId, dataCenterId);
            var ids = new HashSet<long>();

            for (int i = 0; i < 100; i++)
            {
                ids.Add(generator.NewLong());
            }

            // Assert
            Assert.Equal(100, ids.Count);
            _output.WriteLine($"WorkerId={workerId}, DataCenterId={dataCenterId}: 100 unique IDs generated");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Compatibility_Matrix_DifferentMethods(short method)
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(options =>
            {
                options.WorkerId = 1;
                options.DataCenterId = 1;
                options.Method = method;
            });

            // Act
            var ids = new HashSet<long>();
            for (int i = 0; i < 100; i++)
            {
                ids.Add(generator.NewLong());
            }

            // Assert
            Assert.Equal(100, ids.Count);
            _output.WriteLine($"Method={method}: 100 unique IDs generated");
        }

        #endregion

        #region Exception Handling Compatibility

        [Fact]
        public void ExceptionHandling_ShouldWorkAcrossFrameworks()
        {
            // Test 1: Null options
            Assert.Throws<ArgumentNullException>(() =>
                JordiumSnowflakeIDGeneratorFactory.Create((IDGeneratorOptions)null!));

            // Test 2: Null delegate
            Assert.Throws<ArgumentNullException>(() =>
                JordiumSnowflakeIDGeneratorFactory.Create((Action<IDGeneratorOptions>)null!));

            // Test 3: Access uninitialized default
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
            Assert.Throws<InvalidOperationException>(() =>
                JordiumSnowflakeIDGeneratorFactory.Default);

            // Test 4: Double initialization
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(1, 1);
            Assert.Throws<InvalidOperationException>(() =>
                JordiumSnowflakeIDGeneratorFactory.InitializeDefault(2, 2));

            _output.WriteLine("All exception scenarios work correctly across frameworks");

            // Cleanup
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        #endregion
    }
}
