using System;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// Legacy Snowflake ID generator static class
    /// </summary>
    /// <remarks>
    /// This class is deprecated and will be removed in a future version.
    /// Please use <see cref="JordiumSnowflakeIDGeneratorFactory"/> instead for better thread safety and flexibility.
    /// <para>Migration Guide:</para>
    /// <list type="bullet">
    /// <item>Replace <c>IDGenerator.SetIdGenerator(options)</c> with <c>JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options)</c></item>
    /// <item>Replace <c>IDGenerator.NextId()</c> with <c>JordiumSnowflakeIDGeneratorFactory.Default.NewLong()</c></item>
    /// <item>Replace <c>IDGenerator.Instance</c> with <c>JordiumSnowflakeIDGeneratorFactory.Default</c></item>
    /// </list>
    /// </remarks>
    [Obsolete("This class is deprecated. Use JordiumSnowflakeIDGeneratorFactory for better thread safety and more features. This class will be removed in version 3.0.", false)]
    public static class IDGenerator
    {
        private static IIDGenerator instance = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        /// <remarks>
        /// This property is deprecated. Use <see cref="JordiumSnowflakeIDGeneratorFactory.Default"/> instead.
        /// </remarks>
        [Obsolete("Use JordiumSnowflakeIDGeneratorFactory.Default instead. This property will be removed in version 3.0.", false)]
        public static IIDGenerator Instance => instance;

        /// <summary>
        /// Sets the ID generator options (deprecated, not thread-safe in older versions)
        /// </summary>
        /// <param name="options">Configuration options</param>
        /// <remarks>
        /// This method is deprecated. Use <see cref="JordiumSnowflakeIDGeneratorFactory.InitializeDefault(IDGeneratorOptions)"/> instead.
        /// </remarks>
        [Obsolete("Use JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options) instead for better thread safety. This method will be removed in version 3.0.", false)]
        public static void SetIdGenerator(IDGeneratorOptions options)
        {
            lock (_lock)
            {
                instance = new DefaultIDGenerator(options);
            }
        }

        /// <summary>
        /// Generates a new ID (deprecated)
        /// </summary>
        /// <returns>Generated Snowflake ID</returns>
        /// <remarks>
        /// This method is deprecated. Use <see cref="JordiumSnowflakeIDGeneratorFactory.Default"/> instead.
        /// <para>Note: If not initialized, this method will auto-initialize with WorkerId=1, which may cause ID conflicts in distributed systems.</para>
        /// </remarks>
        [Obsolete("Use JordiumSnowflakeIDGeneratorFactory.Default.NewLong() instead. This method will be removed in version 3.0.", false)]
        public static long NextId()
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        // Auto-initialize with default WorkerId=1 (not recommended for production)
                        instance = new DefaultIDGenerator(new IDGeneratorOptions() { WorkerId = 1 });
                    }
                }
            }

            return instance.NewLong();
        }
    }
}