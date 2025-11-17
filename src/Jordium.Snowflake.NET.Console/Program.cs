using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jordium.Snowflake.NET;

namespace Jordium.Snowflake.NET.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("=== Jordium Snowflake ID Generator - Factory Pattern Demo ===\n");

            // Demo 1: Factory Pattern - Create individual instances
            Demo1_FactoryPattern();

            System.Console.WriteLine("\n" + new string('-', 60) + "\n");

            // Demo 2: Singleton Pattern - Global default instance
            Demo2_SingletonPattern();

            System.Console.WriteLine("\n" + new string('-', 60) + "\n");

            // Demo 3: Configuration with delegate
            Demo3_ConfigurationDelegate();

            System.Console.WriteLine("\n" + new string('-', 60) + "\n");

            // Demo 4: Concurrent ID generation
            Demo4_ConcurrentGeneration();

            System.Console.WriteLine("\n" + new string('-', 60) + "\n");

            // Demo 5: Different algorithm methods
            Demo5_DifferentMethods();

            System.Console.WriteLine("\n=== Demo Complete ===");
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadLine();
        }

        /// <summary>
        /// Demo 1: Factory Pattern - Create independent instances
        /// </summary>
        static void Demo1_FactoryPattern()
        {
            System.Console.WriteLine("Demo 1: Factory Pattern - Creating Independent Instances");
            System.Console.WriteLine("--------------------------------------------------------");

            // Method 1: Create with WorkerId and DataCenterId
            var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);
            System.Console.WriteLine($"Generator 1 (WorkerId=1, DataCenterId=1): {generator1.NewLong()}");

            // Method 2: Create with options object
            var options = new IDGeneratorOptions(workerId: 2, dataCenterId: 1)
            {
                Method = 1  // Drift algorithm
            };
            var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(options);
            System.Console.WriteLine($"Generator 2 (WorkerId=2, DataCenterId=1): {generator2.NewLong()}");

            // Method 3: Create with configuration delegate
            var generator3 = JordiumSnowflakeIDGeneratorFactory.Create(opt =>
            {
                opt.WorkerId = 3;
                opt.DataCenterId = 1;
                opt.Method = 1;
                opt.BaseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            });
            System.Console.WriteLine($"Generator 3 (WorkerId=3, DataCenterId=1): {generator3.NewLong()}");

            // Generate multiple IDs
            System.Console.WriteLine("\nGenerating 5 IDs from Generator 1:");
            for (int i = 0; i < 5; i++)
            {
                System.Console.WriteLine($"  ID {i + 1}: {generator1.NewLong()}");
            }
        }

        /// <summary>
        /// Demo 2: Singleton Pattern - Use global default instance
        /// </summary>
        static void Demo2_SingletonPattern()
        {
            System.Console.WriteLine("Demo 2: Singleton Pattern - Global Default Instance");
            System.Console.WriteLine("---------------------------------------------------");

            // Check if default instance is initialized
            if (!JordiumSnowflakeIDGeneratorFactory.IsDefaultInitialized)
            {
                System.Console.WriteLine("Default instance not initialized. Initializing now...");
                
                // Initialize the default singleton instance
                JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 10, dataCenterId: 2);
                System.Console.WriteLine("> Default instance initialized successfully!");
            }

            // Use the default instance anywhere in your application
            long id1 = JordiumSnowflakeIDGeneratorFactory.Default.NewLong();
            long id2 = JordiumSnowflakeIDGeneratorFactory.Default.NewLong();
            long id3 = JordiumSnowflakeIDGeneratorFactory.Default.NewLong();

            System.Console.WriteLine($"\nGenerated IDs using Default instance:");
            System.Console.WriteLine($"  ID 1: {id1}");
            System.Console.WriteLine($"  ID 2: {id2}");
            System.Console.WriteLine($"  ID 3: {id3}");

            // Try to initialize again (will throw exception)
            System.Console.WriteLine("\nTrying to initialize default instance again...");
            try
            {
                JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 99);
            }
            catch (InvalidOperationException ex)
            {
                System.Console.WriteLine($"> Expected error: {ex.Message}");
            }

            // Reset for next demo (only use in testing scenarios!)
            System.Console.WriteLine("\nResetting default instance for next demo...");
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
            System.Console.WriteLine("> Default instance reset complete");
        }

        /// <summary>
        /// Demo 3: Configuration with delegate for complex setup
        /// </summary>
        static void Demo3_ConfigurationDelegate()
        {
            System.Console.WriteLine("Demo 3: Advanced Configuration with Delegate");
            System.Console.WriteLine("--------------------------------------------");

            // Initialize with advanced configuration
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(options =>
            {
                options.WorkerId = 5;
                options.DataCenterId = 3;
                options.Method = 1;  // Drift algorithm
                options.SeqBitLength = 12;
                options.WorkerIdBitLength = 5;
                options.DataCenterIdBitLength = 5;
                options.BaseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                options.TopOverCostCount = 2000;
            });

            System.Console.WriteLine("Configuration:");
            System.Console.WriteLine("  - WorkerId: 5");
            System.Console.WriteLine("  - DataCenterId: 3");
            System.Console.WriteLine("  - Method: Drift Algorithm");
            System.Console.WriteLine("  - Base Time: 2024-01-01");

            System.Console.WriteLine("\nGenerated IDs:");
            for (int i = 0; i < 3; i++)
            {
                System.Console.WriteLine($"  ID {i + 1}: {JordiumSnowflakeIDGeneratorFactory.Default.NewLong()}");
            }

            // Cleanup
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        /// <summary>
        /// Demo 4: Concurrent ID generation (thread-safe)
        /// </summary>
        static void Demo4_ConcurrentGeneration()
        {
            System.Console.WriteLine("Demo 4: Concurrent ID Generation (Thread-Safe)");
            System.Console.WriteLine("----------------------------------------------");

            // Initialize default instance
            JordiumSnowflakeIDGeneratorFactory.InitializeDefault(workerId: 1, dataCenterId: 1);

            var ids = new System.Collections.Concurrent.ConcurrentBag<long>();
            var tasks = new List<Task>();

            System.Console.WriteLine("Generating 1000 IDs across 10 concurrent threads...\n");

            // Create 10 concurrent tasks, each generating 100 IDs
            for (int t = 0; t < 10; t++)
            {
                int threadId = t;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        long id = JordiumSnowflakeIDGeneratorFactory.Default.NewLong();
                        ids.Add(id);
                    }
                    System.Console.WriteLine($"Thread {threadId} completed - Generated 100 IDs");
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Verify uniqueness
            var uniqueIds = new HashSet<long>(ids);
            System.Console.WriteLine($"\nResults:");
            System.Console.WriteLine($"  Total IDs generated: {ids.Count}");
            System.Console.WriteLine($"  Unique IDs: {uniqueIds.Count}");
            System.Console.WriteLine($"  Duplicates: {ids.Count - uniqueIds.Count}");
            System.Console.WriteLine($"  > All IDs are unique: {ids.Count == uniqueIds.Count}");

            // Show first and last few IDs
            var sortedIds = ids.OrderBy(x => x).ToArray();
            System.Console.WriteLine($"\nFirst 3 IDs: {sortedIds[0]}, {sortedIds[1]}, {sortedIds[2]}");
            System.Console.WriteLine($"Last 3 IDs: {sortedIds[^3]}, {sortedIds[^2]}, {sortedIds[^1]}");

            // Cleanup
            JordiumSnowflakeIDGeneratorFactory.ResetDefault();
        }

        /// <summary>
        /// Demo 5: Compare different algorithm methods
        /// </summary>
        static void Demo5_DifferentMethods()
        {
            System.Console.WriteLine("Demo 5: Different Algorithm Methods");
            System.Console.WriteLine("-----------------------------------");

            // Method 1: Drift Algorithm (default, recommended)
            var driftGenerator = JordiumSnowflakeIDGeneratorFactory.Create(opt =>
            {
                opt.WorkerId = 1;
                opt.DataCenterId = 1;
                opt.Method = 1;  // Drift
            });

            // Method 2: Traditional Algorithm
            var traditionalGenerator = JordiumSnowflakeIDGeneratorFactory.Create(opt =>
            {
                opt.WorkerId = 1;
                opt.DataCenterId = 1;
                opt.Method = 2;  // Traditional
            });

            System.Console.WriteLine("Method 1 - Drift Algorithm (Recommended):");
            for (int i = 0; i < 3; i++)
            {
                System.Console.WriteLine($"  ID {i + 1}: {driftGenerator.NewLong()}");
            }

            System.Console.WriteLine("\nMethod 2 - Traditional Algorithm:");
            for (int i = 0; i < 3; i++)
            {
                System.Console.WriteLine($"  ID {i + 1}: {traditionalGenerator.NewLong()}");
            }

            System.Console.WriteLine("\nNote: Both methods generate unique IDs, but Drift Algorithm");
            System.Console.WriteLine("      handles high concurrency scenarios more efficiently.");
        }
    }
}
