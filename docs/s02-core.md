# S02 配置生成核心

## 调用方式

```csharp
var catalog = new EmbeddedTemplateCatalog();
var generator = new ConfigurationGenerator(catalog);
var input = new ConfigurationInput(
    ConfigurationTarget.Desktop,
    new[] { new SubscriptionInput(1, true, "https://subscription.example.net/1?token=DEMO_ONLY") },
    "residential.example.net", "1080", "demo-user", "demo-password",
    "192.168.50.1", "demo-controller-secret");
var result = generator.Generate(input);
// result.Success 才有 result.Yaml；Issues 是安全错误/警告，Changes 是脱敏摘要。
// 不要将 result.Yaml 或整个 result 序列化到日志。
```

示例域名、账号与密码均为虚构资料。生成核心不保存文件、不启动 Mihomo、不访问订阅或住宅服务器、不产生随机凭据。传入 null 的输入对象是调用错误；缺失字段、null 订阅集合和不合法字段返回验证结果。订阅编号必须为 1–6，启用数至少 1；列表中不存在的槽位视为停用。

`ITemplateCatalog` 的生产实现仍只有程序集资源读取器；测试中的可变目录用于注入异常，不是软件的外部模板入口。内置资源读取错误由目录组件返回，界面已有对应错误展示；生成器再次核对软件版本、模板哈希和 YAML 契约。

## 输入和结果约定

- server 仅 trim 两端空白，域名不联网解析；不接受带方括号、zone ID 或端口的 IP。DNS 仅接受完整 IPv4 或 IPv6，IPv4 不接受简写、整数式或前导零写法。
- URL 原文必须为 HTTP/HTTPS，没有空白、控制字符或反斜杠；重复比较使用区分大小写的原字符串，不重编码 token，不把 `%2f` 与 `%2F` 合并。
- port 输入为字符串便于定位表单错误，验证后输出整数。凭据按原字符保留，不 trim、不 Unicode 归一化；NAS 账号额外拒绝冒号。
- `ValidationIssue` 只包含稳定枚举、字段名、可选槽位编号和严重程度，不含输入值。回环 DNS 为 Warning，不阻止生成。失败结果的 YAML 为 null、变更列表为空。
- 成功结果中的完整 YAML 含凭据；变更摘要只有 Updated / Enabled / Removed、字段及槽位，不含敏感值。诊断对象和摘要可以用于后续本地化；不能序列化整个结果对象当作日志。
- UTF-8 无 BOM 的落盘由后续保存层负责；核心返回的字符串无 BOM、仅 LF 换行。S02 样例工具用 `UTF8Encoding(false)` 写出。

## 允许变更对照

| 配置位置 | 唯一允许变化 |
| --- | --- |
| 指定住宅节点的 server / port / username / password | 用户输入；port 为整数，其余为明确引用的字符串 |
| 六个订阅 provider | 更新启用项 URL；移除停用项；保留启用项的周期、缓存路径、健康检查和 override |
| 全部代理组的 use | 移除停用 provider 引用，保留剩余顺序，包括新增的合法代理组 |
| dns.nameserver-policy.geosite:private | 本次 DNS 单元素字符串列表 |
| secret | 本次控制密钥字符串 |
| NAS authentication | 单元素 `用户名:密码` 字符串列表；Windows 不新增该键 |
| 文件开头注释 | 固定英文软件版本、模板版本、构建提交信息；无时间戳或输入资料 |

生成后剔除授权叶子变化并比较其余树结构，另行复核实际写入值和类型。测试还使用独立递归比较覆盖 126 种组合，并主动注入越界修改检查防护。规则/候选顺序、TUN、UDP、IPv6、端口、CORS、访问范围及未知合法字段均保留。不保证原始注释、缩进和引号风格逐字保持。

## 契约与检查边界

清单 schemaVersion=2，contractVersion=1。住宅节点、入口组、出口组、AI 住宅出口及入口三个选择组按固定契约检查。Windows 缓存目录为 `proxy_providers`，NAS 为 `proxy-providers`，不统一改写。

只接受单文档映射；映射键为标量，禁止重复键、锚点、别名、合并键和自定义标签，接受标准 YAML 类型标签。输入模板限制 1 MiB、嵌套深度 64，超过返回安全错误。未知合法映射/列表/标量字段保留。

当前规则契约覆盖内置模板使用的 RULE-SET 和 MATCH，不实现通用规则表达式解释器；新增规则语法需要升级契约及测试。静态检查覆盖节点/组候选、dialer-proxy、use、规则集和 provider 下载代理引用；不会解析尚未下载的订阅节点，也不宣称验证动态节点名或真实网络可达性。缓存路径比较统一分隔符、消除 `.`/`..` 并忽略大小写，以兼顾 Windows。

核心不依赖 WPF、Windows 路径 API 或用户目录；构建当前只配置 Windows x64 发布目标。其他系统的产品支持仍不在本阶段范围。
