# Copilot Instructions

## 项目指南
- 对于 Jordium.Snowflake.NET 系列项目的 ORM 扩展包（如 SqlSugar/EF Core 扩展），开发期内部依赖应使用 ProjectReference 引用核心包 Jordium.Snowflake.NET，而非 PackageReference，以避免因 nupkg 版本滞后导致新增类型（如 SnowflakeIdPopulator）在扩展包中编译不到；打包时 ProjectReference 会自动转换为对应的 PackageReference 依赖。