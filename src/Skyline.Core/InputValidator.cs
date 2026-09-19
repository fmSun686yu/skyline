using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Skyline.Core;

internal static class InputValidator
{
    internal static List<ValidationIssue> Validate(ConfigurationInput input)
    {
        var issues = new List<ValidationIssue>();
        void Error(IssueCode code, string field, int? slot = null) => issues.Add(new(code, field, slot));
        if (!Enum.IsDefined(input.Target)) Error(IssueCode.InvalidTarget, "target");
        var slots = new HashSet<int>();
        var urls = new HashSet<string>(StringComparer.Ordinal);
        var enabled = 0;
        foreach (var sub in input.Subscriptions)
        {
            if (sub is null) { Error(IssueCode.InvalidSlot, "subscriptions"); continue; }
            if (sub.Slot is < 1 or > 6) { Error(IssueCode.InvalidSlot, "subscriptions"); continue; }
            if (!slots.Add(sub.Slot)) Error(IssueCode.DuplicateSlot, "subscriptions", sub.Slot);
            if (!sub.Enabled) continue;
            enabled++;
            if (string.IsNullOrWhiteSpace(sub.Url)) Error(IssueCode.Required, "subscriptions.url", sub.Slot);
            else if (sub.Url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))
                || sub.Url.Contains('\\') || !Uri.TryCreate(sub.Url, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host))
                Error(IssueCode.InvalidUrl, "subscriptions.url", sub.Slot);
            else if (!urls.Add(sub.Url)) Error(IssueCode.DuplicateUrl, "subscriptions.url", sub.Slot);
        }
        if (enabled == 0) Error(IssueCode.SubscriptionRequired, "subscriptions");

        var server = input.Server?.Trim();
        if (string.IsNullOrEmpty(server)) Error(IssueCode.Required, "server");
        else if (input.Server!.Any(char.IsControl) || server.Any(char.IsWhiteSpace)
            || server.IndexOfAny(['/', '\\', '[', ']', '%', '@', '?', '#']) >= 0
            || !(TryStrictIp(server, out _) || (!server.Contains(':') && ValidDomain(server))))
            Error(IssueCode.InvalidServer, "server");
        if (string.IsNullOrEmpty(input.Port) || !input.Port.All(char.IsAsciiDigit)
            || !int.TryParse(input.Port, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || port is < 1 or > 65535) Error(IssueCode.InvalidPort, "port");

        Credential(input.Username, "username", false, issues);
        Credential(input.Password, "password", false, issues);
        Credential(input.ControllerSecret, "controllerSecret", false, issues);
        if (input.Target == ConfigurationTarget.Nas)
        {
            Credential(input.NasUsername, "nasUsername", true, issues);
            Credential(input.NasPassword, "nasPassword", true, issues);
        }
        if (server == "socks5.example.com") Error(IssueCode.Placeholder, "server");
        if (input.Username == "YOUR_SOCKS5_USERNAME") Error(IssueCode.Placeholder, "username");
        if (input.Password == "YOUR_SOCKS5_PASSWORD") Error(IssueCode.Placeholder, "password");
        if (input.ControllerSecret is "your_secret_here" or "CHANGE_ME_CONTROLLER_SECRET") Error(IssueCode.Placeholder, "controllerSecret");
        if (input.Target == ConfigurationTarget.Nas)
        {
            if (input.NasUsername == "CHANGE_ME_PROXY_USER") Error(IssueCode.Placeholder, "nasUsername");
            if (input.NasPassword == "CHANGE_ME_PROXY_PASSWORD") Error(IssueCode.Placeholder, "nasPassword");
        }
        foreach (var sub in input.Subscriptions.Where(s => s is not null && s.Enabled && s.Slot is >= 1 and <= 6))
            if (sub.Url is not null && Enumerable.Range(1, 6).Any(i => sub.Url.Contains($"YOUR_SUBSCRIPTION_TOKEN_{i}", StringComparison.Ordinal)))
                Error(IssueCode.Placeholder, "subscriptions.url", sub.Slot);
        if (string.IsNullOrWhiteSpace(input.LocalDns)) Error(IssueCode.Required, "localDns");
        else if (!TryStrictIp(input.LocalDns, out var ip) || InvalidDns(ip!)) Error(IssueCode.InvalidDns, "localDns");
        else if (IPAddress.IsLoopback(ip!.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip))
            issues.Add(new(IssueCode.LoopbackDns, "localDns", Severity: IssueSeverity.Warning));
        return issues;
    }

    private static void Credential(string? value, string field, bool rejectColon, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value)) issues.Add(new(IssueCode.Required, field));
        else if (value.Any(char.IsControl) || value.Contains('\u2028') || value.Contains('\u2029'))
            issues.Add(new(IssueCode.ControlCharacter, field));
        else if (rejectColon && value.Contains(':')) issues.Add(new(IssueCode.ColonNotAllowed, field));
    }

    internal static bool TryStrictIp(string value, out IPAddress? ip)
    {
        ip = null;
        if (value.Length == 0 || value.Any(char.IsWhiteSpace) || value.IndexOfAny(['[', ']', '%']) >= 0) return false;
        if (!value.Contains(':'))
        {
            var parts = value.Split('.');
            if (parts.Length != 4 || parts.Any(p => p.Length == 0 || p.Length > 3 || !p.All(char.IsAsciiDigit)
                || (p.Length > 1 && p[0] == '0') || !byte.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _))) return false;
        }
        return IPAddress.TryParse(value, out ip);
    }

    private static bool InvalidDns(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.Broadcast)) return true;
        var bytes = ip.GetAddressBytes();
        return ip.AddressFamily == AddressFamily.InterNetwork ? bytes[0] is >= 224 and <= 239 : bytes[0] == 0xff;
    }

    private static bool ValidDomain(string name)
    {
        // Do not treat legacy short/integer IPv4 spellings as hostnames.
        if (name.All(c => char.IsAsciiDigit(c) || c == '.')) return false;
        try
        {
            var ascii = new IdnMapping().GetAscii(name.TrimEnd('.'));
            return ascii.Length is > 0 and <= 253 && ascii.Split('.').All(label => label.Length is > 0 and <= 63
                && char.IsAsciiLetterOrDigit(label[0]) && char.IsAsciiLetterOrDigit(label[^1])
                && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
        }
        catch (ArgumentException) { return false; }
    }
}
