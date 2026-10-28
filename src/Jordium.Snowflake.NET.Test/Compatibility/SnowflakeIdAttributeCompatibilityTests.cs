using Jordium.Snowflake.NET;
using Xunit;
using Xunit.Abstractions;

namespace Jordium.XUnit.Compatibility
{
    /// <summary>
    /// Cross-framework compatibility tests for the [SnowflakeID] attribute feature
    /// across .NET Framework 4.6.1+, .NET 6-10, .NET Standard 2.0/2.1
    /// </summary>
    public class SnowflakeIdAttributeCompatibilityTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public SnowflakeIdAttributeCompatibilityTests(ITestOutputHelper output)
        {
            _output = output;
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        public void Dispose()
        {
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        private class Entity
        {
            [SnowflakeID]
            public long Id { get; set; }

            [SnowflakeID]
            public long? NullableId { get; set; }

            [SnowflakeID]
            public string Code { get; set; } = string.Empty;
        }

        [Fact]
        public void SnowflakeIdAttribute_ShouldBeReadableViaReflection_OnCurrentFramework()
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

            // Arrange
            var property = typeof(Entity).GetProperty(nameof(Entity.Id));

            // Act
            var attribute = property!.GetCustomAttributes(typeof(SnowflakeIDAttribute), inherit: true);

            // Assert
            _output.WriteLine($"Running [SnowflakeID] compatibility test on: {framework}");
            Assert.Single(attribute);
        }

        [Fact]
        public void Populate_OnCurrentFramework_ShouldFillLongNullableLongAndStringMembers()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 20, dataCenterId: 1);
            var entity = new Entity();

            // Act
            var result = SnowflakeIdPopulator.Populate(entity);

            // Assert
            Assert.True(result.Id > 0);
            Assert.NotNull(result.NullableId);
            Assert.False(string.IsNullOrEmpty(result.Code));
        }

        [Fact]
        public void GenerateSnowflakeIds_ExtensionMethod_ShouldWorkConsistentlyAcrossFrameworks()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 21, dataCenterId: 1);
            var entity = new Entity();

            // Act
            entity.GenerateSnowflakeIds();

            // Assert
            Assert.True(entity.Id > 0);
        }
    }
}
