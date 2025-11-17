/* Copyright (c) 2025 大连久元鼎晟科技有限公司. All rights reserved.
 * Licensed under the MIT License.   
 */
using Jordium.Snowflake.NET.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Runtime;

namespace Jordium.Snowflake.NET.Web.Controllers
{
    /// <summary>
    /// Snowflake ID 生成 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SnowflakeController : ControllerBase
    {
        private readonly ISnowflakeService _snowflakeService;
        private readonly ILogger<SnowflakeController> _logger;

        public SnowflakeController(
            ISnowflakeService snowflakeService,
            ILogger<SnowflakeController> logger)
        {
            _snowflakeService = snowflakeService ?? throw new ArgumentNullException(nameof(snowflakeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 生成单个 Snowflake ID
        /// </summary>
        /// <returns>新生成的 ID</returns>
        [HttpGet("generate")]
        [ProducesResponseType(typeof(GenerateIdResponse), StatusCodes.Status200OK)]
        public IActionResult GenerateId()
        {
            try
            {
                var id = _snowflakeService.GenerateId();
                var idInfo = _snowflakeService.ParseId(id);

                _logger.LogInformation("Generated Snowflake ID: {Id}", id);

                return Ok(new GenerateIdResponse
                {
                    Id = id.ToString(),
                    IdString = id.ToString(),
                    Timestamp = idInfo.Timestamp,
                    WorkerId = idInfo.WorkerId,
                    DataCenterId = idInfo.DataCenterId,
                    Sequence = idInfo.Sequence,
                    SeqBitLength = idInfo.SeqBitLength,
                    WorkerIdBitLength = idInfo.WorkerIdBitLength,
                    DataCenterIdBitLength = idInfo.DataCenterIdBitLength
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Snowflake ID");
                return StatusCode(500, new { error = "Failed to generate ID", message = ex.Message });
            }
        }

        /// <summary>
        /// 批量生成 Snowflake ID
        /// </summary>
        /// <param name="count">生成数量 (1-1000)</param>
        /// <returns>生成的 ID 列表</returns>
        [HttpGet("generate/batch")]
        [ProducesResponseType(typeof(BatchGenerateIdResponse), StatusCodes.Status200OK)]
        public IActionResult GenerateBatch([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 1000)
                {
                    return BadRequest(new { error = "Count must be between 1 and 1000" });
                }

                var ids = _snowflakeService.GenerateIds(count).ToList();

                _logger.LogInformation("Generated {Count} Snowflake IDs", ids.Count);

                List<GenerateIdResponse> idObjs = new();
                foreach (var id in ids)
                {
                    Console.WriteLine($"ID: {id}");
                    var idInfo = _snowflakeService.ParseId(id);
                    Console.WriteLine($"  Sequence: {idInfo.Sequence}");
                    idObjs.Add(new GenerateIdResponse {
                        Id = id.ToString(),
                        IdString = id.ToString(),
                        Timestamp = idInfo.Timestamp,
                        WorkerId = idInfo.WorkerId,
                        DataCenterId = idInfo.DataCenterId,
                        Sequence = idInfo.Sequence,
                        SeqBitLength = idInfo.SeqBitLength,
                        WorkerIdBitLength = idInfo.WorkerIdBitLength,
                        DataCenterIdBitLength = idInfo.DataCenterIdBitLength
                    });
                }

                return Ok(new BatchGenerateIdResponse
                {
                    Count = ids.Count,
                    idObjs = idObjs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch Snowflake IDs");
                return StatusCode(500, new { error = "Failed to generate IDs", message = ex.Message });
            }
        }

        /// <summary>
        /// 解析 Snowflake ID
        /// </summary>
        /// <param name="id">要解析的 ID</param>
        /// <returns>ID 详细信息</returns>
        [HttpGet("parse/{id}")]
        [ProducesResponseType(typeof(ParseIdResponse), StatusCodes.Status200OK)]
        public IActionResult ParseId(long id)
        {
            try
            {
                var idInfo = _snowflakeService.ParseId(id);

                return Ok(new ParseIdResponse
                {
                    Id = id.ToString(),
                    IdString = id.ToString(),
                    Timestamp = idInfo.Timestamp,
                    TimestampUtc = idInfo.Timestamp.ToUniversalTime(),
                    WorkerId = idInfo.WorkerId,
                    DataCenterId = idInfo.DataCenterId,
                    Sequence = idInfo.Sequence,
                    SeqBitLength = idInfo.SeqBitLength,
                    WorkerIdBitLength = idInfo.WorkerIdBitLength,
                    DataCenterIdBitLength = idInfo.DataCenterIdBitLength
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Snowflake ID: {Id}", id);
                return StatusCode(500, new { error = "Failed to parse ID", message = ex.Message });
            }
        }

        /// <summary>
        /// 获取当前配置信息
        /// </summary>
        /// <returns>Snowflake 配置</returns>
        [HttpGet("config")]
        [ProducesResponseType(typeof(ConfigResponse), StatusCodes.Status200OK)]
        public IActionResult GetConfig()
        {
            try
            {
                var config = _snowflakeService.GetConfiguration();

                return Ok(new ConfigResponse
                {
                    WorkerId = config.WorkerId,
                    DataCenterId = config.DataCenterId,
                    Method = config.Method,
                    BaseTime = config.BaseTime,
                    SeqBitLength = config.SeqBitLength,
                    WorkerIdBitLength = config.WorkerIdBitLength,
                    DataCenterIdBitLength = config.DataCenterIdBitLength,
                    TopOverCostCount = config.TopOverCostCount,
                    MaxSeqNumber = config.MaxSeqNumber,
                    MinSeqNumber = config.MinSeqNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration");
                return StatusCode(500, new { error = "Failed to get configuration", message = ex.Message });
            }
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "Snowflake ID Generator",
                timestamp = DateTime.UtcNow
            });
        }
    }

    #region Response Models

    public class GenerateIdResponse
    {
        public string Id { get; set; }
        public string IdString { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int WorkerId { get; set; }
        public int DataCenterId { get; set; }
        public int Sequence { get; set; }
        public int SeqBitLength { get; set; }
        public int WorkerIdBitLength { get; set; }
        public int DataCenterIdBitLength { get; set; }
    }

    public class BatchGenerateIdResponse
    {
        public int Count { get; set; }
        public List<GenerateIdResponse> idObjs { get; set; } = new List<GenerateIdResponse>();
    }

    public class ParseIdResponse
    {
        public string Id { get; set; }
        public string IdString { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int WorkerId { get; set; }
        public int DataCenterId { get; set; }
        public int Sequence { get; set; }
        public int SeqBitLength { get; set; }
        public int WorkerIdBitLength { get; set; }
        public int DataCenterIdBitLength { get; set; }
    }

    public class ConfigResponse
    {
        public ushort WorkerId { get; set; }
        public ushort DataCenterId { get; set; }
        public short Method { get; set; }
        public DateTime BaseTime { get; set; }
        public byte SeqBitLength { get; set; }
        public byte WorkerIdBitLength { get; set; }
        public byte DataCenterIdBitLength { get; set; }
        public int TopOverCostCount { get; set; }
        public int MaxSeqNumber { get; set; }
        public ushort MinSeqNumber { get; set; }
    }

    #endregion
}
