using System;
using System.Threading;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// 默认实现
    /// </summary>
    public class DefaultIDGenerator : IIDGenerator
    {
        /// <summary>
        /// 雪花算法接口实例
        /// </summary>
        private ISnowflakeWorker InternalSnowflakeWorker { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public DefaultIDGenerator(IDGeneratorOptions options)
        {
            if (options == null)
            {
                throw new ApplicationException("options error.");
            }

            if (options.BaseTime < DateTime.Now.AddYears(-50) || options.BaseTime > DateTime.Now)
            {
                throw new ApplicationException("BaseTime error.");
            }

            if (options.SeqBitLength + options.WorkerIdBitLength + options.DataCenterIdBitLength > 22)
            {
                throw new ApplicationException("error：WorkerIdBitLength + DataCenterBitLength + SeqBitLength <= 22.");
            }

            var maxWorkerIdNumber = Math.Pow(2, options.WorkerIdBitLength) - 1;
            if (options.WorkerId < 0 || options.WorkerId > maxWorkerIdNumber)
            {
                throw new ApplicationException("WorkerId error. (range:[0, " + maxWorkerIdNumber + "].");
            }

            var maxDataCenterIdNumber = Math.Pow(2, options.DataCenterIdBitLength) - 1;
            if (options.DataCenterId < 0 || options.DataCenterId > maxDataCenterIdNumber)
            {
                throw new ApplicationException("DataCenterId error. (range:[0, " + maxDataCenterIdNumber + "].");
            }

            if (options.SeqBitLength < 1)
            {
                throw new ApplicationException("SeqBitLength error. SeqBitLength >=1.");
            }

            var maxSeqNumber = Math.Pow(2, options.SeqBitLength) - 1;
            if (options.MaxSeqNumber < 0 || options.MaxSeqNumber > maxSeqNumber)
            {
                throw new ApplicationException("MaxSeqNumber error. (range:[0, " + maxSeqNumber + "].");
            }

            if (options.MinSeqNumber < 0 || options.MinSeqNumber > maxSeqNumber)
            {
                throw new ApplicationException("MinSeqNumber error. (range:[0, " + maxSeqNumber + "].");
            }

            InternalSnowflakeWorker = options.Method switch
            {
                1 => new SnowflakeWorkerV1(options),
                2 => new SnowflakeWorkerV2(options),
                3 => new SnowflakeWorkerV3(options),  // [2025–11–15] 新增无锁版本
                _ => new SnowflakeWorkerV1(options),
            };

            if (options.Method == 1)
            {
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 生成雪花 ID 过程中的异步事件
        /// </summary>
        public Action<OverCostActionArg> GenIdActionAsync
        {
            get => InternalSnowflakeWorker.GenAction;
            set => InternalSnowflakeWorker.GenAction = value;
        }

        /// <summary>
        /// 生成新的 long 类型数据
        /// </summary>
        /// <returns></returns>
        public long NewLong()
        {
            return InternalSnowflakeWorker.NextId();
        }
    }
}