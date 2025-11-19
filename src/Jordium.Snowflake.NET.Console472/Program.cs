using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jordium.Snowflake.NET.Console472
{
    internal class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("=== Jordium Snowflake ID Generator under net framework 4.7.2 - Factory Pattern Demo ===\n");
            // Method 1: Create with WorkerId and DataCenterId
            var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);
            System.Console.WriteLine($"Generator 1 (WorkerId=1, DataCenterId=1): {generator1.NewLong()}");
        }
    }
}
