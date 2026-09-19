namespace Skyline.Core;

public enum ConfigurationTarget { Desktop, Nas }
public sealed record SubscriptionInput(int Slot, bool Enabled, string? Url)
{
    public override string ToString() => $"SubscriptionInput(Slot={Slot}, Enabled={Enabled})";
}

/// <summary>Raw form values; contains secrets. Never persist or log this object.</summary>
public sealed record ConfigurationInput(
    ConfigurationTarget Target,
    IReadOnlyList<SubscriptionInput> Subscriptions,
    string? Server, string? Port, string? Username, string? Password,
    string? LocalDns, string? ControllerSecret,
    string? NasUsername = null, string? NasPassword = null)
{
    public override string ToString() => "ConfigurationInput(redacted)";
}

public enum IssueCode
{
    Required, InvalidTarget, InvalidSlot, DuplicateSlot, SubscriptionRequired,
    InvalidUrl, DuplicateUrl, InvalidServer, InvalidPort, InvalidDns, LoopbackDns,
    ControlCharacter, InvalidCredential, ColonNotAllowed, Placeholder,
    TemplateUnavailable, InvalidYaml, ContractMismatch, DuplicateName,
    InvalidReference, DependencyCycle, CachePathCollision,
    UnexpectedChange, RoundTripMismatch
}
public enum IssueSeverity { Error, Warning }

/// <summary>Only schema-owned identifiers and numeric slots; never includes input values.</summary>
public sealed record ValidationIssue(IssueCode Code, string Field, int? Slot = null,
    IssueSeverity Severity = IssueSeverity.Error);
public sealed record ConfigurationChange(string Code, string Field, int? Slot = null);

public sealed class GenerationResult
{
    public bool Success => Yaml is not null;
    public string? Yaml { get; }
    public string? TemplateId { get; }
    public IReadOnlyList<ValidationIssue> Issues { get; }
    public IReadOnlyList<ConfigurationChange> Changes { get; }
    internal GenerationResult(string? yaml, string? templateId, IEnumerable<ValidationIssue> issues,
        IEnumerable<ConfigurationChange>? changes = null)
    {
        Yaml = yaml;
        TemplateId = templateId;
        Issues = Array.AsReadOnly(issues.ToArray());
        Changes = Array.AsReadOnly(changes?.ToArray() ?? []);
    }
    public override string ToString() => $"GenerationResult(Success={Success})";
}
