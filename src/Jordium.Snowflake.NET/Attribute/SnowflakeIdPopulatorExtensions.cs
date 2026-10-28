namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// 为任意对象提供便捷的雪花 ID 填充扩展方法。
    /// </summary>
    public static class SnowflakeIdPopulatorExtensions
    {
        /// <summary>
        /// 对当前对象上标记了 <see cref="SnowflakeIDAttribute"/> 的属性生成并填充雪花 ID。
        /// 默认使用 <see cref="JordiumSnowflakeIDGeneratorFactory.Default"/> 单例实例。
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// 当 Default 实例尚未通过 InitializeDefault 初始化时抛出。
        /// </exception>
        public static T GenerateSnowflakeIds<T>(this T instance)
        {
            return SnowflakeIdPopulator.Populate(instance);
        }

        /// <summary>
        /// 对当前对象上标记了 <see cref="SnowflakeIDAttribute"/> 的属性生成并填充雪花 ID，
        /// 使用调用方显式传入的 ID 生成器实例（推荐在 DI 场景下使用，避免依赖静态 Default）。
        /// </summary>
        public static T GenerateSnowflakeIds<T>(this T instance, IIDGenerator generator)
        {
            return SnowflakeIdPopulator.Populate(instance, generator);
        }
    }
}
