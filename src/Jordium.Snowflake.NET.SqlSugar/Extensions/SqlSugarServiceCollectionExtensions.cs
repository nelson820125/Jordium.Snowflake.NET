using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Linq;
using System.Reflection;

namespace Jordium.Snowflake.NET.SqlSugar.Extensions
{
    /// <summary>
    /// 为 SqlSugar 提供雪花 ID 自动填充能力的扩展方法。
    /// </summary>
    public static class SqlSugarServiceCollectionExtensions
    {
        /// <summary>
        /// 在 <see cref="ISqlSugarClient"/> 上挂载雪花 ID 自动填充的 Aop.DataExecuting 回调。
        /// 每次 Insert 操作时，若实体属性标记了 <see cref="SnowflakeIDAttribute"/> 且满足生成条件，
        /// 会自动调用 <see cref="SnowflakeIdPopulator.TryPopulateProperty"/> 填充雪花 ID。
        /// </summary>
        /// <param name="db">SqlSugar 客户端（支持 SqlSugarClient / SqlSugarScope）</param>
        /// <param name="generator">雪花 ID 生成器实例</param>
        /// <returns>传入的客户端实例，便于链式调用</returns>
        public static ISqlSugarClient UseJordiumSnowflakeId(this ISqlSugarClient db, IIDGenerator generator)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            db.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (entityInfo.OperationType != DataFilterType.InsertByObject)
                    return;

                if (entityInfo.EntityValue == null || string.IsNullOrEmpty(entityInfo.PropertyName))
                    return;

                var propertyInfo = entityInfo.EntityValue
                    .GetType()
                    .GetProperty(entityInfo.PropertyName, BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                    return;

                SnowflakeIdPopulator.TryPopulateProperty(entityInfo.EntityValue, propertyInfo, generator);
            };

            return db;
        }

        /// <summary>
        /// 为已注册的 <see cref="ISqlSugarClient"/> 服务自动挂载雪花 ID 填充能力。
        /// 要求在调用本方法前已注册 <see cref="ISqlSugarClient"/>（以及可选的 <see cref="IIDGenerator"/>，
        /// 若未注册则会使用 <see cref="JordiumSnowflakeIDGeneratorFactory.Default"/>）。
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合，便于链式调用</returns>
        /// <exception cref="InvalidOperationException">未找到已注册的 <see cref="ISqlSugarClient"/> 服务</exception>
        public static IServiceCollection AddJordiumSnowflakeIdForSqlSugar(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(ISqlSugarClient));
            if (descriptor == null)
                throw new InvalidOperationException(
                    "No ISqlSugarClient registration found. Register SqlSugar before calling AddJordiumSnowflakeIdForSqlSugar().");

            services.Remove(descriptor);

            services.Add(new ServiceDescriptor(
                typeof(ISqlSugarClient),
                sp =>
                {
                    var client = (ISqlSugarClient)CreateInstance(descriptor, sp);
                    var generator = sp.GetService<IIDGenerator>() ?? JordiumSnowflakeIDGeneratorFactory.Default;
                    return client.UseJordiumSnowflakeId(generator);
                },
                descriptor.Lifetime));

            return services;
        }

        private static object CreateInstance(ServiceDescriptor descriptor, IServiceProvider provider)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(provider);

            return ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType!);
        }
    }
}
