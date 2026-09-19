using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skyline.Core;

namespace Skyline.Infrastructure;

/// <summary>Loads only assembly resources. Never searches disk or the network.</summary>
public sealed class EmbeddedTemplateCatalog : ITemplateCatalog
{
    internal const string ManifestResource = "Skyline.Templates.manifest.json";
    private readonly Dictionary<string, byte[]> contents = new(StringComparer.Ordinal);
    private const int MaxResourceBytes = 1024 * 1024;

    public string SoftwareVersion { get; }
    public IReadOnlyList<TemplateDescriptor> Templates { get; }

    public EmbeddedTemplateCatalog() : this(
        typeof(EmbeddedTemplateCatalog).Assembly.GetManifestResourceStream,
        typeof(EmbeddedTemplateCatalog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion)
    { }

    internal EmbeddedTemplateCatalog(Func<string, Stream?> openResource, string softwareVersion)
    {
        Manifest manifest;
        try
        {
            var bytes = ReadResource(openResource, ManifestResource);
            using var document = JsonDocument.Parse(bytes);
            RejectDuplicateKeys(document.RootElement);
            manifest = JsonSerializer.Deserialize<Manifest>(bytes, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            }) ?? throw new TemplateCatalogException(TemplateError.InvalidManifest);
        }
        catch (JsonException)
        {
            throw new TemplateCatalogException(TemplateError.InvalidManifest);
        }

        if (manifest.SchemaVersion != 2 || string.IsNullOrWhiteSpace(manifest.SoftwareVersion)
            || manifest.Templates is not { Length: 2 })
            throw new TemplateCatalogException(TemplateError.InvalidManifest);
        if (!string.Equals(manifest.SoftwareVersion, softwareVersion, StringComparison.Ordinal))
            throw new TemplateCatalogException(TemplateError.SoftwareVersionMismatch);

        SoftwareVersion = manifest.SoftwareVersion;
        var descriptors = new List<TemplateDescriptor>();
        var expectedSlots = Enumerable.Range(1, 6).Select(n => $"my-subscription-{n}").ToArray();
        foreach (var item in manifest.Templates)
        {
            if (item is null || item.Id is not ("desktop" or "nas") || contents.ContainsKey(item.Id)
                || !Version.TryParse(item.Version, out var parsedVersion) || parsedVersion.Build < 0
                || item.ResourceName != $"Skyline.Templates.{item.Id}.yaml"
                || item.Sha256 is not { Length: 64 } || !item.Sha256.All(char.IsAsciiHexDigit)
                || item.ResidentialProxyName != "USA_Static_Native_ISP"
                || item.ContractVersion != 1 || item.EntryGroup != "chain-hop1-entry" || item.ExitGroup != "chain-hop2-exit"
                || item.RequiredEnvironmentFields is null || !item.RequiredEnvironmentFields.SequenceEqual(new[] { "localDns" })
                || item.SubscriptionSlots is null || !item.SubscriptionSlots.SequenceEqual(expectedSlots))
                throw new TemplateCatalogException(TemplateError.InvalidManifest);

            var content = ReadResource(openResource, item.ResourceName);
            if (!Convert.ToHexString(SHA256.HashData(content)).Equals(item.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new TemplateCatalogException(TemplateError.HashMismatch);
            contents.Add(item.Id, content);
            descriptors.Add(new TemplateDescriptor(item.Id, item.Version!, item.ResourceName,
                item.Sha256, item.ResidentialProxyName, Array.AsReadOnly(item.SubscriptionSlots),
                item.ContractVersion, item.EntryGroup, item.ExitGroup, Array.AsReadOnly(item.RequiredEnvironmentFields)));
        }
        Templates = descriptors.AsReadOnly();
    }

    public byte[] ReadTemplate(string id) => contents.TryGetValue(id, out var bytes)
        ? (byte[])bytes.Clone()
        : throw new ArgumentException("Unknown built-in template ID.", nameof(id));

    private static byte[] ReadResource(Func<string, Stream?> open, string name)
    {
        try
        {
            using var input = open(name) ?? throw new TemplateCatalogException(TemplateError.ResourceMissing);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + count > MaxResourceBytes)
                    throw new TemplateCatalogException(TemplateError.ResourceUnreadable);
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Do not propagate paths or resource contents to logs/UI.
            throw new TemplateCatalogException(TemplateError.ResourceUnreadable);
        }
    }

    private static void RejectDuplicateKeys(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new TemplateCatalogException(TemplateError.InvalidManifest);
                RejectDuplicateKeys(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectDuplicateKeys(child);
    }

    private sealed class Manifest
    {
        public int SchemaVersion { get; init; }
        public string? SoftwareVersion { get; init; }
        public Entry?[]? Templates { get; init; }
    }

    private sealed class Entry
    {
        public string? Id { get; init; }
        public string? Version { get; init; }
        public string? ResourceName { get; init; }
        public string? Sha256 { get; init; }
        public string? ResidentialProxyName { get; init; }
        public string[]? SubscriptionSlots { get; init; }
        public int ContractVersion { get; init; }
        public string? EntryGroup { get; init; }
        public string? ExitGroup { get; init; }
        public string[]? RequiredEnvironmentFields { get; init; }
    }
}
