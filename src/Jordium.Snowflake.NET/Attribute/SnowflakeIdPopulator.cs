using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// 负责扫描并填充标记了 <see cref="SnowflakeIDAttribute"/> 的属性/字段。
    /// 内部使用类型级反射结果缓存，避免重复反射带来的性能损耗。
    /// </summary>
    public static class SnowflakeIdPopulator
    {
        // 每个 Type 的反射扫描结果仅计算一次，后续直接命中缓存
        private static readonly ConcurrentDictionary<Type, SnowflakeIdMember[]> _memberCache = new ConcurrentDictionary<Type, SnowflakeIdMember[]>();

        /// <summary>
        /// 填充单个对象实例上所有标记了 <see cref="SnowflakeIDAttribute"/> 的成员。
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="instance">目标对象实例</param>
        /// <param name="generator">
        /// 指定使用的 ID 生成器实例；为 null 时使用 <see cref="JordiumSnowflakeIDGeneratorFactory.Default"/>。
        /// </param>
        /// <returns>填充后的同一对象实例（便于链式调用）</returns>
        /// <exception cref="ArgumentNullException">instance 为 null</exception>
        /// <exception cref="InvalidOperationException">未指定 generator 且 Default 实例未初始化</exception>
        public static T Populate<T>(T instance, IIDGenerator? generator = null)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var members = GetOrAddMembers(instance.GetType());
            if (members.Length == 0)
                return instance;

            var effectiveGenerator = generator ?? JordiumSnowflakeIDGeneratorFactory.Default;

            foreach (var member in members)
            {
                PopulateMember(instance, member, effectiveGenerator);
            }

            return instance;
        }

        /// <summary>
        /// 批量填充一组对象实例。
        /// </summary>
        public static void PopulateMany<T>(IEnumerable<T> instances, IIDGenerator? generator = null)
        {
            if (instances == null)
                throw new ArgumentNullException(nameof(instances));

            var effectiveGenerator = generator ?? JordiumSnowflakeIDGeneratorFactory.Default;

            foreach (var instance in instances)
            {
                if (instance == null) continue;

                var members = GetOrAddMembers(instance.GetType());
                foreach (var member in members)
                {
                    PopulateMember(instance, member, effectiveGenerator);
                }
            }
        }

        /// <summary>
        /// 供 ORM 扩展包（如 SqlSugar / EF Core 拦截器）复用的单属性填充入口，
        /// 避免每次 Insert 都重新调用 <see cref="MemberInfo.GetCustomAttribute"/>。
        /// </summary>
        /// <param name="entity">实体实例</param>
        /// <param name="property">反射得到的属性信息（由调用方 ORM 回调提供）</param>
        /// <param name="generator">ID 生成器实例</param>
        /// <returns>若该属性未标记 <see cref="SnowflakeIDAttribute"/>，返回 false；否则返回 true</returns>
        public static bool TryPopulateProperty(object entity, PropertyInfo property, IIDGenerator generator)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var attr = property.GetCustomAttribute<SnowflakeIDAttribute>(inherit: true);
            if (attr == null)
                return false;

            var member = new SnowflakeIdMember(property, attr);
            PopulateMember(entity, member, generator);
            return true;
        }

        private static SnowflakeIdMember[] GetOrAddMembers(Type type)
        {
            return _memberCache.GetOrAdd(type, t =>
            {
                var properties = t
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Select(p => new { Member = (MemberInfo)p, Attr = p.GetCustomAttribute<SnowflakeIDAttribute>(inherit: true) })
                    .Where(x => x.Attr != null);

                var result = new List<SnowflakeIdMember>();
                foreach (var item in properties)
                {
                    var propertyInfo = (PropertyInfo)item.Member;
                    ValidateSupportedType(propertyInfo.PropertyType, propertyInfo.Name, t);
                    result.Add(new SnowflakeIdMember(propertyInfo, item.Attr!));
                }

                return result.ToArray();
            });
        }

        private static void ValidateSupportedType(Type memberType, string memberName, Type declaringType)
        {
            var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (underlying != typeof(long) && underlying != typeof(string))
            {
                throw new NotSupportedException(
                    $"[SnowflakeID] cannot be applied to member '{declaringType.Name}.{memberName}' of type '{memberType.Name}'. " +
                    "Supported types are: long, long?, string.");
            }
        }

        private static void PopulateMember(object instance, SnowflakeIdMember member, IIDGenerator generator)
        {
            var currentValue = member.Property.GetValue(instance);

            if (!member.Attribute.OverwriteExisting && !IsDefaultValue(currentValue))
                return;

            var underlying = Nullable.GetUnderlyingType(member.Property.PropertyType) ?? member.Property.PropertyType;
            var newId = generator.NewLong();

            object valueToSet = underlying == typeof(string) ? newId.ToString() : (object)newId;
            member.Property.SetValue(instance, valueToSet);
        }

        private static bool IsDefaultValue(object? value)
        {
            if (value == null) return true;
            if (value is long l) return l == 0L;
            if (value is string s) return string.IsNullOrEmpty(s);
            return false;
        }

        private readonly struct SnowflakeIdMember
        {
            public SnowflakeIdMember(PropertyInfo property, SnowflakeIDAttribute attribute)
            {
                Property = property;
                Attribute = attribute;
            }

            public PropertyInfo Property { get; }
            public SnowflakeIDAttribute Attribute { get; }
        }
    }
}
