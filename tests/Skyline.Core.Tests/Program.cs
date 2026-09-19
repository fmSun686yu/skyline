using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Skyline.Core;
using Skyline.Infrastructure;
using YamlDotNet.RepresentationModel;
using static Skyline.Core.YamlTree;

var catalog = new EmbeddedTemplateCatalog();
var engine = new ConfigurationGenerator(catalog);
var checks = 0;
var failures = 0;
void Check(string name, Action action)
{
    checks++;
    try { action(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.WriteLine($"FAIL {name} ({(error is ConfigurationException ce ? ce.Code.ToString() : error.GetType().Name)})"); }
}
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
ConfigurationInput Input(ConfigurationTarget target = ConfigurationTarget.Desktop, int mask = 1) => new(target,
    Enumerable.Range(1, 6).Select(i => new SubscriptionInput(i, (mask & (1 << (i - 1))) != 0,
        $"https://subscription.example.net/{i}?token=DEMO_ONLY_{i}%2F%2b&x=1&x=2")).ToArray(),
    "residential.example.net", "1080", "demo-user", "demo-password", "192.168.50.1", "demo-controller-secret",
    "demo-inbound", "demo-inbound-password");
YamlMappingNode Generate(ConfigurationInput input)
{
    var result = engine.Generate(input);
    if (!result.Success) throw new ConfigurationException(result.Issues.First(i => i.Severity == IssueSeverity.Error).Code);
    return Parse(result.Yaml!);
}
void Reject(ConfigurationInput input, IssueCode code, string? field = null)
{
    var result = engine.Generate(input);
    Assert(!result.Success && result.Yaml is null && result.Changes.Count == 0);
    Assert(result.Issues.Any(i => i.Code == code && (field is null || i.Field == field)));
}

// Independent recursive comparison, without calling the generator's projection validator.
void Preserved(YamlNode before, YamlNode after, string path, ConfigurationInput input)
{
    var editable = new[] { "/secret", "/dns/nameserver-policy/geosite:private", "/proxies/0/server", "/proxies/0/port", "/proxies/0/username", "/proxies/0/password" };
    if (editable.Contains(path) || (input.Target == ConfigurationTarget.Nas && path == "/authentication")) return;
    if (path.StartsWith("/proxy-providers/my-subscription-", StringComparison.Ordinal) && path.EndsWith("/url", StringComparison.Ordinal)) return;
    if (before is YamlMappingNode bm && after is YamlMappingNode am)
    {
        var pairs = bm.Children.Where(p => path != "/proxy-providers" || input.Subscriptions.Any(s => s.Enabled && Str(p.Key) == $"my-subscription-{s.Slot}")).ToArray();
        Assert(pairs.Length == am.Children.Count);
        foreach (var pair in pairs)
        {
            Assert(am.Children.ContainsKey(pair.Key));
            Preserved(pair.Value, am.Children[pair.Key], path + "/" + Str(pair.Key), input);
        }
    }
    else if (before is YamlSequenceNode bs && after is YamlSequenceNode ass)
    {
        var items = bs.Children.Where(n => !path.StartsWith("/proxy-groups/", StringComparison.Ordinal) || !path.EndsWith("/use", StringComparison.Ordinal)
            || input.Subscriptions.Any(s => s.Enabled && Str(n) == $"my-subscription-{s.Slot}")).ToArray();
        Assert(items.Length == ass.Children.Count);
        for (var i = 0; i < items.Length; i++) Preserved(items[i], ass.Children[i], path + "/" + i, input);
    }
    else
    {
        Assert(before is YamlScalarNode && after is YamlScalarNode);
        Assert(Str(before) == Str(after) && Kind((YamlScalarNode)before) == Kind((YamlScalarNode)after));
    }
}

foreach (var target in Enum.GetValues<ConfigurationTarget>())
for (var mask = 1; mask < 64; mask++)
{
    var sampleMask = mask;
    Check($"{target} subscription combination {mask}", () =>
    {
        var input = Input(target, sampleMask);
        var root = Generate(input);
        Preserved(Parse(Encoding.UTF8.GetString(catalog.ReadTemplate(target == ConfigurationTarget.Desktop ? "desktop" : "nas"))), root, "", input);
        var expected = input.Subscriptions.Where(s => s.Enabled).Select(s => $"my-subscription-{s.Slot}").ToArray();
        Assert(Map(Get(root, "proxy-providers")).Children.Keys.Select(Str).SequenceEqual(expected));
        foreach (var group in Maps(root, "proxy-groups").Where(g => Has(g, "use")))
            Assert(Seq(Get(group, "use")).Children.Select(Str).SequenceEqual(expected));
        foreach (var sub in input.Subscriptions.Where(s => s.Enabled))
            Assert(Str(Map(Get(Map(Get(root, "proxy-providers")), $"my-subscription-{sub.Slot}")), "url") == sub.Url);
        Assert(Has(root, "authentication") == (target == ConfigurationTarget.Nas));
    });
}

var invalidInputs = new (string Name, ConfigurationInput Value, IssueCode Code, string? Field)[]
{
    ("no subscription", Input(mask: 0), IssueCode.SubscriptionRequired, "subscriptions"),
    ("invalid target", Input() with { Target = (ConfigurationTarget)999 }, IssueCode.InvalidTarget, "target"),
    ("slot zero", Input() with { Subscriptions = [new(0, true, "https://a.example")] }, IssueCode.InvalidSlot, "subscriptions"),
    ("slot seven", Input() with { Subscriptions = [new(7, true, "https://a.example")] }, IssueCode.InvalidSlot, "subscriptions"),
    ("duplicate slot", Input() with { Subscriptions = [new(1, true, "https://a.example"), new(1, false, null)] }, IssueCode.DuplicateSlot, "subscriptions"),
    ("duplicate URL", Input() with { Subscriptions = [new(1, true, "https://a.example"), new(3, true, "https://a.example")] }, IssueCode.DuplicateUrl, "subscriptions.url"),
    ("empty enabled URL", Input() with { Subscriptions = [new(1, true, "")] }, IssueCode.Required, "subscriptions.url"),
    ("non HTTP", Input() with { Subscriptions = [new(1, true, "ftp://a.example")] }, IssueCode.InvalidUrl, "subscriptions.url"),
    ("URL whitespace", Input() with { Subscriptions = [new(1, true, "https://a.example/ a")] }, IssueCode.InvalidUrl, "subscriptions.url"),
    ("URL control", Input() with { Subscriptions = [new(1, true, "https://a.example/\n")] }, IssueCode.InvalidUrl, "subscriptions.url"),
    ("NAS colon user", Input(ConfigurationTarget.Nas) with { NasUsername = "x:y" }, IssueCode.ColonNotAllowed, "nasUsername"),
    ("NAS colon password", Input(ConfigurationTarget.Nas) with { NasPassword = "x:y" }, IssueCode.ColonNotAllowed, "nasPassword"),
    ("NAS absent", Input(ConfigurationTarget.Nas) with { NasUsername = null }, IssueCode.Required, "nasUsername"),
    ("username whitespace", Input() with { Username = "  " }, IssueCode.Required, "username"),
    ("password empty", Input() with { Password = "" }, IssueCode.Required, "password"),
    ("password control", Input() with { Password = "x\ty" }, IssueCode.ControlCharacter, "password"),
    ("secret empty", Input() with { ControllerSecret = " " }, IssueCode.Required, "controllerSecret"),
    ("secret newline", Input() with { ControllerSecret = "x\ny" }, IssueCode.ControlCharacter, "controllerSecret")
};
foreach (var item in invalidInputs) Check(item.Name, () => Reject(item.Value, item.Code, item.Field));
foreach (var value in new[] { "", "0", "65536", "-1", "+80", "1,080", "10.5", "１２３", " 80", "999999999999999" })
    Check($"invalid port case {Array.IndexOf(new[] { "", "0", "65536", "-1", "+80", "1,080", "10.5", "１２３", " 80", "999999999999999" }, value)}", () => Reject(Input() with { Port = value }, IssueCode.InvalidPort));
foreach (var value in new[] { "http://proxy.example", "proxy.example/path", "proxy.example:1080", "[2001:db8::1]", "127.1", "-bad.example", "bad_.example", "a\nb.example" })
    Check("invalid server syntax", () => Reject(Input() with { Server = value }, IssueCode.InvalidServer));
foreach (var value in new[] { "0.0.0.0", "::", "224.0.0.1", "ff02::1", "255.255.255.255", "1.2.3.4:53", "dns.example", "127.1", "01.2.3.4", "::ffff:224.0.0.1", "::ffff:0.0.0.0" })
    Check("invalid DNS syntax or range", () => Reject(Input() with { LocalDns = value }, IssueCode.InvalidDns, "localDns"));
Check("DNS required", () => Reject(Input() with { LocalDns = null }, IssueCode.Required, "localDns"));
foreach (var dns in new[] { "192.168.50.1", "192.168.1.1", "2001:db8::53", "fd00::1" })
    Check("valid DNS", () => Assert(engine.Generate(Input() with { LocalDns = dns }).Success));
foreach (var dns in new[] { "127.0.0.1", "::1", "::ffff:127.0.0.1" })
    Check("loopback DNS warning", () =>
    {
        var result = engine.Generate(Input() with { LocalDns = dns });
        Assert(result.Success && result.Issues.Single().Code == IssueCode.LoopbackDns && result.Issues.Single().Severity == IssueSeverity.Warning);
    });
foreach (var server in new[] { "192.0.2.1", "2001:db8::1", "  proxy.example.net  ", "例子.example" })
    Check("valid server", () => Assert(Str(Named(Generate(Input() with { Server = server }), "proxies", "USA_Static_Native_ISP"), "server") == server.Trim()));
Check("disabled values ignored", () => Assert(engine.Generate(Input() with { Subscriptions = [new(1, true, "https://a.example"), new(6, false, "\n")] }).Success));
Check("Windows ignores NAS credentials", () => Assert(engine.Generate(Input() with { NasUsername = ":\n", NasPassword = null }).Success));
foreach (var target in Enum.GetValues<ConfigurationTarget>())
foreach (var value in new[] { "true", "null", "00123", " leading and trailing ", "中文\"'#\\value", "a:b", "off", "~" })
    Check($"{target} credential round trip", () =>
    {
        var input = Input(target) with { Username = value, Password = value, ControllerSecret = value,
            NasUsername = value.Replace(':', '-'), NasPassword = value.Replace(':', '-') };
        var root = Generate(input);
        var node = Named(root, "proxies", "USA_Static_Native_ISP");
        Assert(Str(node, "username") == value && Str(node, "password") == value && Str(root, "secret") == value);
        Assert(Kind((YamlScalarNode)Get(node, "username")) == "string");
        if (target == ConfigurationTarget.Nas) Assert(Str(Seq(Get(root, "authentication")).Children.Single()) == input.NasUsername + ":" + input.NasPassword);
    });

void TemplateFailure(string name, Func<string, string> mutate, IssueCode code, ConfigurationTarget target = ConfigurationTarget.Desktop)
{
    Check(name, () =>
    {
        var result = new ConfigurationGenerator(new TestCatalog(catalog, mutate)).Generate(Input(target));
        Assert(!result.Success && result.Yaml is null && result.Issues.Any(i => i.Code == code));
    });
}
Func<string, string> Edit(Action<YamlMappingNode> edit) => source => { var root = Parse(source); edit(root); return Write(root); };
TemplateFailure("duplicate root key", s => s + "\nport: 1\n", IssueCode.InvalidYaml);
TemplateFailure("duplicate nested key", s => s.Replace("    server: socks5.example.com", "    server: other.example\n    server: socks5.example.com"), IssueCode.InvalidYaml);
TemplateFailure("multiple documents", s => s + "\n---\nx: 1\n", IssueCode.InvalidYaml);
TemplateFailure("sequence root", _ => "- x", IssueCode.InvalidYaml);
TemplateFailure("anchor", s => s + "\nx-extra: &anchor value\n", IssueCode.InvalidYaml);
TemplateFailure("merge key", s => s + "\nx-extra: {<<: {a: 1}}\n", IssueCode.InvalidYaml);
TemplateFailure("custom tag", s => s + "\nx-extra: !custom value\n", IssueCode.InvalidYaml);
TemplateFailure("undefined alias", s => s + "\nx-extra: *missing\n", IssueCode.InvalidYaml);
TemplateFailure("too deep", s => s + "\nx-extra: " + new string('[', 70) + "0" + new string(']', 70), IssueCode.InvalidYaml);
TemplateFailure("quoted bool", Edit(r => Set(r, "ipv6", Text("false"))), IssueCode.ContractMismatch);
TemplateFailure("quoted port", Edit(r => Set(Named(r, "proxies", "USA_Static_Native_ISP"), "port", Text("1080"))), IssueCode.ContractMismatch);
TemplateFailure("noninteger port", Edit(r => Set(Named(r, "proxies", "USA_Static_Native_ISP"), "port", new YamlScalarNode("1.5"))), IssueCode.ContractMismatch);
TemplateFailure("missing residential", Edit(r => Seq(Get(r, "proxies")).Children.Clear()), IssueCode.InvalidReference);
TemplateFailure("duplicate name", Edit(r => Seq(Get(r, "proxies")).Children.Add(Parse("name: USA_Static_Native_ISP\ntype: socks5"))), IssueCode.DuplicateName);
TemplateFailure("wrong residential type", Edit(r => Set(Named(r, "proxies", "USA_Static_Native_ISP"), "type", Text("http"))), IssueCode.ContractMismatch);
TemplateFailure("non scalar username", Edit(r => Set(Named(r, "proxies", "USA_Static_Native_ISP"), "username", new YamlMappingNode())), IssueCode.ContractMismatch);
TemplateFailure("missing provider", Edit(r => Remove(Map(Get(r, "proxy-providers")), "my-subscription-6")), IssueCode.InvalidReference);
TemplateFailure("dangling rule", Edit(r => Seq(Get(r, "rules")).Children.Add(Text("MATCH,missing"))), IssueCode.InvalidReference);
TemplateFailure("dangling ruleset", Edit(r => Seq(Get(r, "rules")).Children.Add(Text("RULE-SET,missing,DIRECT"))), IssueCode.InvalidReference);
TemplateFailure("dangling use in extra group", Edit(r => Seq(Get(r, "proxy-groups")).Children.Add(Parse("name: Extra\ntype: select\nuse: [missing]"))), IssueCode.InvalidReference);
TemplateFailure("provider download proxy", Edit(r => Set(Map(Get(Map(Get(r, "proxy-providers")), "my-subscription-1")), "proxy", Text("missing"))), IssueCode.InvalidReference);
TemplateFailure("direct cycle", Edit(r => Set(Named(r, "proxy-groups", "Apple"), "proxies", new YamlSequenceNode(Text("Apple")))), IssueCode.DependencyCycle);
TemplateFailure("dialer cycle", Edit(r => Set(Named(r, "proxy-groups", "chain-hop1-entry"), "proxies", new YamlSequenceNode(Text("USA_Static_Native_ISP")))), IssueCode.DependencyCycle);
TemplateFailure("cache collision normalized", Edit(r => Set(Map(Get(Map(Get(r, "rule-providers")), "private_domain")), "path", Text("./proxy_providers/tmp/../my-subscription-1.yaml"))), IssueCode.CachePathCollision);
TemplateFailure("Windows unexpected authentication", Edit(r => Set(r, "authentication", new YamlSequenceNode(Text("x:y")))), IssueCode.ContractMismatch);
TemplateFailure("NAS missing authentication", Edit(r => Remove(r, "authentication")), IssueCode.ContractMismatch, ConfigurationTarget.Nas);
Check("incompatible descriptor", () => Assert(new ConfigurationGenerator(new TestCatalog(catalog, s => s, t => t with { ContractVersion = 99 })).Generate(Input()).Issues.Single().Code == IssueCode.ContractMismatch));
Check("catalog hash mismatch", () => Assert(new ConfigurationGenerator(new TestCatalog(catalog, s => s, t => t with { Sha256 = new string('0', 64) })).Generate(Input()).Issues.Single().Code == IssueCode.TemplateUnavailable));
Check("unknown fields and extra use retained", () =>
{
    var mutate = Edit(r =>
    {
        Set(r, "x-future", Parse("text: '00123'\nflag: true\nlist: [1, null, 'null']\nexplicit: !!str 001"));
        Seq(Get(r, "proxy-groups")).Children.Add(Parse("name: Extra\ntype: select\nuse: [my-subscription-1, my-subscription-3]"));
    });
    var result = new ConfigurationGenerator(new TestCatalog(catalog, mutate)).Generate(Input(mask: 4));
    Assert(result.Success);
    var root = Parse(result.Yaml!);
    Assert(Equal(Get(root, "x-future"), Parse("text: '00123'\nflag: true\nlist: [1, null, 'null']\nexplicit: !!str 001")));
    Assert(Seq(Get(Named(root, "proxy-groups", "Extra"), "use")).Children.Select(Str).SequenceEqual(new[] { "my-subscription-3" }));
});
foreach (var field in new[] { "tun", "ipv6", "port", "allow-lan", "secret-type" })
    Check($"unauthorized change rejected {field}", () =>
    {
        var input = Input();
        var after = Generate(input);
        if (field == "secret-type") Set(Named(after, "proxies", "USA_Static_Native_ISP"), "udp", Text("true"));
        else Set(after, field, Text("changed"));
        var template = catalog.Templates.Single(t => t.Id == "desktop");
        try { ConfigurationGenerator.VerifyChanges(Parse(Encoding.UTF8.GetString(catalog.ReadTemplate("desktop"))), after, template, input); }
        catch (ConfigurationException e) { Assert(e.Code == IssueCode.UnexpectedChange); return; }
        throw new Exception("Unexpected mutation accepted");
    });
Check("known placeholder rejected", () => Reject(Input() with { Password = "YOUR_SOCKS5_PASSWORD" }, IssueCode.Placeholder));
Check("subscription placeholder identifies slot", () =>
{
    var result = engine.Generate(Input() with { Subscriptions = [new(3, true, "https://a.example/?token=YOUR_SUBSCRIPTION_TOKEN_3")] });
    Assert(!result.Success && result.Issues.Any(i => i.Code == IssueCode.Placeholder && i.Slot == 3 && i.Field == "subscriptions.url"));
});
foreach (var port in new[] { "1", "65535", "001080" }) Check("valid integer port bounds", () => Assert(engine.Generate(Input() with { Port = port }).Success));
Check("HTTP and raw query variants supported", () => Assert(engine.Generate(Input() with { Subscriptions = [new(1, true, "http://a.example/?x=%2f"), new(2, true, "http://a.example/?x=%2F")] }).Success));
foreach (var target in Enum.GetValues<ConfigurationTarget>())
    Check($"{target} provider settings mutations detected", () =>
    {
        var input = Input(target);
        var root = Generate(input);
        Set(Map(Get(Map(Get(root, "proxy-providers")), "my-subscription-1")), "interval", Integer(1));
        var id = target == ConfigurationTarget.Desktop ? "desktop" : "nas";
        try { ConfigurationGenerator.VerifyChanges(Parse(Encoding.UTF8.GetString(catalog.ReadTemplate(id))), root, catalog.Templates.Single(t => t.Id == id), input); }
        catch (ConfigurationException e) { Assert(e.Code == IssueCode.UnexpectedChange); return; }
        throw new Exception("Provider mutation accepted");
    });
Check("input and summary privacy", () =>
{
    var input = Input() with { Password = "SENSITIVE_MARKER", ControllerSecret = "SENSITIVE_MARKER" };
    var result = engine.Generate(input);
    Assert(result.Success && result.Yaml!.Contains("SENSITIVE_MARKER"));
    Assert(!JsonSerializer.Serialize(result.Changes).Contains("SENSITIVE_MARKER"));
    Assert(!JsonSerializer.Serialize(result.Issues).Contains("SENSITIVE_MARKER"));
    Assert(!input.ToString().Contains("SENSITIVE_MARKER") && !result.ToString().Contains("SENSITIVE_MARKER"));
    var bad = engine.Generate(input with { Password = "SENSITIVE_MARKER\n" });
    Assert(!JsonSerializer.Serialize(bad.Issues).Contains("SENSITIVE_MARKER"));
});
Check("deterministic across six cultures", () =>
{
    var previous = CultureInfo.CurrentCulture;
    var previousUi = CultureInfo.CurrentUICulture;
    try
    {
        var expected = engine.Generate(Input(ConfigurationTarget.Nas, 37)).Yaml;
        foreach (var culture in new[] { "en-US", "zh-CN", "zh-TW", "ja-JP", "de-DE", "fr-FR" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert(engine.Generate(Input(ConfigurationTarget.Nas, 37)).Yaml == expected);
        }
        Assert(expected is not null && !expected.Contains('\r') && !expected.StartsWith('\ufeff'));
    }
    finally { CultureInfo.CurrentCulture = previous; CultureInfo.CurrentUICulture = previousUi; }
});

Console.WriteLine($"{checks - failures}/{checks} passed.");
if (failures == 0 && args.Length == 2 && args[0] == "--samples")
{
    Directory.CreateDirectory(args[1]);
    foreach (var target in Enum.GetValues<ConfigurationTarget>())
    foreach (var (label, mask) in new[] { ("single", 1), ("non-contiguous", 37), ("six", 63) })
    {
        var input = Input(target, mask);
        var result = engine.Generate(input);
        if (!result.Success) return 1;
        var name = $"{target.ToString().ToLowerInvariant()}-{label}";
        File.WriteAllText(Path.Combine(args[1], name + ".yaml"), result.Yaml, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(args[1], name + ".changes.json"), JsonSerializer.Serialize(result.Changes, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    }
    Console.WriteLine("Wrote six fictional sample configurations and redacted change summaries.");
}
return failures == 0 ? 0 : 1;

sealed class TestCatalog : ITemplateCatalog
{
    private readonly Dictionary<string, byte[]> resources;
    public string SoftwareVersion { get; }
    public IReadOnlyList<TemplateDescriptor> Templates { get; }
    internal TestCatalog(ITemplateCatalog source, Func<string, string> mutate, Func<TemplateDescriptor, TemplateDescriptor>? descriptor = null)
    {
        SoftwareVersion = source.SoftwareVersion;
        resources = source.Templates.ToDictionary(t => t.Id, t => Encoding.UTF8.GetBytes(mutate(Encoding.UTF8.GetString(source.ReadTemplate(t.Id)))));
        Templates = source.Templates.Select(t => (descriptor ?? (d => d))(t with { Sha256 = Convert.ToHexString(SHA256.HashData(resources[t.Id])) })).ToArray();
    }
    public byte[] ReadTemplate(string id) => (byte[])resources[id].Clone();
}
