# S02 配置生成核心：交付报告

日期：2026-09-18。软件版本：`0.1.0-s02`。状态：实现完成、阶段验证通过、等待用户审核；S03 未开始。

## 实现与交付

- 核心输入模型、字段校验、模板契约、YAML 定点修改、静态校验、变更范围检查及序列化往返检查。
- 安全错误代码、字段定位和脱敏变更摘要；失败没有可保存的 YAML。
- 内置清单结构升级为 2，契约版本 1，模板内容及哈希不变。
- YamlDotNet 精确固定为 18.1.0，各项目依赖锁文件及 MIT 许可说明。
- [核心接口、变更对照及限制](../s02-core.md)、[开发与测试命令](../development.md)。
- [S02 便携预览程序](../../artifacts/S02/win-x64/Skyline.exe)及[便携包](../../artifacts/S02/Skyline-0.1.0-s02-win-x64-portable.zip)。窗口仍仅展示信息，生成引擎尚未接入表单。

## 审核样例

全部使用虚构域名和演示凭据，仅用于审阅，不能证明实际连接可用。非连续组合为槽位 1、3、6。

| 类型 | 单订阅 | 非连续订阅 | 六订阅 |
| --- | --- | --- | --- |
| Windows | [YAML](../../artifacts/S02/samples/desktop-single.yaml) | [YAML](../../artifacts/S02/samples/desktop-non-contiguous.yaml) | [YAML](../../artifacts/S02/samples/desktop-six.yaml) |
| NAS | [YAML](../../artifacts/S02/samples/nas-single.yaml) | [YAML](../../artifacts/S02/samples/nas-non-contiguous.yaml) | [YAML](../../artifacts/S02/samples/nas-six.yaml) |

每份 YAML 有同名 `.changes.json` 摘要，例如 [NAS 非连续订阅变更摘要](../../artifacts/S02/samples/nas-non-contiguous.changes.json)。摘要只包含字段和槽位状态；完整 YAML 含演示凭据。

## 最终验证

| 检查 | 结果 |
| --- | --- |
| 锁定模式依赖恢复 | 通过 |
| Release 构建 | 通过，0 警告、0 错误 |
| 资源及清单验收 | 34/34 通过，包含原 S01 测试及新增清单字段 |
| 窗口状态回归 | 9/9 通过，S02 标识、错误状态及默认英文 |
| 核心验收 | 247/247 通过 |
| Windows/NAS 订阅组合 | 共 126 种全部通过，每种独立比较允许范围外的树结构和类型 |
| 特殊字符与输入验证 | 字符串原样恢复、数字端口、非法输入字段定位、NAS/Windows 差异通过 |
| 模板异常 | 重复键、文档/根类型、锚点、别名、合并键、自定义标签、深度、缺节点、重名、引用、循环、路径冲突和类型异常通过 |
| 保留与隐私 | 未知字段及标准类型标签保留、越界变更拦截、摘要/错误不含敏感标记、六种区域设置输出一致 |
| 自包含发布 | win-x64 发布通过，独立 S02 目录，S01 原包保留 |
| 源文件保护 | 两套 YAML 哈希与已批准基线一致 |

合计 **290 项自动化检查**。证据：[恢复日志](../../artifacts/S02/restore.log)、[构建日志](../../artifacts/S02/build.log)、[测试日志](../../artifacts/S02/tests.log)、[样例生成日志](../../artifacts/S02/tests-and-samples.log)、[发布日志](../../artifacts/S02/publish.log)。

```text
desktop  7AEFB272E8F095B5EE69E374982BE61A62F774DD367884C2265F59E106588660
nas      5379A7ADE11AB9E1B827D851772436ABD3F7A3B840A22092F537667BC04D21A3
```

## 过程修正与实际限制

首次依赖下载受到沙箱网络限制，获准访问 NuGet 后恢复完成。最初的 NAS 验收发现其订阅路径与 Windows 不同，已调整契约分别保留。首次锁定模式发布发现 RID 不在锁文件中，已显式声明 win-x64 并重新生成锁文件；最终锁定恢复、构建和发布均通过。

没有运行 Mihomo 内核、下载真实订阅、连接住宅代理或验证 NAS 部署；没有进行干净 Windows 环境验收，也未将本阶段视图模型测试当作新一轮 GUI 人工验收。模板原有网络策略、订阅资源下载和部署前提仍需后续实测。

源码尚未提交，生成注释因此带当前 HEAD 和 `-uncommitted`，不把未提交内容冒充正式提交版本。没有提交、推送或公开发布。

## 阶段边界

未开发配置表单、产品保存流程、随机凭据、会话管理或五种新增语言资源。六语文案约定保持。审核重点为样例结构、变更范围及错误行为；用户确认后才进入 S03。
