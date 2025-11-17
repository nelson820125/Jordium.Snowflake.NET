# Jordium.Snowflake.NET

<p align="center">
  <img src="https://iili.io/fJBcoPV.png" alt="Jordium.Snowflake.NET Logo" width="200"/>
</p>

[English](./README-EN.md) | 简体中文

[![NuGet](https://img.shields.io/nuget/v/Jordium.Snowflake.NET.svg)](https://www.nuget.org/packages/Jordium.Snowflake.NET/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

高性能分布式 ID 生成器，基于 Twitter Snowflake 算法的 .NET 实现。支持三种实现方式，适用于不同的应用场景。

## 特性

- ✅ **全局唯一**：支持多数据中心、多机器部署
- 📈 **趋势递增**：ID 按时间戳递增，支持数据库索引优化
- ⚡ **高性能**：单机器可达 200 万 - 2000 万 ID/秒（依据不同测试环境的条件会产生结果偏差）
- 🔧 **多种实现**：漂移算法、传统算法、无锁算法
- ⏰ **时间回拨处理**：自动处理系统时间回拨问题
- 🎯 **灵活配置**：支持自定义位数分配

## ID 结构

### 标准 64 位雪花 ID

```
┌─────────────────┬─────────────┬─────────────┬──────────────┐
│ 时间戳 (41 位)   │ 数据中心(5位)│ 机器ID (5位) │ 序列号(12位)  │
└─────────────────┴─────────────┴─────────────┴──────────────┘
  约 69 年          32 个        32 台        4096/毫秒
```

### 位数分配示例

| 配置       | Worker 位 | DC 位 | Seq 位 | 容量                        |
| ---------- | --------- | ----- | ------ | --------------------------- |
| 标准       | 5         | 5     | 12     | 32 DC × 32 Worker × 4096/ms |
| 多数据中心 | 4         | 6     | 12     | 64 DC × 16 Worker × 4096/ms |
| 多机器     | 6         | 4     | 12     | 16 DC × 64 Worker × 4096/ms |
| 低并发     | 5         | 5     | 10     | 32 DC × 32 Worker × 1024/ms |

**约束条件**：

- Worker 位 + DC 位 + Seq 位 ≤ 22
- Seq 位 ≤ 12

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
<PackageReference Include="Jordium.Snowflake.NET" Version="1.2.0" />
```

## 开始使用（v1.2.0）

### 客户端应用

#### 1. 引入命名空间

```csharp
using Jordium.Snowflake.NET;
```

#### 2. 利用工厂模型创建实例

```csharp
// 方式1： 使用WorkerId和DataCenterId创建实例
var generator1 = JordiumSnowflakeIDGeneratorFactory.Create(workerId: 1, dataCenterId: 1);
System.Console.WriteLine($"Generator 1 (WorkerId=1, DataCenterId=1): {generator1.NewLong()}");
```

```csharp
// 方式2： 使用IDGeneratorOption对象创建实例
var options = new IDGeneratorOptions(workerId: 2, dataCenterId: 1)
{
    Method = 1
};
var generator2 = JordiumSnowflakeIDGeneratorFactory.Create(options);
System.Console.WriteLine($"Generator 2 (WorkerId=2, DataCenterId=1): {generator2.NewLong()}");
```

```csharp
// 方式3： 使用配置委托创建实例
var generator3 = JordiumSnowflakeIDGeneratorFactory.Create(opt =>
{
    opt.WorkerId = 3;
    opt.DataCenterId = 1;
    opt.Method = 1;
    opt.BaseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
});
System.Console.WriteLine($"Generator 3 (WorkerId=3, DataCenterId=1): {generator3.NewLong()}");
```

### ASP.NET Core 依赖注入（v1.2.0+ 更方便的注册方式，更遵循 ASP.NET Core 标准）

#### 1. 引入命名空间

```csharp
using Jordium.Snowflake.NET.Extensions;
```

#### 2. 注册 Jordium.Snowflake.NET 服务

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

#### 3. 支持 3 种注册方式

```csharp
// 方式1: 从appsettings.json中读取默认的"JordiumSnowflakeConfig"配置
services.AddJordiumSnowflakeIdGenerator();

// appsettings.json中配置：
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
// 方案2: 使用自定义配置"MyCustomSnowflakeConfigSection"
services.AddJordiumSnowflakeIdGenerator(_configuration, "MyCustomSnowflakeConfigSection");

// appsettings.json中配置：
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
// 方案3: 基于代码的配置
services.AddJordiumSnowflakeIdGenerator(options => {
    options.WorkerId = 1;
    options.DataCenterId = 1;
    ...其他属性
});
```

## IDGeneratorOptions 属性说明

| 属性名                    | 类型     | 说明                                                                                 | 默认值                                                |
| ------------------------- | -------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------- |
| **Method**                | short    | 计算方法（1-漂移算法，2-传统算法，3-无锁算法）                                       | 1                                                     |
| **BaseTime**              | DateTime | 开始时间（UTC 格式），不能超过当前系统时间                                           | DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc) |
| **WorkerId**              | ushort   | 机器码                                                                               | 0                                                     |
| **DataCenterId**          | ushort   | 数据中心标识码                                                                       | 0                                                     |
| **WorkerIdBitLength**     | byte     | 机器码位长。建议范围：1-5（要求：序列数位长+数据标识+机器码位长不超过 22）。         | 5                                                     |
| **DataCenterIdBitLength** | byte     | 数据中心识别码位长。建议范围：1-5（要求：序列数位长+数据标识+机器码位长不超过 22）。 | 5                                                     |
| **SeqBitLength**          | byte     | 序列数位长。建议范围：1-12（要求：序列数位长+机器码位长不超过 22）4096。             | 12                                                    |
| **MaxSeqNumber**          | int      | 最大序列数（含）。（由 SeqBitLength 计算的最大值）                                   | 0                                                     |
| **MinSeqNumber**          | int      | 最小序列数（含）。默认 0，不小于 0，不大于 MaxSeqNumber                              | 0                                                     |
| **TopOverCostCount**      | int      | 最大漂移次数（含）。默认 2000，推荐范围 500-10000（与计算能力有关）                  | 2000                                                  |

## 支持目标框架

- .NET Framework (>= 4.6.1)
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10
- .NET Standard (>= 2.0)

## 内置算法对比（可查看开源库中提供的源码）

> 本章节提及的代码块为进行单元测试，基于底层代码直接实例化和演示，非依赖注入和工厂模式实例

### 三种算法

| 算法版本     | 实现类            | 适用场景             | 性能       | 时间回拨处理      | 推荐度     |
| ------------ | ----------------- | -------------------- | ---------- | ----------------- | ---------- |
| **Method 1** | SnowflakeWorkerV1 | 单体应用、高并发单机 | ⭐⭐⭐⭐   | ✅ 特殊序列号     | ⭐⭐⭐⭐⭐ |
| **Method 2** | SnowflakeWorkerV2 | 通用场景、标准实现   | ⭐⭐⭐⭐   | ❌ 抛出异常       | ⭐⭐⭐     |
| **Method 3** | SnowflakeWorkerV3 | 分布式集群、微服务   | ⭐⭐⭐⭐⭐ | ⚠️ 大幅回拨抛异常 | ⭐⭐⭐⭐   |

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

| 算法     | 吞吐量           | 说明                      |
| -------- | ---------------- | ------------------------- |
| Method 1 | 600-800 万 ID/秒 | Lock 在单机竞争下表现稳定 |
| Method 2 | 500-700 万 ID/秒 | 标准实现，性能中等        |
| Method 3 | 400-600 万 ID/秒 | CAS 冲突导致性能下降      |

**结论**：单 WorkerId 高并发场景，推荐 **Method 1**

---

#### 多 WorkerId 独立场景（8 个 WorkerId）

| 算法     | 吞吐量                 | 说明            |
| -------- | ---------------------- | --------------- |
| Method 1 | 800-1200 万 ID/秒      | Lock 有固定开销 |
| Method 2 | 700-1100 万 ID/秒      | 标准实现        |
| Method 3 | **1200-1800 万 ID/秒** | 无锁优势明显 ?  |

**结论**：多 WorkerId 分布式场景，推荐 **Method 3**

## 性能测试报告

### 测试环境

- **CPU**: Intel Core i7-12700K (12 核 20 线程) @ 3.6GHz
- **内存**: 32GB DDR4 3200MHz
- **操作系统**: Windows 11 Pro
- **.NET 版本**: .NET 8.0
- **测试方式**: Release 模式，无调试器

### 单机性能测试（Method 1）

| 线程数 | 吞吐量             | 平均延迟       | 备注         |
| ------ | ------------------ | -------------- | ------------ |
| 1      | 1200-1500 万 ID/秒 | 0.067-0.083 μs | 单线程无竞争 |
| 2      | 900-1200 万 ID/秒  | 0.167-0.222 μs | 轻度竞争     |
| 4      | 700-900 万 ID/秒   | 0.444-0.571 μs | 中度竞争     |
| 8      | 600-800 万 ID/秒   | 1.000-1.333 μs | 高度竞争     |

### 分布式性能测试（Method 3）

| WorkerId 数量 | 每个生成数 | 总吞吐量           | 性能提升 |
| ------------- | ---------- | ------------------ | -------- |
| 2             | 50,000     | 1000-1300 万 ID/秒 | +10-15%  |
| 4             | 25,000     | 1200-1500 万 ID/秒 | +35-45%  |
| 8             | 12,500     | 1400-1800 万 ID/秒 | +50-60%  |

### 正确性测试

| 测试项              | 生成数量 | 重复数 | 结果    |
| ------------------- | -------- | ------ | ------- |
| 单 WorkerId 并发    | 100 万   | 0      | ✅ 通过 |
| 多 WorkerId 并发    | 100 万   | 0      | ✅ 通过 |
| 极限并发（32 线程） | 320 万   | 0      | ✅ 通过 |
| 持续压力（5 秒）    | 1000 万+ | 0      | ✅ 通过 |

### 稳定性测试

- ✅ **百万级测试**：单次生成 100 万 ID，无重复
- ✅ **持续压力测试**：5 秒持续生成，无重复
- ✅ **多数据中心测试**：3 个数据中心 × 3 个 Worker，无重复
- ✅ **时间回拨测试**：Method 1 自动处理，Method 2/3 正确抛异常

### 性能说明

> **注意**：实际性能受多种因素影响：
>
> - CPU 型号和主频
> - 内存速度
> - 操作系统调度
> - .NET 运行时版本
> - 是否在 Debug/Release 模式
> - 系统负载情况
>
> 上述数据为典型测试环境下的参考值，实际部署环境可能有 ±30% 的波动。
> 推荐在实际硬件环境下进行基准测试以获得准确数据。

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

| 算法     | 处理方式                               |
| -------- | -------------------------------------- |
| Method 1 | 使用预留序列号 1-4，最多支持 4 次回拨  |
| Method 2 | 直接抛出异常                           |
| Method 3 | 短时间（< 1 秒）休眠等待，长时间抛异常 |

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

MIT License @ 2025 JORDIUM.COM

## 联系我们

- **仓库**: https://gitee.com/nelson820125/Jordium.Snowflake.NET
- **文档**: https://github.com/nelson820125/Jordium.Snowflake.NET/blob/master/README-EN.md
- **问题反馈**: https://github.com/nelson820125/Jordium.Snowflake.NET/issues
