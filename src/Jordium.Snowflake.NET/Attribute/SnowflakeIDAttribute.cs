using System;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// 标记该属性/字段需要由雪花算法自动生成唯一 ID。
    /// 支持的目标类型：<see cref="long"/>、<see cref="Nullable{Int64}"/>、<see cref="string"/>。
    /// </summary>
    /// <remarks>
    /// 该特性本身不会自动触发赋值行为，必须配合 <see cref="SnowflakeIdPopulator"/>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class SnowflakeIDAttribute : Attribute
    {
        /// <summary>
        /// 是否覆盖已存在的非默认值。
        /// 默认为 false：仅当当前值为 0 / null / 空字符串时才会填充，保证重复调用的幂等性。
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
    }
}
