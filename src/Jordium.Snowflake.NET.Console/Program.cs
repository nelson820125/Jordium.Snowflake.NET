using System;
using Jordium.Snowflake.NET;

namespace Jordium.Snowflake.NET.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 创建 ID 生成器
            var options = new IDGeneratorOptions
            {
                WorkerId = 1,        // 机器 ID (0-31)
                DataCenterId = 1,    // 数据中心 ID (0-31)
                Method = 1           // 算法版本 (1=漂移, 2=传统, 3=无锁)
            };

            var generator = new DefaultIDGenerator(options);

            // 生成 ID
            long id = generator.NewLong();
            System.Console.WriteLine($"Generated ID: {id}");
            System.Console.ReadLine();
        }
    }
}
