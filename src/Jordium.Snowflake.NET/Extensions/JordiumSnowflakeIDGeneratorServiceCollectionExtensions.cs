using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Jordium.Snowflake.NET.Extensions
{
    /// <summary>
    /// Jordium.Snowflake.NET ID Generator Service Collection Extensions
    /// </summary>
    public static class JordiumSnowflakeIDGeneratorServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Jordium Snowflake ID generator service (recommended for ASP.NET Core)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Optional configuration delegate</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddJordiumSnowflakeIdGenerator(
            this IServiceCollection services,
            Action<IDGeneratorOptions>? configure = null)
        {
            // Bind from appsettings.json section
            services.AddOptions<IDGeneratorOptions>()
                .Configure<IConfiguration>((opts, config) =>
                {
                    var section = config.GetSection("JordiumSnowflakeConfig");
                    if (section.Exists())
                        section.Bind(opts);
                });

            // Override with code configuration
            if (configure != null)
                services.Configure(configure);

            // Register core generator (singleton)
            services.AddSingleton<IIDGenerator>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<IDGeneratorOptions>>().Value;
                return new DefaultIDGenerator(options);
            });

            return services;
        }

        /// <summary>
        /// Adds Jordium Snowflake ID generator service (with configuration section)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="sectionName">Configuration section name, default is "JordiumSnowflakeConfig"</param>
        /// <returns>Service collection</returns>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        public static IServiceCollection AddJordiumSnowflakeIdGenerator(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "JordiumSnowflakeConfig")
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or whitespace.", nameof(sectionName));

            var section = configuration.GetSection(sectionName);
            
            // Check if section exists
            if (section.Exists())
            {
                services.Configure<IDGeneratorOptions>(opts => section.Bind(opts));
            }
            else
            {
                // If configuration section doesn't exist, use default configuration
                services.Configure<IDGeneratorOptions>(options =>
                {
                    options.Method = 1;
                    options.WorkerId = 1;
                    options.DataCenterId = 1;
                });
            }

            services.AddSingleton<IIDGenerator>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<IDGeneratorOptions>>().Value;
                return new DefaultIDGenerator(options);
            });

            return services;
        }

        /// <summary>
        /// Adds Jordium Snowflake ID generator service (with direct options object)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="options">Configuration options</param>
        /// <returns>Service collection</returns>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        public static IServiceCollection AddJordiumSnowflakeIdGenerator(
            this IServiceCollection services,
            IDGeneratorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.AddSingleton<IIDGenerator>(new DefaultIDGenerator(options));
            return services;
        }
    }
}
