/* Copyright (c) 2025 大连久元鼎晟科技有限公司. All rights reserved.
 * Licensed under the MIT License.   
 */
using Jordium.Snowflake.NET;
using Microsoft.Extensions.Options;
using System;

namespace Jordium.Snowflake.NET.Web.Services
{
    /// <summary>
    /// Snowflake ID 生成服务
    /// </summary>
    public interface ISnowflakeService
    {
        /// <summary>
        /// 生成新的 Snowflake ID
        /// </summary>
        long GenerateId();

        /// <summary>
        /// 批量生成 Snowflake ID
        /// </summary>
        IEnumerable<long> GenerateIds(int count);

        /// <summary>
        /// 获取当前配置信息
        /// </summary>
        IDGeneratorOptions GetConfiguration();

        /// <summary>
        /// 解析 Snowflake ID
        /// </summary>
        IdInfo ParseId(long id);
    }

    /// <summary>
    /// Snowflake ID 信息
    /// </summary>
    public class IdInfo
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int WorkerId { get; set; }
        public int DataCenterId { get; set; }
        public int Sequence { get; set; }
        public int SeqBitLength { get; set; }
        public int WorkerIdBitLength { get; set; }
        public int DataCenterIdBitLength { get; set; }
    }

    /// <summary>
    /// Snowflake ID 生成服务实现
    /// </summary>
    public class SnowflakeService : ISnowflakeService
    {
        private readonly IIDGenerator _idGenerator;
        private readonly IDGeneratorOptions _options;

        public SnowflakeService(IIDGenerator idGenerator, IOptions<IDGeneratorOptions> options)
        {
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public long GenerateId()
        {
            return _idGenerator.NewLong();
        }

        public IEnumerable<long> GenerateIds(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));

            if (count > 1000)
                throw new ArgumentException("Count cannot exceed 1000", nameof(count));

            var ids = new List<long>(count);
            for (int i = 0; i < count; i++)
            {
                ids.Add(_idGenerator.NewLong());
            }
            return ids;
        }

        public IDGeneratorOptions GetConfiguration()
        {
            return _options;
        }

        public IdInfo ParseId(long id)
        {
            int seqBitLength = _options?.SeqBitLength ?? 12;
            int workerIdBitLength = _options?.WorkerIdBitLength ?? 5;
            int dataCenterIdBitLength = _options?.DataCenterIdBitLength ?? 5;
            int timestampShift = seqBitLength + workerIdBitLength + dataCenterIdBitLength;

            // 1. 提取时间戳（相对于 BaseTime 的毫秒数）
            long timestampMillis = id >> timestampShift;

            long dataCenterIdMask = (1L << dataCenterIdBitLength) - 1;
            var dataCenterId = (int)((id >> (seqBitLength + workerIdBitLength)) & dataCenterIdMask);
            // create worker id mask code
            long workerIdMask = (1L << workerIdBitLength) - 1;
            int workerId = (int)((id >> seqBitLength) & workerIdMask);
            // 创建序列号掩码
            long sequenceMask = (1L << seqBitLength) - 1;
            // 使用掩码提取序列号
            int sequence = (int)(id & sequenceMask);

            // 2. 计算实际时间
            DateTime baseTime = _options?.BaseTime ?? new DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc);

            // BaseTime + 提取的毫秒数
            DateTime actualTimestamp = baseTime.AddMilliseconds(timestampMillis);

            return new IdInfo
            {
                Id = id,
                Timestamp = actualTimestamp,
                WorkerId = workerId,
                DataCenterId = dataCenterId,
                Sequence = sequence,
                SeqBitLength = seqBitLength,
                WorkerIdBitLength = workerIdBitLength,
                DataCenterIdBitLength = dataCenterIdBitLength
            };
        }
    }
}
