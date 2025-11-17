/* Copyright (c) 2025 大连久元鼎晟科技有限公司. All rights reserved.
 * 创建时间：2025/11/17 14:36:22
 * 创建人：linin
 * 联系方式：ning.li@jordium.com
 * Licensed under the MIT License.   
 */
using Jordium.Snowflake.NET.Extensions;
using Jordium.Snowflake.NET.Web.Services;

namespace Jordium.Snowflake.NET.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        // 注册服务
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Add services to the container.
            services.AddRazorPages();

            // register Jordium Snowflake ID Generator service
            // Case1: use default configuration section "JordiumSnowflakeConfig" from appsettings.json
            //services.AddJordiumSnowflakeIdGenerator();

            // Case2: use custom configuration section name
            services.AddJordiumSnowflakeIdGenerator(_configuration, "MyCustomSnowflakeConfigSection");

            // Case3: use code-based configuration
            //services.AddJordiumSnowflakeIdGenerator(options => { 
            //    options.WorkerId = 1;
            //    options.DataCenterId = 1;
            //});

            // Register Snowflake Service
            services.AddScoped<ISnowflakeService, SnowflakeService>();

            // Add Swagger for API documentation - Only in Development
            if (_environment.IsDevelopment())
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "Jordium Snowflake ID Generator API",
                        Version = "v1.3.0",
                        Description = "A high-performance Snowflake ID generator API",
                        Contact = new Microsoft.OpenApi.Models.OpenApiContact
                        {
                            Name = "Jordium.com",
                            Email = "ning.li@jordium.com"
                        }
                    });
                });
            }
        }

        // 配置 HTTP 管道
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Only enable Swagger in Development environment
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Snowflake API v1");
                    options.RoutePrefix = "swagger";
                });
            }
            else
            {
                // In production, use exception handler
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
