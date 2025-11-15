using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Jordium.Snowflake.NET
{
    /// <summary>
    /// 雪花算法 - 无锁实现 (Method 3)
    /// 基于 CAS 操作的无锁并发实现，时间戳缓存在 State 中
    /// </summary>
    internal class SnowflakeWorkerV3 : ISnowflakeWorker
    {
        protected readonly DateTime BaseTime;
        protected readonly ushort WorkerId;
        protected readonly ushort DataCenterId;
        protected readonly byte WorkerIdBitLength;
        protected readonly byte DataCenterIdBitLength;
        protected readonly byte SeqBitLength;
        protected readonly int MaxSeqNumber;
        protected readonly ushort MinSeqNumber;
        protected readonly byte _TimestampShift;

        /// <summary>
        /// 状态：高位存储时间戳，低位存储序列号
        /// </summary>
        private long _state;

        /// <summary>
        /// 序列号掩码
        /// </summary>
        private readonly long _seqMask;

        public SnowflakeWorkerV3(IDGeneratorOptions options)
        {
            BaseTime = options.BaseTime != DateTime.MinValue 
                ? options.BaseTime 
                : new DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc);

            WorkerId = options.WorkerId >= 0 
                ? options.WorkerId 
                : (ushort)DateTime.Now.Millisecond;

            DataCenterId = options.DataCenterId >= 0 
                ? options.DataCenterId 
                : (ushort)DateTime.Now.Millisecond;

            WorkerIdBitLength = options.WorkerIdBitLength > 0 
                ? options.WorkerIdBitLength 
                : (byte)5;

            DataCenterIdBitLength = options.DataCenterIdBitLength > 0 
                ? options.DataCenterIdBitLength 
                : (byte)5;

            SeqBitLength = options.SeqBitLength > 0 
                ? options.SeqBitLength 
                : (byte)12;

            MinSeqNumber = options.MinSeqNumber;

            MaxSeqNumber = options.MaxSeqNumber > 0 
                ? options.MaxSeqNumber 
                : (int)Math.Pow(2, SeqBitLength) - 1;

            _TimestampShift = (byte)(DataCenterIdBitLength + WorkerIdBitLength + SeqBitLength);
            _seqMask = (1L << SeqBitLength) - 1;

            long initialTimestamp = GetCurrentTimeTick();
            _state = (initialTimestamp << SeqBitLength) | MinSeqNumber;
        }

        public Action<OverCostActionArg> GenAction { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual long NextId()
        {
            while (true)
            {
                long currentState = Volatile.Read(ref _state);
                long lastTimestamp = currentState >> SeqBitLength;
                int lastSeq = (int)(currentState & _seqMask);

                int newSeq = lastSeq + 1;
                long newTimestamp;

                if (newSeq <= MaxSeqNumber)
                {
                    newTimestamp = lastTimestamp;
                }
                else
                {
                    newTimestamp = GetNextTimestamp(lastTimestamp);
                    newSeq = MinSeqNumber;
                }

                long newState = (newTimestamp << SeqBitLength) | newSeq;

                if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                {
                    return BuildSnowflakeId(newTimestamp, newSeq);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetNextTimestamp(long lastTimestamp)
        {
            long timestamp = GetCurrentTimeTick();

            if (timestamp < lastTimestamp)
            {
                long timeDiff = lastTimestamp - timestamp;
                
                if (timeDiff > 1000)
                {
                    throw new Exception($"Clock moved backwards. Refusing to generate id for {timeDiff} milliseconds");
                }
                
                Thread.Sleep((int)timeDiff + 1);
                timestamp = GetCurrentTimeTick();
            }
            else if (timestamp == lastTimestamp)
            {
                SpinWait spinWait = new SpinWait();
                do
                {
                    spinWait.SpinOnce();
                    timestamp = GetCurrentTimeTick();
                } while (timestamp <= lastTimestamp);
            }

            return timestamp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual long GetCurrentTimeTick()
        {
            return (long)(DateTime.UtcNow - BaseTime).TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long BuildSnowflakeId(long timestamp, int sequence)
        {
            return (timestamp << _TimestampShift)
                 | ((long)DataCenterId << (WorkerIdBitLength + SeqBitLength))
                 | ((long)WorkerId << SeqBitLength)
                 | sequence;
        }
    }
}
