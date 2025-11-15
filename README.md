# Jordium.Snowflake.NET

[English](./README-EN.md) | 简体中文

[![NuGet](https://img.shields.io/nuget/v/Jordium.Snowflake.NET.svg)](https://www.nuget.org/packages/Jordium.Snowflake.NET/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

高性能分布式 ID 生成器，基于 Twitter Snowflake 算法的 .NET 实现。支持三种实现方式，适用于不同的应用场景。

## 特性

- ✅ **全局唯一**：支持多数据中心、多机器部署
- 📈 **趋势递增**：ID 按时间戳递增，支持数据库索引优化
- ⚡ **高性能**：单机器可达 200 万 - 2000 万 ID/秒
- 🔧 **多种实现**：漂移算法、传统算法、无锁算法
- ⏰ **时间回拨处理**：自动处理系统时间回拨问题
- 🎯 **灵活配置**：支持自定义位数分配

## 安装

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

## 快速开始

### 基本用法

```csharp
using Jordium.Framework.Snowflake;

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
Console.WriteLine($"Generated ID: {id}");
```

### 高级配置

```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 1,
    
    // 自定义位数分配
    WorkerIdBitLength = 5,      // Worker ID 位数 (默认 5)
    DataCenterIdBitLength = 5,  // 数据中心 ID 位数 (默认 5)
    SeqBitLength = 12,          // 序列号位数 (默认 12)
    
    // 基准时间（用于计算时间戳）
    BaseTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    
    // 序列号范围
    MinSeqNumber = 0,
    MaxSeqNumber = 4095,  // 2^12 - 1
    
    // 漂移算法专用：最大漂移次数
    TopOverCostCount = 2000
};

var generator = new DefaultIDGenerator(options);
```

### ASP.NET Core 依赖注入

```csharp
// Program.cs 或 Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // 注册为单例
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

// Controller 中使用
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
        // 使用生成的 ID
        return Ok(new { OrderId = orderId });
    }
}
```

## 算法对比

### 三种实现方式

| 算法版本 | 实现类 | 适用场景 | 性能 | 时间回拨处理 | 推荐度 |
|---------|--------|---------|------|-------------|--------|
| **Method 1** | SnowflakeWorkerV1 | 单体应用、高并发单机 | ⭐⭐⭐⭐ | ✅ 特殊序列号 | ⭐⭐⭐⭐⭐ |
| **Method 2** | SnowflakeWorkerV2 | 通用场景、标准实现 | ⭐⭐⭐⭐ | ❌ 抛出异常 | ⭐⭐⭐ |
| **Method 3** | SnowflakeWorkerV3 | 分布式集群、微服务 | ⭐⭐⭐⭐⭐ | ⚠️ 大幅回拨抛异常 | ⭐⭐⭐⭐ |

### 详细说明

#### Method 1 - 漂移算法（推荐）

**特点**：
- ✅ 自动处理时间回拨（使用预留序列号 1-4）
- ✅ 序列号溢出时自动"漂移"到未来时间
- ✅ 有完善的事件回调机制
- 🔒 使用 `lock` 同步

**适用场景**：
- 单体应用（多线程竞争同一个 WorkerId）
- 对时间回拨敏感的业务
- 需要监控 ID 生成状态的系统

**配置示例**：
```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 1,
    TopOverCostCount = 2000  // 最大漂移次数
};

var generator = new DefaultIDGenerator(options);

// 可选：监听事件
generator.GenIdActionAsync = arg =>
{
    if (arg.ActionType == 1) // 开始漂移
    {
        Console.WriteLine($"开始漂移: WorkerId={arg.WorkerId}");
    }
};
```

---

#### Method 2 - 传统算法

**特点**：
- ✅ 标准 Snowflake 实现
- ✅ 逻辑简单，易于理解
- ❌ 时间回拨时抛出异常
- 🔒 使用 `lock` 同步

**适用场景**：
- 学习和理解 Snowflake 算法
- 时间回拨极少的稳定环境
- 不需要复杂功能的简单场景

**配置示例**：
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
    // 时间回拨时会抛出异常
    Console.WriteLine($"时间回拨错误: {ex.Message}");
}
```

---

#### Method 3 - 无锁算法（高性能）

**特点**：
- ✅ 使用 CAS 无锁并发，性能最高
- ✅ 时间戳缓存在状态中，减少系统调用
- ✅ 在分布式场景下性能优势明显
- ⚠️ 短时间回拨自动休眠等待
- ⚠️ 大幅回拨（> 1000ms）抛出异常

**适用场景**：
- 微服务集群（每个服务独立 WorkerId）
- Kubernetes 部署（每个 Pod 独立 WorkerId）
- 高并发分布式系统
- 追求极致性能的场景

**配置示例**：
```csharp
var options = new IDGeneratorOptions
{
    WorkerId = 1,
    DataCenterId = 1,
    Method = 3
};

var generator = new DefaultIDGenerator(options);

// 无锁算法在多 WorkerId 场景下性能最佳
// 示例：微服务集群
// Service 1: WorkerId = 1
// Service 2: WorkerId = 2
// Service 3: WorkerId = 3
// ...
```

### 性能对比

#### 单 WorkerId 竞争场景（8 线程）

| 算法 | 吞吐量 | 说明 |
|------|--------|------|
| Method 1 | 600-800 万 ID/秒 | Lock 在单机竞争下表现稳定 |
| Method 2 | 500-700 万 ID/秒 | 标准实现，性能中等 |
| Method 3 | 400-600 万 ID/秒 | CAS 冲突导致性能下降 |

**结论**：单 WorkerId 高并发场景，推荐 **Method 1**

---

#### 多 WorkerId 独立场景（8 个 WorkerId）

| 算法 | 吞吐量 | 说明 |
|------|--------|------|
| Method 1 | 800-1200 万 ID/秒 | Lock 有固定开销 |
| Method 2 | 700-1100 万 ID/秒 | 标准实现 |
| Method 3 | **1200-1800 万 ID/秒** | 无锁优势明显 ? |

**结论**：多 WorkerId 分布式场景，推荐 **Method 3**

## 性能测试报告

### 测试环境

- **CPU**: Intel Core i7-12700K (12 核 20 线程) @ 3.6GHz
- **内存**: 32GB DDR4 3200MHz
- **操作系统**: Windows 11 Pro
- **.NET 版本**: .NET 8.0
- **测试方式**: Release 模式，无调试器

### 单机性能测试（Method 1）

| 线程数 | 吞吐量 | 平均延迟 | 备注 |
|--------|--------|---------|------|
| 1      | 1200-1500 万 ID/秒 | 0.067-0.083 μs | 单线程无竞争 |
| 2      | 900-1200 万 ID/秒 | 0.167-0.222 μs | 轻度竞争 |
| 4      | 700-900 万 ID/秒 | 0.444-0.571 μs | 中度竞争 |
| 8      | 600-800 万 ID/秒 | 1.000-1.333 μs | 高度竞争 |

### 分布式性能测试（Method 3）

| WorkerId 数量 | 每个生成数 | 总吞吐量 | 性能提升 |
|--------------|-----------|---------|---------|
| 2            | 50,000    | 1000-1300 万 ID/秒 | +10-15% |
| 4            | 25,000    | 1200-1500 万 ID/秒 | +35-45% |
| 8            | 12,500    | 1400-1800 万 ID/秒 | +50-60% |

### 正确性测试

| 测试项 | 生成数量 | 重复数 | 结果 |
|--------|---------|--------|------|
| 单 WorkerId 并发 | 100 万 | 0 | ✅ 通过 |
| 多 WorkerId 并发 | 100 万 | 0 | ✅ 通过 |
| 极限并发（32 线程）| 320 万 | 0 | ✅ 通过 |
| 持续压力（5 秒）| 1000 万+ | 0 | ✅ 通过 |

### 稳定性测试

- ✅ **百万级测试**：单次生成 100 万 ID，无重复
- ✅ **持续压力测试**：5 秒持续生成，无重复
- ✅ **多数据中心测试**：3 个数据中心 × 3 个 Worker，无重复
- ✅ **时间回拨测试**：Method 1 自动处理，Method 2/3 正确抛异常

### 性能说明

> **注意**：实际性能受多种因素影响：
> - CPU 型号和主频
> - 内存速度
> - 操作系统调度
> - .NET 运行时版本
> - 是否在 Debug/Release 模式
> - 系统负载情况
>
> 上述数据为典型测试环境下的参考值，实际部署环境可能有 ±30% 的波动。
> 推荐在实际硬件环境下进行基准测试以获得准确数据。

## ID 结构

### 标准 64 位雪花 ID

```
┌─────────────────┬─────────────┬─────────────┬──────────────┐
│ 时间戳 (41 位)   │ 数据中心(5位)│ 机器ID (5位) │ 序列号(12位)  │
└─────────────────┴─────────────┴─────────────┴──────────────┘
  约 69 年          32 个        32 台        4096/毫秒
```

### 位数分配示例

| 配置 | Worker 位 | DC 位 | Seq 位 | 容量 |
|------|----------|-------|--------|------|
| 标准 | 5 | 5 | 12 | 32 DC × 32 Worker × 4096/ms |
| 多数据中心 | 4 | 6 | 12 | 64 DC × 16 Worker × 4096/ms |
| 多机器 | 6 | 4 | 12 | 16 DC × 64 Worker × 4096/ms |
| 低并发 | 5 | 5 | 10 | 32 DC × 32 Worker × 1024/ms |

**约束条件**：
- Worker 位 + DC 位 + Seq 位 ≤ 22
- Seq 位 ≤ 12

## 常见问题

### 1. 如何选择 WorkerId？

**推荐方案**：
- 单体应用：固定值（如 1）
- 微服务：根据服务名哈希取模
- K8s：使用 Pod 序号（StatefulSet）
- 配置中心：从配置服务获取

```csharp
// 示例：从环境变量获取
var workerId = Environment.GetEnvironmentVariable("WORKER_ID");
var options = new IDGeneratorOptions
{
    WorkerId = ushort.Parse(workerId ?? "1"),
    DataCenterId = 1,
    Method = 1
};
```

### 2. 时间回拨如何处理？

| 算法 | 处理方式 |
|------|---------|
| Method 1 | 使用预留序列号 1-4，最多支持 4 次回拨 |
| Method 2 | 直接抛出异常 |
| Method 3 | 短时间（< 1秒）休眠等待，长时间抛异常 |

### 3. 如何保证分布式唯一性？

确保每个机器的 `WorkerId` 和 `DataCenterId` 组合唯一即可：

```csharp
// 错误示例 ❌
// Server 1: WorkerId=1, DataCenterId=1
// Server 2: WorkerId=1, DataCenterId=1  ← 会产生重复 ID

// 正确示例 ✅
// Server 1: WorkerId=1, DataCenterId=1
// Server 2: WorkerId=2, DataCenterId=1
// Server 3: WorkerId=1, DataCenterId=2
```

### 4. 性能不达预期怎么办？

**检查清单**：
- ✅ 使用单例模式（`AddSingleton`）
- ✅ 避免频繁创建 `DefaultIDGenerator` 实例
- ✅ 单机高并发选择 Method 1
- ✅ 分布式场景选择 Method 3
- ✅ 检查 WorkerId 是否重复
- ✅ 使用 Release 模式编译
- ✅ 确保系统时钟稳定

## 许可证

MIT License

## 联系我们

- **仓库**: https://gitee.com/nelson820125/Jordium.Snowflake.NET
- **文档**: https://gitee.com/nelson820125/Jordium.Snowflake.NET/blob/master/README.md
- **问题反馈**: https://gitee.com/nelson820125/Jordium.Snowflake.NET/issues
