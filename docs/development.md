# Skyline Configurator 开发指南

当前版本为 `0.1.0-s02`，已实现工程、内置模板基础和可独立调用的配置生成核心，界面仍为信息预览窗口。产品范围以已批准的 [v5 方案](change-plans/2026-09-18-skyline-configurator-design-v5.md) 为准；后续阶段须分别审核。

## 开发环境

- Windows 11 x64；本次验证系统版本为 10.0.26200。
- 官方 .NET SDK **10.0.401 x64**，固定于 `global.json`，不自动滚动到其他版本。
- SDK 放在仓库 `.tools/dotnet`，与系统已有运行时并存，不替换系统运行时、不修改机器 PATH。
- 自包含程序运行时固定为 **10.0.12**。最终使用者无需安装 SDK 或 .NET。
- 首次下载 SDK 和恢复运行时包需要访问 Microsoft / NuGet；程序运行不需要模板下载服务。

在仓库根目录打开 PowerShell，依次执行：

```powershell
./scripts/setup-sdk.ps1
./scripts/dev.ps1 Restore
./scripts/dev.ps1 Build
./scripts/dev.ps1 Test
./scripts/dev.ps1 Run
```

`setup-sdk.ps1` 使用固定的 Microsoft 下载地址和 SHA-512 校验值。版本来源为 [Microsoft .NET 10 发布元数据](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json)。升级 SDK 时同时更新 `global.json`、下载校验值和本说明。

脚本为当前进程设置 SDK 和包缓存路径；`.tools`、`.dotnet-home`、`.nuget`、`bin`、`obj` 与 `artifacts` 均不入库。脚本关闭遥测、全局工具 PATH 添加及首次运行开发证书生成。本次最初初始化 SDK 时，CLI 曾报告创建 ASP.NET 开发证书；WPF 工程不使用该证书。

## 工程与依赖方向

```text
Skyline.App (WPF) ──→ Skyline.Core
       │
       └──→ Skyline.Infrastructure ──→ Skyline.Core
tests/Skyline.Infrastructure.Tests ──→ Skyline.Infrastructure
tests/Skyline.App.Tests ──→ Skyline.App
tests/Skyline.Core.Tests ──→ Skyline.Core + Skyline.Infrastructure
```

Core 定义输入模型、模板契约、结构化 YAML 生成、静态校验和安全错误码；Infrastructure 读取和校验程序集资源；App 提供英文资源、最小窗口和错误展示。Core 使用精确固定的 YamlDotNet 18.1.0，许可见根目录 `THIRD-PARTY-NOTICES.txt`。

测试采用三个无需测试框架依赖的可执行验收项目。**请使用 `./scripts/dev.ps1 Test`，不要用 `dotnet test` 的退出码替代本阶段验收。** 任意断言失败都会使脚本失败。模板测试参数为仓库根目录；独立同名文件测试只在 `artifacts/tests` 下写入虚构内容。

各工程的 `packages.lock.json` 均需入库，普通恢复及发布启用锁定模式；所有项目声明 `win-x64` 恢复目标，保证普通构建和便携发布使用同一锁定文件。明确升级依赖时才使用本地 SDK 的 `restore Skyline.sln --configfile NuGet.Config --force-evaluate` 更新锁定文件，并审阅差异。

构建脚本将当前 Git HEAD 写入构建元数据；工作区有未提交内容时加 `-uncommitted`。直接使用 SDK 构建而未传 `BuildRevision` 时记录为 `uncommitted`。生成注释读取编译时元数据，运行时不调用 Git。

## 模板版本绑定

`Mihomo/manifest.json` 使用清单结构版本 2，记录软件版本、模板版本、资源名称、SHA-256、住宅节点名、六个订阅槽位、契约版本 1、入口/出口组和必填环境字段。两套 `config.yaml` 和清单通过 `EmbeddedResource` 直接嵌入基础设施程序集，不经过文本转换或复制改写。

启动时检查清单结构、软件版本、资源存在性和全部模板哈希；任一失败显示英文错误，不显示校验成功。返回的模板字节为副本。运行时无外部文件路径、仓库路径或在线更新入口。

更新模板时必须修改对应清单哈希/版本及软件版本，重新构建和验证。软件版本集中在 `Directory.Build.props`，清单的 `softwareVersion` 必须与其一致。完整性检查用于发现损坏或打包不一致，不是数字签名。

## 生成本阶段审核程序

先关闭正在运行的审核程序，再执行：

```powershell
./scripts/dev.ps1 Publish
```

结果在 `artifacts/S02/win-x64`。直接运行其中的 `Skyline.exe`；复制或压缩时保留整个目录，不能只复制 EXE。这是包含 .NET/WPF 运行时的目录式便携程序，不是安装包。S02 窗口只展示模板信息；生成核心通过测试或程序集接口调用。

S01 原审核 ZIP 保留在 `artifacts/S01`。S02 产物写入单独的 `artifacts/S02`，未加入 Git。完整正式发布、签名和干净机器验证属于后续阶段。构建或发布失败时不要把旧目录误当作新产物；检查命令退出码和错误信息。

## 生成 S02 虚构样例

```powershell
./scripts/dev.ps1 Samples
```

运行核心验收测试，通过后将六份样例 YAML 和六份脱敏变更摘要写到 `artifacts/S02/samples`。此命令仅属于测试辅助工具，不接收真实用户资料、不实现产品文件保存。

接口用法及边界见 [S02 核心说明](s02-core.md)。验证结果见 [S01 交付报告](stages/S01-report.md)和 [S02 交付报告](stages/S02-report.md)。
