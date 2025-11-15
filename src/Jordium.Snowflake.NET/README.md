# Jordium.Snowflake.NET

English | [简体中文](./README-zh-CN.md)

[![NuGet](https://img.shields.io/nuget/v/Jordium.Snowflake.NET.svg)](https://www.nuget.org/packages/Jordium.Snowflake.NET/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

High-performance distributed ID generator based on Twitter's Snowflake algorithm for .NET. Supports three implementation methods for different scenarios.

## Features

- ✅ **Globally Unique**: Supports multi-datacenter, multi-machine deployment
- 📈 **Trend Increasing**: IDs increase by timestamp, optimized for database indexing
- ⚡ **High Performance**: 2 million - 20 million IDs/second per machine
- 🔧 **Multiple Implementations**: Drift algorithm, Traditional algorithm, Lock-free algorithm
- ⏰ **Clock Rollback Handling**: Automatic handling of system time rollback
- 🎯 **Flexible Configuration**: Customizable bit allocation

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
<PackageReference Include="Jordium.Snowflake.NET" Version="1.0.0" />
```

## Quick Start

### Basic Usage

```csharp
using Jordium.Framework.Snowflake;

// Create ID generator
var options = new IDGeneratorOptions
{
    WorkerId = 1,        // Worker ID (0-31)
    DataCenterId = 1,    // Datacenter ID (0-31)
    Method = 1           // Algorithm version (1=Drift, 2=Traditional, 3=Lock-free)
};

var generator = new DefaultIDGenerator(options);

// Generate ID
long id = generator.NewLong();
Console.WriteLine($"Generated ID: {id}");
```

### Advanced Configuration

```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 1,
    
    // Custom bit allocation
    WorkerIdBitLength = 5,      // Worker ID bits (default 5)
    DataCenterIdBitLength = 5,  // Datacenter ID bits (default 5)
    SeqBitLength = 12,          // Sequence bits (default 12)
    
    // Base time (for timestamp calculation)
    BaseTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    
    // Sequence range
    MinSeqNumber = 0,
    MaxSeqNumber = 4095,  // 2^12 - 1
    
    // Drift algorithm specific: max drift count
    TopOverCostCount = 2000
};

var generator = new DefaultIDGenerator(options);
```

### ASP.NET Core Dependency Injection

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register as singleton
    services.AddSingleton<IIDGenerator>(sp =>
    {
        var options = new IDGeneratorOptions
        {
            WorkerId = 1,
            DataCenterId = 1,
            Method = 1
        };
        return new DefaultIDGenerator(options);
    });
}

// Use in Controller
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
        // Use generated ID
        return Ok(new { OrderId = orderId });
    }
}
```

## Algorithm Comparison

### Three Implementation Methods

| Algorithm | Implementation | Use Case | Performance | Clock Rollback | Recommended |
|-----------|---------------|----------|-------------|----------------|-------------|
| **Method 1** | SnowflakeWorkerV1 | Monolith app, high concurrency | ⭐⭐⭐⭐ | ✅ Special sequence | ⭐⭐⭐⭐⭐ |
| **Method 2** | SnowflakeWorkerV2 | General, standard implementation | ⭐⭐⭐⭐ | ❌ Throws exception | ⭐⭐⭐ |
| **Method 3** | SnowflakeWorkerV3 | Distributed cluster, microservices | ⭐⭐⭐⭐⭐ | ⚠️ Large rollback throws | ⭐⭐⭐⭐ |

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

| Algorithm | Throughput | Note |
|-----------|-----------|------|
| Method 1 | 6-8M IDs/sec | Lock performs stable under single-machine contention |
| Method 2 | 5-7M IDs/sec | Standard implementation, medium performance |
| Method 3 | 4-6M IDs/sec | CAS conflict causes performance degradation |

**Conclusion**: For single WorkerId high concurrency, recommend **Method 1**

---

#### Multi WorkerId Independent Scenario (8 WorkerIds)

| Algorithm | Throughput | Note |
|-----------|-----------|------|
| Method 1 | 8-12M IDs/sec | Lock has fixed overhead |
| Method 2 | 7-11M IDs/sec | Standard implementation |
| Method 3 | **12-18M IDs/sec** | Lock-free advantage is significant ? |

**Conclusion**: For multi-WorkerId distributed scenarios, recommend **Method 3**

## Performance Test Report

### Test Environment

- **CPU**: Intel Core i7-12700K (12 cores, 20 threads) @ 3.6GHz
- **RAM**: 32GB DDR4 3200MHz
- **OS**: Windows 11 Pro
- **.NET Version**: .NET 8.0
- **Test Mode**: Release build, no debugger

### Single Machine Performance (Method 1)

| Threads | Throughput | Avg Latency | Note |
|---------|-----------|-------------|------|
| 1       | 12-15M IDs/sec | 0.067-0.083 ��s | Single thread, no contention |
| 2       | 9-12M IDs/sec | 0.167-0.222 ��s | Light contention |
| 4       | 7-9M IDs/sec | 0.444-0.571 ��s | Medium contention |
| 8       | 6-8M IDs/sec | 1.000-1.333 ��s | Heavy contention |

### Distributed Performance (Method 3)

| WorkerIds | Count Each | Total Throughput | Performance Gain |
|-----------|-----------|-----------------|------------------|
| 2         | 50,000    | 10-13M IDs/sec | +10-15% |
| 4         | 25,000    | 12-15M IDs/sec | +35-45% |
| 8         | 12,500    | 14-18M IDs/sec | +50-60% |

### Correctness Tests

| Test Item | Count | Duplicates | Result |
|-----------|-------|-----------|--------|
| Single WorkerId Concurrent | 1M | 0 | ✅ Pass |
| Multi WorkerId Concurrent | 1M | 0 | ✅ Pass |
| Extreme Concurrency (32 threads) | 3.2M | 0 | ✅ Pass |
| Sustained Pressure (5 sec) | 10M+ | 0 | ✅ Pass |

### Stability Tests

- ✅ **Million-level Test**: Generate 1M IDs at once, no duplicates
- ✅ **Sustained Pressure Test**: Continuous generation for 5 seconds, no duplicates
- ✅ **Multi-datacenter Test**: 3 datacenters × 3 workers, no duplicates
- ✅ **Clock Rollback Test**: Method 1 auto-handles, Method 2/3 correctly throw exceptions

### Performance Notes

> **Note**: Actual performance depends on multiple factors:
> - CPU model and frequency
> - Memory speed
> - OS scheduling
> - .NET runtime version
> - Debug/Release mode
> - System load
>
> The above data are reference values from typical test environments. 
> Real deployment environments may have ��30% variations.
> We recommend running benchmarks on actual hardware for accurate data.

## ID Structure

### Standard 64-bit Snowflake ID

```
�������������������������������������Щ��������������������������Щ��������������������������Щ�����������������������������
�� Timestamp(41bit)�� DC ID (5bit)�� Worker(5bit)�� Sequence(12) ��
�������������������������������������ة��������������������������ة��������������������������ة�����������������������������
  ~69 years        32 DCs        32 Workers   4096/ms
```

### Bit Allocation Examples

| Config | Worker Bits | DC Bits | Seq Bits | Capacity |
|--------|------------|---------|----------|----------|
| Standard | 5 | 5 | 12 | 32 DC �� 32 Worker �� 4096/ms |
| Multi-DC | 4 | 6 | 12 | 64 DC �� 16 Worker �� 4096/ms |
| Multi-Worker | 6 | 4 | 12 | 16 DC �� 64 Worker �� 4096/ms |
| Low Concurrency | 5 | 5 | 10 | 32 DC �� 32 Worker �� 1024/ms |

**Constraints**:
- Worker bits + DC bits + Seq bits �� 22
- Seq bits �� 12

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

| Algorithm | Handling |
|-----------|----------|
| Method 1 | Uses reserved sequence 1-4, supports up to 4 rollbacks |
| Method 2 | Throws exception directly |
| Method 3 | Short time (< 1s) sleeps and waits, long time throws exception |

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

MIT License

## Contact Us

- **Website**: https://jordium.com
- **Documentation**: https://docs.jordium.com
- **Issues**: https://gitee.com/jordium/jordium_framework_net/issues
