using Jordium.Snowflake.NET;
using Jordium.Snowflake.NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jordium.XUnit.Framework
{
    public class JordiumSnowflakeIDGeneratorServiceCollectionExtensionsTests
    {
        #region AddJordiumSnowflakeIdGenerator - No Parameters Tests

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_NoParameters_ShouldRegisterService()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetService<IIDGenerator>();
            Assert.NotNull(generator);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithConfigDelegate_ShouldApplyConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 5;
                options.DataCenterId = 3;
                options.Method = 1;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(5, optionsSnapshot.Value.WorkerId);
            Assert.Equal(3, optionsSnapshot.Value.DataCenterId);
            Assert.Equal(1, optionsSnapshot.Value.Method);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithAppsettingsJson_ShouldBindConfiguration()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "10",
                ["JordiumSnowflakeConfig:DataCenterId"] = "5",
                ["JordiumSnowflakeConfig:Method"] = "2"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(10, optionsSnapshot.Value.WorkerId);
            Assert.Equal(5, optionsSnapshot.Value.DataCenterId);
            Assert.Equal(2, optionsSnapshot.Value.Method);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_CodeConfigOverridesAppsettings_ShouldUseCodeConfig()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "10",
                ["JordiumSnowflakeConfig:DataCenterId"] = "5"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 99;
                options.DataCenterId = 88;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(99, optionsSnapshot.Value.WorkerId);
            Assert.Equal(88, optionsSnapshot.Value.DataCenterId);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_RegisteredAsSingleton_ShouldReturnSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddJordiumSnowflakeIdGenerator(options =>
            {
                options.WorkerId = 1;
            });

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var generator1 = serviceProvider.GetRequiredService<IIDGenerator>();
            var generator2 = serviceProvider.GetRequiredService<IIDGenerator>();

            // Assert
            Assert.Same(generator1, generator2);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_GeneratedIds_ShouldBeUnique()
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

            // Act
            var ids = new HashSet<long>();
            for (int i = 0; i < 1000; i++)
            {
                ids.Add(generator.NewLong());
            }

            // Assert
            Assert.Equal(1000, ids.Count);
        }

        #endregion

        #region AddJordiumSnowflakeIdGenerator - With IConfiguration Tests

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithConfiguration_ShouldBindFromSection()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "7",
                ["JordiumSnowflakeConfig:DataCenterId"] = "4",
                ["JordiumSnowflakeConfig:Method"] = "1"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddJordiumSnowflakeIdGenerator(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(7, optionsSnapshot.Value.WorkerId);
            Assert.Equal(4, optionsSnapshot.Value.DataCenterId);
            Assert.Equal(1, optionsSnapshot.Value.Method);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithCustomSectionName_ShouldBindFromCustomSection()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["CustomSnowflake:WorkerId"] = "15",
                ["CustomSnowflake:DataCenterId"] = "8",
                ["CustomSnowflake:Method"] = "2"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddJordiumSnowflakeIdGenerator(configuration, "CustomSnowflake");

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(15, optionsSnapshot.Value.WorkerId);
            Assert.Equal(8, optionsSnapshot.Value.DataCenterId);
            Assert.Equal(2, optionsSnapshot.Value.Method);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            IConfiguration? configuration = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                services.AddJordiumSnowflakeIdGenerator(configuration!, "JordiumSnowflakeConfig"));
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithNonExistentSection_ShouldUseDefaultValues()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // Act
            services.AddJordiumSnowflakeIdGenerator(configuration, "NonExistentSection");

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetService<IIDGenerator>();
            Assert.NotNull(generator);
        }

        #endregion

        #region AddJordiumSnowflakeIdGenerator - With IDGeneratorOptions Tests

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithOptionsObject_ShouldUseProvidedOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new IDGeneratorOptions(workerId: 20, dataCenterId: 10)
            {
                Method = 1
            };

            // Act
            services.AddJordiumSnowflakeIdGenerator(options);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var generator = serviceProvider.GetRequiredService<IIDGenerator>();
            Assert.NotNull(generator);

            long id = generator.NewLong();
            Assert.True(id > 0);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithNullOptionsObject_ShouldThrowArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            IDGeneratorOptions? options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                services.AddJordiumSnowflakeIdGenerator(options!));
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithOptionsObject_ShouldRegisterAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new IDGeneratorOptions(workerId: 1, dataCenterId: 1);
            services.AddJordiumSnowflakeIdGenerator(options);

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var generator1 = serviceProvider.GetRequiredService<IIDGenerator>();
            var generator2 = serviceProvider.GetRequiredService<IIDGenerator>();

            // Assert
            Assert.Same(generator1, generator2);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_MultipleRegistrations_ShouldUseLastRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator(options => options.WorkerId = 1);
            services.AddJordiumSnowflakeIdGenerator(options => options.WorkerId = 2);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            Assert.Equal(2, optionsSnapshot.Value.WorkerId);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_InRealScenario_ShouldWorkWithDependencyInjection()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "3",
                ["JordiumSnowflakeConfig:DataCenterId"] = "2",
                ["JordiumSnowflakeConfig:Method"] = "1"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddJordiumSnowflakeIdGenerator();

            // Simulate a service that depends on IIDGenerator
            services.AddTransient<TestService>();

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var testService = serviceProvider.GetRequiredService<TestService>();

            // Assert
            long id = testService.GenerateId();
            Assert.True(id > 0);
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_ConcurrentAccess_ShouldBeThreadSafe()
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

            var ids = new System.Collections.Concurrent.ConcurrentBag<long>();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        ids.Add(generator.NewLong());
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(1000, ids.Count);
            Assert.Equal(1000, ids.Distinct().Count());
        }

        [Fact]
        public void AddJordiumSnowflakeIdGenerator_WithComplexConfiguration_ShouldBindAllProperties()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["JordiumSnowflakeConfig:WorkerId"] = "5",
                ["JordiumSnowflakeConfig:DataCenterId"] = "3",
                ["JordiumSnowflakeConfig:Method"] = "1",
                ["JordiumSnowflakeConfig:SeqBitLength"] = "10",
                ["JordiumSnowflakeConfig:WorkerIdBitLength"] = "6",
                ["JordiumSnowflakeConfig:DataCenterIdBitLength"] = "4",
                ["JordiumSnowflakeConfig:TopOverCostCount"] = "3000"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Act
            services.AddJordiumSnowflakeIdGenerator();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<IDGeneratorOptions>>();
            var options = optionsSnapshot.Value;

            Assert.Equal(5, options.WorkerId);
            Assert.Equal(3, options.DataCenterId);
            Assert.Equal(1, options.Method);
            Assert.Equal(10, options.SeqBitLength);
            Assert.Equal(6, options.WorkerIdBitLength);
            Assert.Equal(4, options.DataCenterIdBitLength);
            Assert.Equal(3000, options.TopOverCostCount);
        }

        #endregion

        #region Helper Classes

        private class TestService
        {
            private readonly IIDGenerator _idGenerator;

            public TestService(IIDGenerator idGenerator)
            {
                _idGenerator = idGenerator;
            }

            public long GenerateId()
            {
                return _idGenerator.NewLong();
            }
        }

        #endregion
    }
}
