# Jordium.Snowflake.NET

<p align="center">
  <img src="https://iili.io/fJBcoPV.png" alt="Jordium.Snowflake.NET Logo" width="200"/>
</p>

English | [简体中文](./README.md)

[![NuGet](https://img.shields.io/nuget/v/Jordium.Snowflake.NET.svg)](https://www.nuget.org/packages/Jordium.Snowflake.NET/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

High-performance distributed ID generator based on Twitter's Snowflake algorithm for .NET. Supports three implementation methods for different scenarios.

## Features

- ✅ **Globally Unique**: Supports multi-datacenter, multi-machine deployment
- 📈 **Trend Increasing**: IDs increase by timestamp, optimized for database indexing
- ⚡ **High Performance**: 2 million - 20 million IDs/second per machine (actual results may vary depending on test environment)
- 🔧 **Multiple Implementations**: Drift algorithm, Traditional algorithm, Lock-free algorithm
- ⏰ **Clock Rollback Handling**: Automatic handling of system time rollback
- 🎯 **Flexible Configuration**: Customizable bit allocation

## ID Structure

### Standard 64-bit Snowflake ID

```
┌─────────────────┬─────────────┬─────────────┬──────────────┐
│ Timestamp(41bit)│ DC ID (5bit)│ Worker(5bit)│ Sequence(12) │
└─────────────────┴─────────────┴─────────────┴──────────────┘
  ~69 years        32 DCs        32 Workers   4096/ms
```

### Bit Allocation Examples

| Config          | Worker Bits | DC Bits | Seq Bits | Capacity                    |
| --------------- | ----------- | ------- | -------- | --------------------------- |
| Standard        | 5           | 5       | 12       | 32 DC × 32 Worker × 4096/ms |
| Multi-DC        | 4           | 6       | 12       | 64 DC × 16 Worker × 4096/ms |
| Multi-Worker    | 6           | 4       | 12       | 16 DC × 64 Worker × 4096/ms |
| Low Concurrency | 5           | 5       | 10       | 32 DC × 32 Worker × 1024/ms |

**Constraints**:

- Worker bits + DC bits + Seq bits ≤ 22
- Seq bits ≤ 12

## Installation

### NuGet CLI

```bash
Install-Package Jordium.Snowflake.NET
```

### .NET CLI

```bash
dotnet add package Jordium.Snowflake.NET
```

### PackageReference

```xml
<PackageReference Include="Jordium.Snowflake.NET" Version="1.2.0" />
```

## Getting Started (v1.2.0)

### Client Application

#### 1. Import Namespace

```csharp
using Jordium.Snowflake.NET;
```

#### 2. Create Instance Using Factory Pattern

```csharp
// Method 1: Create instance using WorkerId and DataCenterId
var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);
System.Console.WriteLine($"Generator 1 (WorkerId=1, DataCenterId=1): {generator1.NewLong()}");
```

```csharp
// Method 2: Create instance using IDGeneratorOption object
var options = new IDGeneratorOptions(workerId: 2, dataCenterId: 1)
{
    Method = 1
};
var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(options);
System.Console.WriteLine($"Generator 2 (WorkerId=2, DataCenterId=1): {generator2.NewLong()}");
```

```csharp
// Method 3: Create instance using configuration delegate
var generator3 = JordiumSnowflakeIDGeneratorFactory.Create(opt =>
{
    opt.WorkerId = 3;
    opt.DataCenterId = 1;
    opt.Method = 1;
    opt.BaseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
});
System.Console.WriteLine($"Generator 3 (WorkerId=3, DataCenterId=1): {generator3.NewLong()}");
```

### ASP.NET Core Dependency Injection (v1.2.0+ Easier Registration, More Standard ASP.NET Core Approach)

#### 1. Import Namespace

```csharp
using Jordium.Snowflake.NET.Extensions;
```

#### 2. Register Jordium.Snowflake.NET Service

```csharp
// Program.cs OR Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // use code-based configuration
    services.AddJordiumSnowflakeIdGenerator(options => {
        options.WorkerId = 1;
        options.DataCenterId = 1;
    });
}

// Controller
public class OrderController : ControllerBase
{
    private readonly IIDGenerator _idGenerator;

    public OrderController(IIDGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    [HttpPost]
    public IActionResult CreateOrder()
    {
        long orderId = _idGenerator.NewLong();
        // ID Generated
        return Ok(new { OrderId = orderId });
    }
}
```

#### 3. Supports 3 Registration Methods

```csharp
// Method 1: Read default "JordiumSnowflakeConfig" configuration from appsettings.json
services.AddJordiumSnowflakeIdGenerator();

// Configuration in appsettings.json:
"JordiumSnowflakeConfig": {
  "DataCenterId": 1,
  "WorkerId": 2,
  "Method": 1,
  "WorkerIdBitLength": 5,
  "DataCenterIdBitLength": 5,
  "SequenceBitLength": 12
}
```

```csharp
// Method 2: Use custom configuration section "MyCustomSnowflakeConfigSection"
services.AddJordiumSnowflakeIdGenerator(_configuration, "MyCustomSnowflakeConfigSection");

// Configuration in appsettings.json:
"MyCustomSnowflakeConfigSection": {
  "DataCenterId": 1,
  "WorkerId": 2,
  "Method": 1,
  "WorkerIdBitLength": 5,
  "DataCenterIdBitLength": 5,
  "SequenceBitLength": 12
}
```

```csharp
// Method 3: Code-based configuration
services.AddJordiumSnowflakeIdGenerator(options => {
    options.WorkerId = 1;
    options.DataCenterId = 1;
    ...other properties
});
```

## IDGeneratorOptions Property Description

| Property Name             | Type     | Description                                                                                                                        | Default Value                                         |
| ------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| **Method**                | short    | Calculation method (1-Drift algorithm, 2-Traditional algorithm, 3-Lock-free algorithm)                                             | 1                                                     |
| **BaseTime**              | DateTime | Start time (UTC format), cannot exceed current system time                                                                         | DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc) |
| **WorkerId**              | ushort   | Machine code                                                                                                                       | 0                                                     |
| **DataCenterId**          | ushort   | Data center identifier                                                                                                             | 0                                                     |
| **WorkerIdBitLength**     | byte     | Machine code bit length. Recommended range: 1-5 (requirement: sequence bits + data identifier + machine code bits ≤ 22).           | 5                                                     |
| **DataCenterIdBitLength** | byte     | Data center identifier bit length. Recommended range: 1-5 (requirement: sequence bits + data identifier + machine code bits ≤ 22). | 5                                                     |
| **SeqBitLength**          | byte     | Sequence bit length. Recommended range: 1-12 (requirement: sequence bits + machine code bits ≤ 22) 4096.                           | 12                                                    |
| **MaxSeqNumber**          | int      | Maximum sequence number (inclusive). (Maximum value calculated by SeqBitLength)                                                    | 0                                                     |
| **MinSeqNumber**          | int      | Minimum sequence number (inclusive). Default 0, not less than 0, not greater than MaxSeqNumber                                     | 0                                                     |
| **TopOverCostCount**      | int      | Maximum drift count (inclusive). Default 2000, recommended range 500-10000 (related to computing power)                            | 2000                                                  |

## Supported Target Frameworks

- .NET Framework (>= 4.6.1)
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10
- .NET Standard (>= 2.0)

## Built-in Algorithm Comparison (see source code provided in the open-source library)

> The code blocks mentioned in this section are for unit testing, directly instantiated and demonstrated based on the underlying code, not using dependency injection or factory pattern instances.

### Three Algorithms

| Algorithm    | Implementation    | Use Case                           | Performance | Clock Rollback           | Recommended |
| ------------ | ----------------- | ---------------------------------- | ----------- | ------------------------ | ----------- |
| **Method 1** | SnowflakeWorkerV1 | Monolith app, high concurrency     | ⭐⭐⭐⭐    | ✅ Special sequence      | ⭐⭐⭐⭐⭐  |
| **Method 2** | SnowflakeWorkerV2 | General, standard implementation   | ⭐⭐⭐⭐    | ❌ Throws exception      | ⭐⭐⭐      |
| **Method 3** | SnowflakeWorkerV3 | Distributed cluster, microservices | ⭐⭐⭐⭐⭐  | ⚠️ Large rollback throws | ⭐⭐⭐⭐    |

### Detailed Description

#### Method 1 - Drift Algorithm (Recommended)

**Features**:

- ✅ Auto-handles clock rollback (uses reserved sequence 1-4)
- ✅ Auto "drift" to future time when sequence overflows
- ✅ Complete event callback mechanism
- 🔒 Uses `lock` for synchronization

**Use Cases**:

- Monolithic applications (multiple threads competing for same WorkerId)
- Business sensitive to clock rollback
- Systems requiring ID generation monitoring

**Configuration Example**:

```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 1,
    TopOverCostCount = 2000  // Max drift count
};

var generator = new DefaultIDGenerator(options);

// Optional: Listen to events
generator.GenIdActionAsync = arg =>
{
    if (arg.ActionType == 1) // Begin drift
    {
        Console.WriteLine($"Begin drift: WorkerId={arg.WorkerId}");
    }
};
```

---

#### Method 2 - Traditional Algorithm

**Features**:

- ✅ Standard Snowflake implementation
- ✅ Simple logic, easy to understand
- ❌ Throws exception on clock rollback
- 🔒 Uses `lock` for synchronization

**Use Cases**:

- Learning and understanding Snowflake algorithm
- Stable environment with rare clock rollback
- Simple scenarios without complex features

**Configuration Example**:

```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 2
};

var generator = new DefaultIDGenerator(options);

try
{
    long id = generator.NewLong();
}
catch (Exception ex)
{
    // Throws exception on clock rollback
    Console.WriteLine($"Clock rollback error: {ex.Message}");
}
```

---

#### Method 3 - Lock-Free Algorithm (High Performance)

**Features**:

- ✅ CAS lock-free concurrency, highest performance
- ✅ Timestamp cached in state, reduces system calls
- ✅ Significant performance advantage in distributed scenarios
- ⚠️ Short rollback auto-sleeps and waits
- ⚠️ Large rollback (> 1000ms) throws exception

**Use Cases**:

- Microservice clusters (each service has independent WorkerId)
- Kubernetes deployment (each Pod has independent WorkerId)
- High-concurrency distributed systems
- Scenarios pursuing extreme performance

**Configuration Example**:

```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 3
};

var generator = new DefaultIDGenerator(options);

// Lock-free performs best in multi-WorkerId scenarios
// Example: Microservice cluster
// Service 1: WorkerId = 1
// Service 2: WorkerId = 2
// Service 3: WorkerId = 3
// ...
```

### Performance Comparison

#### Single WorkerId Contention (8 threads)

| Algorithm | Throughput   | Note                                                 |
| --------- | ------------ | ---------------------------------------------------- |
| Method 1  | 6-8M IDs/sec | Lock performs stable under single-machine contention |
| Method 2  | 5-7M IDs/sec | Standard implementation, medium performance          |
| Method 3  | 4-6M IDs/sec | CAS conflict causes performance degradation          |

**Conclusion**: For single WorkerId high concurrency, recommend **Method 1**

---

#### Multi WorkerId Independent Scenario (8 WorkerIds)

| Algorithm | Throughput         | Note                                 |
| --------- | ------------------ | ------------------------------------ |
| Method 1  | 8-12M IDs/sec      | Lock has fixed overhead              |
| Method 2  | 7-11M IDs/sec      | Standard implementation              |
| Method 3  | **12-18M IDs/sec** | Lock-free advantage is significant ? |

**Conclusion**: For multi-WorkerId distributed scenarios, recommend **Method 3**

## Performance Test Report

### Test Environment

- **CPU**: Intel Core i7-12700K (12 cores, 20 threads) @ 3.6GHz
- **RAM**: 32GB DDR4 3200MHz
- **OS**: Windows 11 Pro
- **.NET Version**: .NET 8.0
- **Test Mode**: Release build, no debugger

### Single Machine Performance (Method 1)

| Threads | Throughput     | Avg Latency    | Note                         |
| ------- | -------------- | -------------- | ---------------------------- |
| 1       | 12-15M IDs/sec | 0.067-0.083 μs | Single thread, no contention |
| 2       | 9-12M IDs/sec  | 0.167-0.222 μs | Light contention             |
| 4       | 7-9M IDs/sec   | 0.444-0.571 μs | Medium contention            |
| 8       | 6-8M IDs/sec   | 1.000-1.333 μs | Heavy contention             |

### Distributed Performance (Method 3)

| WorkerIds | Count Each | Total Throughput | Performance Gain |
| --------- | ---------- | ---------------- | ---------------- |
| 2         | 50,000     | 10-13M IDs/sec   | +10-15%          |
| 4         | 25,000     | 12-15M IDs/sec   | +35-45%          |
| 8         | 12,500     | 14-18M IDs/sec   | +50-60%          |

### Correctness Tests

| Test Item                        | Count | Duplicates | Result  |
| -------------------------------- | ----- | ---------- | ------- |
| Single WorkerId Concurrent       | 1M    | 0          | ✅ Pass |
| Multi WorkerId Concurrent        | 1M    | 0          | ✅ Pass |
| Extreme Concurrency (32 threads) | 3.2M  | 0          | ✅ Pass |
| Sustained Pressure (5 sec)       | 10M+  | 0          | ✅ Pass |

### Stability Tests

- ✅ **Million-level Test**: Generate 1M IDs at once, no duplicates
- ✅ **Sustained Pressure Test**: Continuous generation for 5 seconds, no duplicates
- ✅ **Multi-datacenter Test**: 3 datacenters × 3 workers, no duplicates
- ✅ **Clock Rollback Test**: Method 1 auto-handles, Method 2/3 correctly throw exceptions

### Performance Notes

> **Note**: Actual performance depends on multiple factors:
>
> - CPU model and frequency
> - Memory speed
> - OS scheduling
> - .NET runtime version
> - Debug/Release mode
> - System load
>
> The above data are reference values from typical test environments.
> Real deployment environments may have ±30% variations.
> We recommend running benchmarks on actual hardware for accurate data.

## FAQ

### 1. How to Choose WorkerId?

**Recommended Approaches**:

- Monolithic app: Fixed value (e.g., 1)
- Microservices: Hash service name and modulo
- K8s: Use Pod ordinal (StatefulSet)
- Config center: Fetch from configuration service

```csharp
// Example: Get from environment variable
var workerId = Environment.GetEnvironmentVariable("WORKER_ID");
var options = new IDGeneratorOptions
{
    WorkerId = ushort.Parse(workerId ?? "1"),
    DataCenterId = 1,
    Method = 1
};
```

### 2. How to Handle Clock Rollback?

| Algorithm | Handling                                                       |
| --------- | -------------------------------------------------------------- |
| Method 1  | Uses reserved sequence 1-4, supports up to 4 rollbacks         |
| Method 2  | Throws exception directly                                      |
| Method 3  | Short time (< 1s) sleeps and waits, long time throws exception |

### 3. How to Ensure Distributed Uniqueness?

Ensure each machine has a unique `WorkerId` and `DataCenterId` combination:

```csharp
// Wrong Example ❌
// Server 1: WorkerId=1, DataCenterId=1
// Server 2: WorkerId=1, DataCenterId=1  ← Will produce duplicate IDs

// Correct Example ✅
// Server 1: WorkerId=1, DataCenterId=1
// Server 2: WorkerId=2, DataCenterId=1
// Server 3: WorkerId=1, DataCenterId=2
```

### 4. What if Performance is Below Expectations?

**Checklist**:

- ✅ Use singleton pattern (`AddSingleton`)
- ✅ Avoid frequent creation of `DefaultIDGenerator` instances
- ✅ Choose Method 1 for single-machine high concurrency
- ✅ Choose Method 3 for distributed scenarios
- ✅ Check for duplicate WorkerIds
- ✅ Use Release mode build
- ✅ Ensure stable system clock

## License

MIT License @ 2025 JORDIUM.COM

## Contact Us

- **Repo**: https://github.com/nelson820125/Jordium.Snowflake.NET
- **Documentation**: https://github.com/nelson820125/Jordium.Snowflake.NET/blob/master/README-EN.md
- **Issues**: https://github.com/nelson820125/Jordium.Snowflake.NET/issues
