using System.Text;
using System.Text.Json.Nodes;
using Skyline.Core;
using Skyline.Infrastructure;

// Dependency-free executable acceptance tests for S01. Exit nonzero on any failure.
var root = Path.GetFullPath(args.Single());
var assembly = typeof(EmbeddedTemplateCatalog).Assembly;
var originals = assembly.GetManifestResourceNames().ToDictionary(name => name, name =>
{
    using var stream = assembly.GetManifestResourceStream(name)!;
    using var output = new MemoryStream();
    stream.CopyTo(output);
    return output.ToArray();
});
var failures = 0;
var checks = 0;
void Check(string name, Action action)
{
    checks++;
    try { action(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.WriteLine($"FAIL {name}: {error}"); }
}
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
EmbeddedTemplateCatalog Load(Dictionary<string, byte[]> data, string version = "0.1.0-s02") =>
    new(name => data.TryGetValue(name, out var bytes) ? new MemoryStream(bytes) : null, version);
Dictionary<string, byte[]> Alter(Action<JsonObject> edit)
{
    var data = new Dictionary<string, byte[]>(originals);
    var manifest = JsonNode.Parse(data[EmbeddedTemplateCatalog.ManifestResource])!.AsObject();
    edit(manifest);
    data[EmbeddedTemplateCatalog.ManifestResource] = Encoding.UTF8.GetBytes(manifest.ToJsonString());
    return data;
}
void Expect(TemplateError code, Action action)
{
    try { action(); }
    catch (TemplateCatalogException error) { Assert(error.Code == code); return; }
    throw new Exception($"Expected {code}.");
}

Check("embedded template bytes and manifest match repository", () =>
{
    var catalog = new EmbeddedTemplateCatalog();
    Assert(catalog.SoftwareVersion == "0.1.0-s02" && catalog.Templates.Count == 2);
    Assert(originals[EmbeddedTemplateCatalog.ManifestResource].SequenceEqual(File.ReadAllBytes(Path.Combine(root, "Mihomo", "manifest.json"))));
    foreach (var item in catalog.Templates)
    {
        Assert(catalog.ReadTemplate(item.Id).SequenceEqual(File.ReadAllBytes(Path.Combine(root, "Mihomo", item.Id, "config.yaml"))));
        Assert(item.SubscriptionSlots.Count == 6);
        Assert(Encoding.UTF8.GetString(catalog.ReadTemplate(item.Id)).Contains($"v{item.Version}"));
        Assert(Encoding.UTF8.GetString(catalog.ReadTemplate(item.Id)).Contains($"name: {item.ResidentialProxyName}"));
    }
});
Check("returned content cannot mutate catalog", () =>
{
    var catalog = new EmbeddedTemplateCatalog();
    var bytes = catalog.ReadTemplate("desktop");
    bytes[0] ^= 0xff;
    Assert(!bytes.SequenceEqual(catalog.ReadTemplate("desktop")));
});
foreach (var resource in originals.Keys)
    Check($"missing resource: {resource}", () =>
    {
        var data = new Dictionary<string, byte[]>(originals);
        data.Remove(resource);
        Expect(TemplateError.ResourceMissing, () => Load(data));
    });
foreach (var id in new[] { "desktop", "nas" })
    Check($"corrupt template rejected: {id}", () =>
    {
        var data = new Dictionary<string, byte[]>(originals) { [$"Skyline.Templates.{id}.yaml"] = [0, 1, 2] };
        Expect(TemplateError.HashMismatch, () => Load(data));
    });
Check("wrong software binding", () => Expect(TemplateError.SoftwareVersionMismatch, () => Load(originals, "9.9.9")));
var invalidManifests = new Dictionary<string, Action<JsonObject>>
{
    ["schema"] = m => m["schemaVersion"] = 999,
    ["old schema"] = m => m["schemaVersion"] = 1,
    ["contract version"] = m => m["templates"]![0]!["contractVersion"] = 99,
    ["missing contract"] = m => m["templates"]![0]!.AsObject().Remove("contractVersion"),
    ["entry group"] = m => m["templates"]![0]!["entryGroup"] = "other",
    ["exit group"] = m => m["templates"]![0]!["exitGroup"] = "other",
    ["environment fields"] = m => m["templates"]![0]!["requiredEnvironmentFields"] = new JsonArray(),
    ["unknown property"] = m => m["sourceUrl"] = "https://example.com",
    ["missing software version"] = m => m.Remove("softwareVersion"),
    ["missing template"] = m => m["templates"]!.AsArray().RemoveAt(1),
    ["null template"] = m => m["templates"]![0] = null,
    ["duplicate ID"] = m => m["templates"]![1]!["id"] = "desktop",
    ["external resource"] = m => m["templates"]![0]!["resourceName"] = "C:/config.yaml",
    ["invalid hash"] = m => m["templates"]![0]!["sha256"] = new string('Z', 64),
    ["missing version"] = m => m["templates"]![0]!.AsObject().Remove("version"),
    ["wrong node"] = m => m["templates"]![0]!["residentialProxyName"] = "other",
    ["missing slot"] = m => m["templates"]![0]!["subscriptionSlots"]!.AsArray().RemoveAt(0),
    ["duplicate slot"] = m => m["templates"]![0]!["subscriptionSlots"]![1] = "my-subscription-1"
};
foreach (var (name, edit) in invalidManifests)
    Check($"invalid manifest: {name}", () => Expect(TemplateError.InvalidManifest, () => Load(Alter(edit))));
foreach (var invalid in new[] { "{", "null", "[]", "{\"schemaVersion\":1,\"schemaVersion\":1}" })
    Check($"malformed manifest: {invalid}", () =>
    {
        var data = new Dictionary<string, byte[]>(originals) { [EmbeddedTemplateCatalog.ManifestResource] = Encoding.UTF8.GetBytes(invalid) };
        Expect(TemplateError.InvalidManifest, () => Load(data));
    });
Check("unreadable resource has safe diagnostic", () =>
    Expect(TemplateError.ResourceUnreadable, () => new EmbeddedTemplateCatalog(_ => throw new IOException("private-path"), "0.1.0-s02")));
Check("oversized resource rejected", () =>
    Expect(TemplateError.ResourceUnreadable, () => new EmbeddedTemplateCatalog(_ => new MemoryStream(new byte[1024 * 1024 + 1]), "0.1.0-s02")));
Check("unknown template ID rejected", () =>
{
    try { new EmbeddedTemplateCatalog().ReadTemplate("../../config.yaml"); }
    catch (ArgumentException) { return; }
    throw new Exception("Unknown ID accepted.");
});
Check("external same-name files do not affect built-in resources", () =>
{
    var previous = Environment.CurrentDirectory;
    var isolated = Path.Combine(root, "artifacts", "tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(isolated, "Mihomo", "desktop"));
    Directory.CreateDirectory(Path.Combine(isolated, "Mihomo", "nas"));
    File.WriteAllText(Path.Combine(isolated, "config.yaml"), "external: wrong");
    File.WriteAllText(Path.Combine(isolated, "Mihomo", "manifest.json"), "broken");
    foreach (var id in new[] { "desktop", "nas" }) File.WriteAllText(Path.Combine(isolated, "Mihomo", id, "config.yaml"), "wrong");
    try
    {
        Environment.CurrentDirectory = isolated;
        var catalog = new EmbeddedTemplateCatalog();
        foreach (var id in new[] { "desktop", "nas" })
            Assert(catalog.ReadTemplate(id).SequenceEqual(originals[$"Skyline.Templates.{id}.yaml"]));
    }
    finally { Environment.CurrentDirectory = previous; }
});
Console.WriteLine($"{checks - failures}/{checks} passed.");
return failures == 0 ? 0 : 1;
