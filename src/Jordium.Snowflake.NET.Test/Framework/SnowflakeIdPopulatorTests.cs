using Jordium.Snowflake.NET;
using Xunit;

namespace Jordium.XUnit.Framework
{
    public class SnowflakeIdPopulatorTests : IDisposable
    {
        public SnowflakeIdPopulatorTests()
        {
            // Reset before each test
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        public void Dispose()
        {
            // Cleanup after each test
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        #region Test Entities

        private class OrderWithLong
        {
            [SnowflakeID]
            public long Id { get; set; }

            public decimal Amount { get; set; }
        }

        private class OrderWithNullableLong
        {
            [SnowflakeID]
            public long? Id { get; set; }
        }

        private class OrderWithString
        {
            [SnowflakeID]
            public string TraceCode { get; set; } = string.Empty;
        }

        private class OrderWithOverwrite
        {
            [SnowflakeID(OverwriteExisting = true)]
            public long Id { get; set; }
        }

        private class OrderWithoutAttribute
        {
            public long Id { get; set; }
        }

        private class OrderWithUnsupportedType
        {
            [SnowflakeID]
            public int Id { get; set; }
        }

        private class OrderWithMultipleMembers
        {
            [SnowflakeID]
            public long Id { get; set; }

            [SnowflakeID]
            public string TraceCode { get; set; } = string.Empty;

            public string Remark { get; set; } = string.Empty;
        }

        #endregion

        #region Populate - Supported Types

        [Fact]
        public void Populate_WithLongProperty_ShouldFillGeneratedId()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1, dataCenterId: 1);
            var order = new OrderWithLong { Amount = 100m };

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.True(result.Id > 0);
        }

        [Fact]
        public void Populate_WithNullableLongProperty_ShouldFillGeneratedId()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 2, dataCenterId: 1);
            var order = new OrderWithNullableLong();

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.NotNull(result.Id);
            Assert.True(result.Id!.Value > 0);
        }

        [Fact]
        public void Populate_WithStringProperty_ShouldFillGeneratedIdAsString()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 3, dataCenterId: 1);
            var order = new OrderWithString();

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.False(string.IsNullOrEmpty(result.TraceCode));
            Assert.True(long.Parse(result.TraceCode) > 0);
        }

        [Fact]
        public void Populate_WithMultipleMarkedMembers_ShouldFillAll()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 4, dataCenterId: 1);
            var order = new OrderWithMultipleMembers();

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.True(result.Id > 0);
            Assert.False(string.IsNullOrEmpty(result.TraceCode));
        }

        #endregion

        #region Populate - Idempotency / Overwrite

        [Fact]
        public void Populate_WhenValueAlreadySet_ShouldNotOverwriteByDefault()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 5, dataCenterId: 1);
            var order = new OrderWithLong { Id = 123456L };

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.Equal(123456L, result.Id);
        }

        [Fact]
        public void Populate_WithOverwriteExistingTrue_ShouldAlwaysRegenerate()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 6, dataCenterId: 1);
            var order = new OrderWithOverwrite { Id = 123456L };

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.NotEqual(123456L, result.Id);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public void Populate_WithoutAttribute_ShouldNotChangeValue()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 7, dataCenterId: 1);
            var order = new OrderWithoutAttribute();

            // Act
            var result = SnowflakeIdPopulator.Populate(order);

            // Assert
            Assert.Equal(0L, result.Id);
        }

        #endregion

        #region Populate - Generator Resolution

        [Fact]
        public void Populate_WithExplicitGenerator_ShouldUseProvidedGeneratorNotDefault()
        {
            // Arrange: Default instance intentionally NOT initialized
            var explicitGenerator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 9, dataCenterId: 9);
            var order = new OrderWithLong();

            // Act
            var result = SnowflakeIdPopulator.Populate(order, explicitGenerator);

            // Assert
            Assert.True(result.Id > 0);
        }

        [Fact]
        public void Populate_WithoutGeneratorAndDefaultNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var order = new OrderWithLong();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SnowflakeIdPopulator.Populate(order));
        }

        [Fact]
        public void Populate_WithNullInstance_ShouldThrowArgumentNullException()
        {
            // Arrange
            OrderWithLong? order = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SnowflakeIdPopulator.Populate(order!));
        }

        #endregion

        #region Populate - Unsupported Type

        [Fact]
        public void Populate_WithUnsupportedMemberType_ShouldThrowNotSupportedException()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 10, dataCenterId: 1);
            var order = new OrderWithUnsupportedType();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => SnowflakeIdPopulator.Populate(order));
        }

        #endregion

        #region PopulateMany

        [Fact]
        public void PopulateMany_ShouldFillUniqueIdsForAllInstances()
        {
            // Arrange
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 11, dataCenterId: 1);
            var orders = Enumerable.Range(0, 100).Select(_ => new OrderWithLong()).ToList();

            // Act
            SnowflakeIdPopulator.PopulateMany(orders);

            // Assert
            Assert.All(orders, o => Assert.True(o.Id > 0));
            Assert.Equal(orders.Count, orders.Select(o => o.Id).Distinct().Count());
        }

        #endregion

        #region TryPopulateProperty (ORM integration entry point)

        [Fact]
        public void TryPopulateProperty_WithMarkedProperty_ShouldReturnTrueAndFillValue()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 12, dataCenterId: 1);
            var order = new OrderWithLong();
            var property = typeof(OrderWithLong).GetProperty(nameof(OrderWithLong.Id))!;

            // Act
            var handled = SnowflakeIdPopulator.TryPopulateProperty(order, property, generator);

            // Assert
            Assert.True(handled);
            Assert.True(order.Id > 0);
        }

        [Fact]
        public void TryPopulateProperty_WithUnmarkedProperty_ShouldReturnFalse()
        {
            // Arrange
            var generator = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 13, dataCenterId: 1);
            var order = new OrderWithoutAttribute();
            var property = typeof(OrderWithoutAttribute).GetProperty(nameof(OrderWithoutAttribute.Id))!;

            // Act
            var handled = SnowflakeIdPopulator.TryPopulateProperty(order, property, generator);

            // Assert
            Assert.False(handled);
            Assert.Equal(0L, order.Id);
        }

        #endregion
    }
}
