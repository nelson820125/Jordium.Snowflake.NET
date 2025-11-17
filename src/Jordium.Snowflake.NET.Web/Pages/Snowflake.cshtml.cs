/* Copyright (c) 2025 大连久元鼎晟科技有限公司. All rights reserved.
 * Licensed under the MIT License.   
 */
using Jordium.Snowflake.NET;
using Jordium.Snowflake.NET.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jordium.Snowflake.NET.Web.Pages
{
    public class SnowflakeModel : PageModel
    {
        private readonly ISnowflakeService _snowflakeService;
        private readonly ILogger<SnowflakeModel> _logger;

        public IDGeneratorOptions Config { get; private set; } = null!;
        public List<long> GeneratedIds { get; private set; } = new();

        public SnowflakeModel(ISnowflakeService snowflakeService, ILogger<SnowflakeModel> logger)
        {
            _snowflakeService = snowflakeService ?? throw new ArgumentNullException(nameof(snowflakeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnGet()
        {
            try
            {
                // 获取配置信息
                Config = _snowflakeService.GetConfiguration();

                // 生成初始的 5 个示例 ID
                GeneratedIds = _snowflakeService.GenerateIds(5).ToList();

                _logger.LogInformation(
                    "Snowflake page loaded. WorkerId: {WorkerId}, DataCenterId: {DataCenterId}, Generated {Count} sample IDs",
                    Config.WorkerId,
                    Config.DataCenterId,
                    GeneratedIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Snowflake page");
                Config = new IDGeneratorOptions(); // 提供默认配置以避免 null 引用
            }
        }

        public IdInfo ParseId(long id)
        {
            return _snowflakeService.ParseId(id);
        }
    }
}
