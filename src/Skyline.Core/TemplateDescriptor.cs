namespace Skyline.Core;

/// <summary>Version-bound metadata; no deployment credentials or UI dependencies.</summary>
public sealed record TemplateDescriptor(
    string Id,
    string Version,
    string ResourceName,
    string Sha256,
    string ResidentialProxyName,
    IReadOnlyList<string> SubscriptionSlots,
    int ContractVersion,
    string EntryGroup,
    string ExitGroup,
    IReadOnlyList<string> RequiredEnvironmentFields);

public interface ITemplateCatalog
{
    string SoftwareVersion { get; }
    IReadOnlyList<TemplateDescriptor> Templates { get; }
    byte[] ReadTemplate(string id);
}

public enum TemplateError
{
    ResourceMissing,
    InvalidManifest,
    SoftwareVersionMismatch,
    HashMismatch,
    ResourceUnreadable
}

/// <summary>Stable diagnostic code; UI text belongs to the application resource layer.</summary>
public sealed class TemplateCatalogException(TemplateError code) : Exception(code.ToString())
{
    public TemplateError Code { get; } = code;
}
