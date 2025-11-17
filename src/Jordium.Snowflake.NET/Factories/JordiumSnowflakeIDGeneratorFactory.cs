using System;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// Provides a factory for creating and managing instances of Snowflake ID generators.
    /// </summary>
    /// <remarks>This class serves as a centralized utility for working with Snowflake ID generation, which
    /// is commonly used for generating unique, time-ordered identifiers in distributed systems.</remarks>
    public static class JordiumSnowflakeIDGeneratorFactory
    {
        private static IIDGenerator? _defaultInstance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Creates a new ID generator instance
        /// </summary>
        /// <param name="workerId">Machine ID</param>
        /// <param name="dataCenterId">Data center ID (optional, default is 0)</param>
        /// <returns>ID generator instance</returns>
        public static IIDGenerator Create(ushort workerId, ushort dataCenterId = 0)
        {
            return new DefaultIDGenerator(new IDGeneratorOptions(workerId, dataCenterId));
        }

        /// <summary>
        /// Creates a new ID generator instance
        /// </summary>
        /// <param name="options">Configuration options</param>
        /// <returns>ID generator instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        public static IIDGenerator Create(IDGeneratorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new DefaultIDGenerator(options);
        }

        /// <summary>
        /// Creates a new ID generator instance with custom configuration
        /// </summary>
        /// <param name="configure">Configuration delegate to customize options</param>
        /// <returns>ID generator instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configure is null</exception>
        public static IIDGenerator Create(Action<IDGeneratorOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new IDGeneratorOptions();
            configure(options);
            return new DefaultIDGenerator(options);
        }

        /// <summary>
        /// Gets the default singleton instance (thread-safe)
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the default instance is not initialized</exception>
        public static IIDGenerator Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    throw new InvalidOperationException(
                        "Default instance is not initialized. Please call JordiumSnowflakeIDGeneratorFactory.InitializeDefault() method first.");
                }
                return _defaultInstance;
            }
        }

        /// <summary>
        /// Initializes the default singleton instance
        /// </summary>
        /// <param name="workerId">Machine ID</param>
        /// <param name="dataCenterId">Data center ID (optional, default is 0)</param>
        /// <exception cref="InvalidOperationException">Thrown when the default instance is already initialized</exception>
        public static void InitializeDefault(ushort workerId, ushort dataCenterId = 0)
        {
            lock (_lock)
            {
                if (_defaultInstance != null)
                {
                    throw new InvalidOperationException("Default instance is already initialized and cannot be reinitialized.");
                }
                _defaultInstance = new DefaultIDGenerator(new IDGeneratorOptions(workerId, dataCenterId));
            }
        }

        /// <summary>
        /// Initializes the default singleton instance
        /// </summary>
        /// <param name="options">Configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the default instance is already initialized</exception>
        public static void InitializeDefault(IDGeneratorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (_lock)
            {
                if (_defaultInstance != null)
                {
                    throw new InvalidOperationException("Default instance is already initialized and cannot be reinitialized.");
                }
                _defaultInstance = new DefaultIDGenerator(options);
            }
        }

        /// <summary>
        /// Initializes the default singleton instance
        /// </summary>
        /// <param name="configure">Configuration delegate</param>
        /// <exception cref="ArgumentNullException">Thrown when configure is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the default instance is already initialized</exception>
        public static void InitializeDefault(Action<IDGeneratorOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            lock (_lock)
            {
                if (_defaultInstance != null)
                {
                    throw new InvalidOperationException("Default instance is already initialized and cannot be reinitialized.");
                }
                var options = new IDGeneratorOptions();
                configure(options);
                _defaultInstance = new DefaultIDGenerator(options);
            }
        }

        /// <summary>
        /// Checks if the default instance has been initialized
        /// </summary>
        public static bool IsDefaultInitialized => _defaultInstance != null;

        /// <summary>
        /// Resets the default instance (use with caution, primarily for testing scenarios)
        /// </summary>
        public static void ResetDefault()
        {
            lock (_lock)
            {
                _defaultInstance = null;
            }
        }
    }
}
